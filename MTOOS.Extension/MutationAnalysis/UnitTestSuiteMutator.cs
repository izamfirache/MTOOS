using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using MTOOS.Extension.Helpers;
using MTOOS.Extension.Models;
using MTOOS.Extension.Mutators;
using MTOOS.Extension.TestMutators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MTOOS.Extension.MutationAnalysis
{
    public class UnitTestSuiteMutator
    {
        private Solution2 _currentSolution;
        private RoslynSetupHelper _roslynSetupHelper;
        private EnvDteHelper _envDteHelper;

        public UnitTestSuiteMutator(Solution2 currentSolution)
        {
            _currentSolution = currentSolution;
            _roslynSetupHelper = new RoslynSetupHelper();
            _envDteHelper = new EnvDteHelper();
        }

        public UnitTestMutationResult PerformMutationForUnitTestProject(
            EnvDTE.Project unitTestProject, 
            SourceCodeMutationResult sourceCodeMutationResult)
        {
            //setup workspace, solution, project
            var solution = _roslynSetupHelper.GetSolutionToAnalyze(
                _roslynSetupHelper.CreateWorkspace(), _currentSolution.FileName);
            var projectToAnalyze = _roslynSetupHelper.GetProjectToAnalyze(solution, unitTestProject.Name);
            var projectAssembly = _roslynSetupHelper.GetProjectAssembly(projectToAnalyze);

            //generate the mutants based on user options
            List<GeneratedMutant> generatedUnitTestMutants = 
                GenerateMutantsForUnitTestProject(projectAssembly, sourceCodeMutationResult.GeneratedMutants);

            //compile the generated unit test mutants
            List<MetadataReference> metadataReferences = 
                CompileUnitTestMutants(generatedUnitTestMutants, projectAssembly, 
                sourceCodeMutationResult, unitTestProject);

            //compile the unit test suite code with the unit test mutants
            //in order to run them over the mutants
            var mutatedUnitTestProject = projectToAnalyze.AddMetadataReferences(metadataReferences);
            Compilation mutatedSourceCodeProjectCompilation = mutatedUnitTestProject.GetCompilationAsync().Result;

            return new UnitTestMutationResult()
            {
                MutatedUnitTestProject = mutatedUnitTestProject,
                GeneratedUnitTestMutants = generatedUnitTestMutants
            };
        }

        private List<MetadataReference> CompileUnitTestMutants(
            List<GeneratedMutant> generatedMutants, 
            Compilation projectAssembly,
            SourceCodeMutationResult sourceCodeMutationResult,
            EnvDTE.Project unitTestProject)
        {
            var compiledMutants = new List<MetadataReference>();
            var options = new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Debug,
                        allowUnsafe: true);

            var unitTestProjectRefereces = _envDteHelper.GetEnvDteProjectReferences(unitTestProject);
            var _references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(System.Reflection.Binder).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(sourceCodeMutationResult.MutatedSourceCodeProject.OutputFilePath)
            };
            foreach (string referencePath in unitTestProjectRefereces)
            {
                _references.Add(MetadataReference.CreateFromFile(referencePath));
            }

            foreach (GeneratedMutant genMutant in generatedMutants)
            {
                var compilation = CSharpCompilation.Create(
                    genMutant.MutantName,
                    options: options,
                    references: _references)
                    .AddSyntaxTrees(genMutant.MutatedCodeRoot.SyntaxTree);

                var stream = new MemoryStream();
                var emitResult = compilation.Emit(stream);

                if (emitResult.Success)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(stream);
                    compiledMutants.Add(MetadataReference.CreateFromStream(stream));
                }
            }

            return compiledMutants;
        }

        private List<GeneratedMutant> GenerateMutantsForUnitTestProject(
            Compilation projectAssembly, 
            List<GeneratedMutant> sourceCodeGeneratedMutants)
        {
            var generatedMutants = new List<GeneratedMutant>();

            foreach (var syntaxTree in projectAssembly.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;
                var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();

                foreach (NamespaceDeclarationSyntax ns in namespaces)
                {
                    var namespaceClasses = ns.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                    var namespaceTreeRoot = ns.SyntaxTree.GetRoot();

                    if (namespaceClasses.Count != 0)
                    {
                        var unitTestClassName = namespaceClasses.ElementAt(0).Identifier.Value.ToString();

                        foreach (GeneratedMutant mi in sourceCodeGeneratedMutants)
                        {
                            if (unitTestClassName.Contains(mi.OriginalClassName))
                            {
                                var mutatedUnitTestClassName = string.Format("{0}UnitTestMutant", mi.MutantName);
                                var unitTestClassMutator = new UnitTestClassMutator(mi.OriginalClassName, mi.MutantName,
                                    unitTestClassName, mutatedUnitTestClassName);
                                var mutatedUnitTestClassNsRoot = unitTestClassMutator.Visit(namespaceTreeRoot);

                                generatedMutants.Add(new GeneratedMutant()
                                {
                                    Id = Guid.NewGuid(),
                                    OriginalClassName = unitTestClassName,
                                    MutantName = mutatedUnitTestClassName,
                                    OriginalCodeRoot = namespaceTreeRoot,
                                    MutatedCodeRoot = mutatedUnitTestClassNsRoot,
                                    OriginalProgramCode = namespaceTreeRoot.ToFullString(),
                                    MutatedCode = mutatedUnitTestClassNsRoot.ToFullString()
                                });
                            }
                        }
                    }
                }
            }

            return generatedMutants;
        }
    }
}
