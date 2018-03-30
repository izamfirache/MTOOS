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
using System.Reflection.PortableExecutable;

namespace MTOOS.Extension.MutationAnalysis
{
    public class SourceCodeMutator
    {
        private Solution2 _currentSolution;
        private RoslynSetupHelper _roslynSetupHelper;
        private EnvDteHelper _envDteHelper;
        public SourceCodeMutator(Solution2 currentSolution)
        {
            _currentSolution = currentSolution;
            _roslynSetupHelper = new RoslynSetupHelper();
            _envDteHelper = new EnvDteHelper();
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
            
            var mutantsGenerationResult = 
                GenerateMutantsForProject(projectAssembly, projectSemanticModel, options);
            var mutatedSourceCodeProjectDllPath = Path.Combine(
                Path.GetDirectoryName(sourceCodeProject.FullName), "MutatedSourceCodeProject.dll");

            Compilation finalCompilation = GetMutatedCompilation(mutantsGenerationResult, sourceCodeProject,
                mutatedSourceCodeProjectDllPath);

            return new SourceCodeMutationResult()
            {
                MutatedSourceCodeProjectCompilation = finalCompilation,
                GeneratedMutants = mutantsGenerationResult.GeneratedMutants,
                OutputPath = mutatedSourceCodeProjectDllPath
            };
        }

        private Compilation GetMutatedCompilation(GenerateMutantsForProjectResult mutantsGenerationResult,
            EnvDTE.Project sourceCodeProject, string outputPath)
        {
            List<MetadataReference> metadataReferences = new List<MetadataReference>();
            var options = new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Debug,
                        allowUnsafe: true);

            var _references = new List<MetadataReference>()
            { 
                MetadataReference.CreateFromFile(typeof(System.Reflection.Binder).GetTypeInfo().Assembly.Location)
            };
            var sourceCodeRefereces = _envDteHelper.GetEnvDteProjectReferences(sourceCodeProject);
            foreach (string referencePath in sourceCodeRefereces)
            {
                _references.Add(MetadataReference.CreateFromFile(referencePath));
            }

            var compilationUnit = SyntaxFactory.CompilationUnit().WithMembers(
                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                    SyntaxFactory.NamespaceDeclaration(
                        SyntaxFactory.IdentifierName("Default"))));

            var compilationUnitRoot = compilationUnit.SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var defaultAddedNameSpace = compilationUnitRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
            var compilationUnitSyntaxTree = compilationUnitRoot
                .InsertNodesAfter(defaultAddedNameSpace, mutantsGenerationResult.MutatedSyntaxTrees);

            var compilation = CSharpCompilation.Create(
                "MutatedSourceCodeProjectCompilation",
                options: options,
                references: _references)
                .AddSyntaxTrees(compilationUnitSyntaxTree.SyntaxTree);
            
            var emitResult = compilation.Emit(outputPath);

            if (emitResult.Success)
            {
                return compilation;
            }
            
            return null;
        }

        private GenerateMutantsForProjectResult GenerateMutantsForProject(
            Compilation projectAssembly,
            SemanticModel projectSemanticModel,
            List<string> options)
        {
            var generatedMutants = new List<GeneratedMutant>();
            var mutatedNamespaces = new List<NamespaceDeclarationSyntax>();

            foreach (var syntaxTree in projectAssembly.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;
                var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();

                foreach (NamespaceDeclarationSyntax ns in namespaces)
                {
                    var namespaceClasses = ns.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                    var namespaceTreeRoot = ns.SyntaxTree.GetRoot();

                    var name = ns.Name.ToFullString();
                    if (namespaceClasses.Count != 0 && !mutatedNamespaces.Any(p => p.Name.ToFullString() == name))
                    {
                        //TODO: do this for all classes from that namespace !
                        ClassDeclarationSyntax classSyntaxNode = namespaceClasses.ElementAt(0);
                        SyntaxNode classSyntaxRootNode = classSyntaxNode.SyntaxTree.GetRoot();
                        var className = classSyntaxNode.Identifier.Value.ToString();

                        var mutantCreator = new MutantCreator(className, classSyntaxRootNode);

                        if (options.Contains("1"))
                        {
                            //apply additive and multiplicative mutations
                            var mathOperatorMutator = new AdditiveAndMultiplicativeOp
                                (classSyntaxRootNode, mutantCreator);
                            mathOperatorMutator.Visit(classSyntaxRootNode);
                        }

                        if (options.Contains("2"))
                        {
                            //apply assignment expression mutation
                            var assignmentExprMutator = new AssignmentExprMutator
                            (classSyntaxRootNode, mutantCreator, projectSemanticModel);
                            assignmentExprMutator.Visit(classSyntaxRootNode);
                        }

                        if (options.Contains("3"))
                        {
                            //apply realtional and equity mutations
                            var relationalAndEquityOp = new RelationalAndEqualityOp
                            (classSyntaxRootNode, mutantCreator);
                            relationalAndEquityOp.Visit(classSyntaxRootNode);
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
                                (classSyntaxRootNode, mutantCreator, classFieldsIdentifiers);
                            thisKeywordStatementDeletion.Visit(classSyntaxRootNode);
                        }

                        generatedMutants.AddRange(mutantCreator.GetMutatedClasses());

                        //add the mutants classes under the same namespace root node
                        var toBeAddedClasses = new List<ClassDeclarationSyntax>();
                        foreach(SyntaxNode syntaxNode in generatedMutants.Select(gm => gm.MutatedCodeRoot).ToList())
                        {
                            var classes = syntaxNode.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                            var firstClass = classes.ElementAt(0);
                            toBeAddedClasses.Add(firstClass);
                        }
                        
                        NamespaceDeclarationSyntax mutatedNamespace = 
                            ns.InsertNodesAfter(classSyntaxNode, toBeAddedClasses);
                        mutatedNamespaces.Add(mutatedNamespace);
                    }
                }
            }

            return new GenerateMutantsForProjectResult()
            {
                GeneratedMutants = generatedMutants,
                MutatedSyntaxTrees = mutatedNamespaces
            };
        }
    }
}
