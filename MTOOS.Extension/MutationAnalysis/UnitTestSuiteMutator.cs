using EnvDTE;
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
        public string SourceCodeMutantsCodePath = "";
        public string UnitTestsMutantsCodePath = "";
        private List<UsingDirectiveSyntax> Usings = new List<UsingDirectiveSyntax>();
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

            var isMutatioAnalysisCompleted = GetMutatedCompilation(generatedUnitTestMutants, unitTestProject,
                sourceCodeProject, sourceCodeMutationResult, projectToAnalyze.OutputFilePath);

            if (isMutatioAnalysisCompleted)
            {
                return new UnitTestMutationResult()
                {
                    GeneratedUnitTestMutants = generatedUnitTestMutants,
                    OutputPath = projectToAnalyze.OutputFilePath
                };
            }
            else
            {
                throw new Exception("Error at mutation analysis step.");
            }
        }

        private bool GetMutatedCompilation(
            List<GeneratedMutant> generatedUnitTestMutants,
            EnvDTE.Project unitTestProject,
            EnvDTE.Project sourceCodeproject,
            SourceCodeMutationResult sourceCodeMutationResult,
            string unitTestProjectoutputPath)
        {
            bool sourceCodeMutation = false;
            bool unitTestCodeMutation = false;
            
            sourceCodeMutation = 
                AddMutationAnalysisToSourceCodeProject(sourceCodeMutationResult, sourceCodeproject);
            
            if (sourceCodeMutation)
            {
                //MessageBox.Show("Source code project compiled after mutation.");

                unitTestCodeMutation =
                    AddMutationAnalysisToUnitTestProject(generatedUnitTestMutants, unitTestProject);

                //if (unitTestCodeMutation)
                //{
                //    MessageBox.Show("Unit test project compiled after mutation.");
                //}
            }

            if(sourceCodeMutation && unitTestCodeMutation)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool AddMutationAnalysisToUnitTestProject(List<GeneratedMutant> generatedUnitTestMutants,
            EnvDTE.Project unitTestProject)
        {
            var selfUsing = SyntaxFactory.UsingDirective(
                    SyntaxFactory.IdentifierName("SourceCodeProjectMutants"));

            selfUsing = selfUsing.WithUsingKeyword(
                                selfUsing.UsingKeyword.WithTrailingTrivia(
                                    SyntaxFactory.Whitespace(" ")));
            Usings.Add(selfUsing);

            var unitTestCompilationUnit = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(Usings.ToArray()))
            .WithMembers(
                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                    SyntaxFactory.NamespaceDeclaration(
                        SyntaxFactory.IdentifierName("UnitTestProjectMutants"))
                    .WithMembers(
                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                            SyntaxFactory.ClassDeclaration("FirstUnitTestMutant")
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)))))))
            .NormalizeWhitespace();

            var unitTestCompilationUnitRoot = unitTestCompilationUnit.SyntaxTree.GetRoot() as CompilationUnitSyntax;

            var unitTestMutantsNameSpace =
                unitTestCompilationUnitRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();

            var firstUnitTestMutant =
                unitTestMutantsNameSpace.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

            var unitTestCompilationUnitSyntaxTree = unitTestCompilationUnitRoot
                .InsertNodesAfter(firstUnitTestMutant,
                generatedUnitTestMutants.Select(p => p.MutatedCodeRoot));

            var unitTestTree = CSharpSyntaxTree.ParseText(unitTestCompilationUnitSyntaxTree.ToFullString());

            UnitTestsMutantsCodePath = Path.Combine(Path.GetDirectoryName(unitTestProject.FullName),
                "UnitTestMutants.cs");
            File.WriteAllText(UnitTestsMutantsCodePath, unitTestTree.GetRoot().ToFullString());

            unitTestProject.ProjectItems.AddFromFile(UnitTestsMutantsCodePath);
            unitTestProject.Save();

            SolutionBuild2 solutionBuild2 = (SolutionBuild2)unitTestProject.DTE.Solution.SolutionBuild;
            solutionBuild2.BuildProject(solutionBuild2.ActiveConfiguration.Name,
                unitTestProject.UniqueName, true);
            bool unitTestCompiledOK = (solutionBuild2.LastBuildInfo == 0);

            return unitTestCompiledOK;
        }

        private bool AddMutationAnalysisToSourceCodeProject(SourceCodeMutationResult sourceCodeMutationResult,
            EnvDTE.Project sourceCodeProject)
        {
            var sourceCodeUsings = sourceCodeMutationResult.Usings
                .GroupBy(u => u.ToFullString()).Select(iu => iu.First()).ToList();
            var unitTestUsings = Usings.GroupBy(u => u.ToFullString()).Select(iu => iu.First()).ToList();

            var sourceCodeCompilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(sourceCodeUsings.ToArray()))
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.NamespaceDeclaration(
                            SyntaxFactory.IdentifierName("SourceCodeProjectMutants"))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.ClassDeclaration("FirstSourceCodeMutant")
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))))))
                .NormalizeWhitespace();

            var sourceCodeCompilationUnitRoot = sourceCodeCompilationUnit.SyntaxTree.GetRoot() as CompilationUnitSyntax;

            var sourceCodeMutantsNameSpace =
                sourceCodeCompilationUnitRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();

            var firstSourceCodeMutant =
                sourceCodeMutantsNameSpace.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

            var sourceCodeCompilationUnitSyntaxTree = sourceCodeCompilationUnitRoot
                .InsertNodesAfter(firstSourceCodeMutant,
                sourceCodeMutationResult.GeneratedMutants.Select(p => p.MutatedCodeRoot));

            var sourceCodeTree = CSharpSyntaxTree.ParseText(sourceCodeCompilationUnitSyntaxTree.ToFullString());

            SourceCodeMutantsCodePath = Path.Combine(Path.GetDirectoryName(sourceCodeProject.FullName), 
                "SourceCodeMutants.cs");
            File.WriteAllText(SourceCodeMutantsCodePath, sourceCodeTree.GetRoot().ToFullString());

            //add source code mutation analysis to sc project
            sourceCodeProject.ProjectItems.AddFromFile(SourceCodeMutantsCodePath);
            sourceCodeProject.Save();

            //recompile the source code project
            SolutionBuild2 sourceCodeSolutionBuild2 = (SolutionBuild2)sourceCodeProject.DTE.Solution.SolutionBuild;
            sourceCodeSolutionBuild2.BuildProject(sourceCodeSolutionBuild2.ActiveConfiguration.Name,
                sourceCodeProject.UniqueName, true);

            bool sourceCodeCompiledOK = (sourceCodeSolutionBuild2.LastBuildInfo == 0);

            return sourceCodeCompiledOK;
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
                        Usings.AddRange(namespaceTreeRoot.DescendantNodes()
                            .OfType<UsingDirectiveSyntax>().ToList());

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
