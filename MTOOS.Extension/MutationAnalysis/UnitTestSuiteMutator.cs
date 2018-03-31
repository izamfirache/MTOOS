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
        public string MutatedUnitTestCodePath;
        public UnitTestSuiteMutator(Solution2 currentSolution)
        {
            _currentSolution = currentSolution;
            _roslynSetupHelper = new RoslynSetupHelper();
            _envDteHelper = new EnvDteHelper();
        }

        public UnitTestMutationResult PerformMutationForUnitTestProject(
            EnvDTE.Project unitTestProject, 
            SourceCodeMutationResult sourceCodeMutationResult,
            EnvDTE.Project sourceCodeProject)
        {
            var solution = _roslynSetupHelper.GetSolutionToAnalyze(
                _roslynSetupHelper.CreateWorkspace(), _currentSolution.FileName);
            var projectToAnalyze = _roslynSetupHelper.GetProjectToAnalyze(solution, unitTestProject.Name);
            var projectAssembly = _roslynSetupHelper.GetProjectAssembly(projectToAnalyze);
            
            List<GeneratedMutant> generatedUnitTestMutants = 
                GenerateMutantsForUnitTestProject(projectAssembly, sourceCodeMutationResult.GeneratedMutants);

            Compilation finalCompilation = GetMutatedCompilation(generatedUnitTestMutants, unitTestProject,
                sourceCodeMutationResult);

            if (finalCompilation != null)
            {
                return new UnitTestMutationResult()
                {
                    MutatedUnitTestProjectCompilation = finalCompilation,
                    GeneratedUnitTestMutants = generatedUnitTestMutants,
                    OutputPath = projectToAnalyze.OutputFilePath
                };
            }

            return null;
        }

        private Compilation GetMutatedCompilation(
            List<GeneratedMutant> generatedUnitTestMutants,
            EnvDTE.Project unitTestProject,
            SourceCodeMutationResult sourceCodeMutationResult)
        {
            var options = new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Debug,
                        allowUnsafe: true);

            var unitTestProjectRefereces = _envDteHelper.GetEnvDteProjectReferences(unitTestProject);
            var _references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(System.Reflection.Binder).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(sourceCodeMutationResult.OutputPath)
            };
            foreach (string referencePath in unitTestProjectRefereces)
            {
                _references.Add(MetadataReference.CreateFromFile(referencePath));
            }

            var compilationUnit = SyntaxFactory.CompilationUnit().WithUsings(
                    SyntaxFactory.List(
                    new UsingDirectiveSyntax[]{
                        SyntaxFactory.UsingDirective(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("NUnit"),
                                SyntaxFactory.IdentifierName("Framework")))}))
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.NamespaceDeclaration(
                            SyntaxFactory.IdentifierName("MutatedUnitTestClasses"))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.ClassDeclaration("FirstUnitTestTestClass")
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))))))
                .NormalizeWhitespace();

            var compilationUnitRoot = compilationUnit.SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var mutatedUnitTestClassesNameSpace =
                compilationUnitRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
            var firstUnitTestTestMutant =
                mutatedUnitTestClassesNameSpace.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

            generatedUnitTestMutants.AddRange(sourceCodeMutationResult.GeneratedMutants);
            var compilationUnitSyntaxTree = compilationUnitRoot
                .InsertNodesAfter(firstUnitTestTestMutant, 
                generatedUnitTestMutants.Select(p => p.MutatedCodeRoot));
            
            var compilation = CSharpCompilation.Create(
                "MutatedUnitTestprojectCompilation",
                options: options,
                references: _references)
                .AddSyntaxTrees(compilationUnitSyntaxTree.SyntaxTree);

            var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream);

            if (emitResult.Success)
            {
                MutatedUnitTestCodePath = Path.Combine(Path.GetDirectoryName(unitTestProject.FullName), 
                    "MutationTestingCode.cs");
                using (StreamWriter file = new StreamWriter(MutatedUnitTestCodePath, true))
                {
                    file.Write(compilationUnitSyntaxTree.SyntaxTree);
                }
                return compilation;
            }

            return null;
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
                        var unitTestClass = namespaceClasses.ElementAt(0);
                        var unitTestClassName = unitTestClass.Identifier.Value.ToString();

                        foreach (GeneratedMutant mi in sourceCodeGeneratedMutants)
                        {
                            if (unitTestClassName.Contains(mi.OriginalClassName))
                            {
                                var mutatedUnitTestClassName = string.Format("{0}UnitTestMutant", mi.MutantName);
                                var unitTestClassMutator = new UnitTestClassMutator(mi.OriginalClassName, mi.MutantName,
                                    unitTestClassName, mutatedUnitTestClassName);
                                var mutatedUnitTestClassNsRoot = (ClassDeclarationSyntax)unitTestClassMutator.Visit(unitTestClass);

                                generatedMutants.Add(new GeneratedMutant()
                                {
                                    Id = Guid.NewGuid(),
                                    OriginalClassName = unitTestClassName,
                                    MutantName = mutatedUnitTestClassName,
                                    OriginalCodeRoot = unitTestClass,
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
