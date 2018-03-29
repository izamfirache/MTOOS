using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;
using MTOOS.Extension.Mutators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE80;
using MTOOS.Extension.Helpers;
using MTOOS.Extension.Models;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CSharp.RuntimeBinder;
using System.Reflection;
using Mono.Cecil;

namespace MTOOS.Extension.MutationAnalysis
{
    public class SourceCodeMutator
    {
        private Solution2 _currentSolution;
        private RoslynSetupHelper _roslynSetupHelper;
        public SourceCodeMutator(Solution2 currentSolution)
        {
            _currentSolution = currentSolution;
            _roslynSetupHelper = new RoslynSetupHelper();
        }

        public SourceCodeMutationResult PerformMutationAnalysisOnSourceCodeProject(
            EnvDTE.Project sourceCodeProject, 
            List<string> options)
        {
            //setup, create workspace, find solution, 
            //get the compilation unit and the semantic model for the source code project
            var workspace = _roslynSetupHelper.CreateWorkspace();
            var solution = _roslynSetupHelper.GetSolutionToAnalyze(
                workspace, _currentSolution.FileName);
            var projectToAnalyze = _roslynSetupHelper.GetProjectToAnalyze(solution, sourceCodeProject.Name);
            var projectAssembly = _roslynSetupHelper.GetProjectAssembly(projectToAnalyze);
            var projectSemanticModel = _roslynSetupHelper.GetProjectSemanticModel(
                _roslynSetupHelper.GetProjectToAnalyze(solution, sourceCodeProject.Name));

            //generate the mutants based on user options
            var generatedMutants = GenerateMutantsForProject(projectAssembly, projectSemanticModel, options);

            //compile the generated mutants
            var metadataReferences = CompileTheGeneratedMutants(generatedMutants, sourceCodeProject,  projectToAnalyze);

            //compile the source code with the mutants in order to make them visible (and usable)
            var mutatedSourceCodeProject = projectToAnalyze.AddMetadataReferences(metadataReferences);
            Compilation mutatedSourceCodeProjectCompilation = mutatedSourceCodeProject.GetCompilationAsync().Result;

            return new SourceCodeMutationResult()
            {
                MutatedSourceCodeProject = mutatedSourceCodeProject,
                GeneratedMutants = generatedMutants
            };
        }

        private List<MetadataReference> CompileTheGeneratedMutants(
            List<GeneratedMutant> generatedMutants, 
            EnvDTE.Project sourceCodeProject,
            Project projectToAnalyze)
        {
            List<MetadataReference> metadataReferences = new List<MetadataReference>();
            var options = new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Debug,
                        allowUnsafe: true);

            var _references = new[] {
                    MetadataReference.CreateFromFile(typeof(System.Reflection.Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(projectToAnalyze.OutputFilePath)
                };

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
                    metadataReferences.Add(MetadataReference.CreateFromStream(stream));
                }
            }

            return metadataReferences;
        }

        private List<GeneratedMutant> GenerateMutantsForProject(
            Compilation projectAssembly,
            SemanticModel projectSemanticModel,
            List<string> options)
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
                        //TODO: do this for all classes from that namespace !

                        var className = namespaceClasses.ElementAt(0).Identifier.Value.ToString();
                        var mutantCreator = new MutantCreator(className, namespaceTreeRoot);

                        if (options.Contains("1"))
                        {
                            //apply additive and multiplicative mutations
                            var mathOperatorMutator = new AdditiveAndMultiplicativeOp
                                (namespaceTreeRoot, mutantCreator);
                            mathOperatorMutator.Visit(namespaceTreeRoot);
                        }

                        if (options.Contains("2"))
                        {
                            //apply assignment expression mutation
                            var assignmentExprMutator = new AssignmentExprMutator
                            (namespaceTreeRoot, mutantCreator, projectSemanticModel);
                            assignmentExprMutator.Visit(namespaceTreeRoot);
                        }

                        if (options.Contains("3"))
                        {
                            //apply realtional and equity mutations
                            var relationalAndEquityOp = new RelationalAndEqualityOp
                            (namespaceTreeRoot, mutantCreator);
                            relationalAndEquityOp.Visit(namespaceTreeRoot);
                        }

                        if (options.Contains("4"))
                        {
                            //apply this keyword statement deletion muttion
                            var classFields = namespaceClasses.ElementAt(0)
                            .DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

                            var classFieldsIdentifiers = new List<string>();
                            foreach (FieldDeclarationSyntax field in classFields)
                            {
                                classFieldsIdentifiers.Add(
                                    field.Declaration.Variables.First().Identifier.ToString());
                            }

                            var thisKeywordStatementDeletion = new ThisStatementDeletion
                                (namespaceTreeRoot, mutantCreator, classFieldsIdentifiers);
                            thisKeywordStatementDeletion.Visit(namespaceTreeRoot);
                        }

                        generatedMutants.AddRange(mutantCreator.GetMutatedClasses());
                    }
                }
            }

            return generatedMutants;
        }
    }
}
