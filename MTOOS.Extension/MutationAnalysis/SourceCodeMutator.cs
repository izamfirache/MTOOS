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
            var workspace = _roslynSetupHelper.CreateWorkspace();
            var solution = _roslynSetupHelper.GetSolutionToAnalyze(
                workspace, _currentSolution.FileName);
            var projectToAnalyze = _roslynSetupHelper.GetProjectToAnalyze(solution, sourceCodeProject.Name);
            var projectAssembly = _roslynSetupHelper.GetProjectAssembly(projectToAnalyze);
            var projectSemanticModel = _roslynSetupHelper.GetProjectSemanticModel(
                _roslynSetupHelper.GetProjectToAnalyze(solution, sourceCodeProject.Name));
            
            var generatedMutants = 
                GenerateMutantsForProject(projectAssembly, projectSemanticModel, options, workspace);

            //Compilation finalCompilation = GetMutatedCompilation(generatedMutants, sourceCodeProject);
            
            return new SourceCodeMutationResult()
            {
                GeneratedMutants = generatedMutants,
                OutputPath = projectToAnalyze.OutputFilePath
            };
        }

        //private Compilation GetMutatedCompilation(List<GeneratedMutant> generatedMutants,
        //    EnvDTE.Project sourceCodeProject)
        //{
        //    List<MetadataReference> metadataReferences = new List<MetadataReference>();
        //    var options = new CSharpCompilationOptions(
        //                OutputKind.DynamicallyLinkedLibrary,
        //                optimizationLevel: OptimizationLevel.Debug,
        //                allowUnsafe: true);

        //    var _references = new List<MetadataReference>()
        //    { 
        //        MetadataReference.CreateFromFile(typeof(System.Reflection.Binder).GetTypeInfo().Assembly.Location)
        //    };
        //    var sourceCodeRefereces = _envDteHelper.GetEnvDteProjectReferences(sourceCodeProject);
        //    foreach (string referencePath in sourceCodeRefereces)
        //    {
        //        _references.Add(MetadataReference.CreateFromFile(referencePath));
        //    }

        //    var compilationUnit = SyntaxFactory.CompilationUnit().WithMembers(
        //        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        //            SyntaxFactory.NamespaceDeclaration(
        //                SyntaxFactory.IdentifierName("MutatedClasses"))
        //                .WithMembers(
        //    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        //        SyntaxFactory.ClassDeclaration("FirstTestMutant")
        //        .WithModifiers(
        //            SyntaxFactory.TokenList(
        //                SyntaxFactory.Token(SyntaxKind.PublicKeyword)))))))
        //                    .NormalizeWhitespace();

        //    var compilationUnitRoot = compilationUnit.SyntaxTree.GetRoot() as CompilationUnitSyntax;
        //    var mutatedClassesNameSpace = 
        //        compilationUnitRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
        //    var firstTestMutant = 
        //        mutatedClassesNameSpace.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        //    var compilationUnitSyntaxTree = compilationUnitRoot
        //        .InsertNodesAfter(firstTestMutant, generatedMutants.Select(p=>p.MutatedCodeRoot));

        //    var compilation = CSharpCompilation.Create(
        //        "MutatedSourceCodeProject",
        //        options: options,
        //        references: _references)
        //        .AddSyntaxTrees(compilationUnitSyntaxTree.SyntaxTree);

        //    var stream = new MemoryStream();
        //    var emitResult = compilation.Emit(stream);

        //    if (emitResult.Success)
        //    {
        //        return compilation;
        //    }

        //    throw new Exception("There was an error while trying to mutate the source code project." +
        //        "Can not compile the generated mutants.");
        //}

        private List<GeneratedMutant> GenerateMutantsForProject(
            Compilation projectAssembly,
            SemanticModel projectSemanticModel,
            List<string> options,
            Workspace workspace)
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

                    var name = ns.Name.ToFullString();
                    if (namespaceClasses.Count != 0)
                    {
                        //TODO: do this for all classes from that namespace !
                        ClassDeclarationSyntax classSyntaxNode = namespaceClasses.ElementAt(0);
                        var className = classSyntaxNode.Identifier.Value.ToString();

                        var mutantCreator = new MutantCreator(className, classSyntaxNode);

                        //mutate boundary operators
                        if (options.Contains("1"))
                        {
                            mutantCreator.MutatorType = "BOM";
                            var mathOperatorMutator = new BoundaryOpMutator
                                (classSyntaxNode, mutantCreator);
                            mathOperatorMutator.Visit(classSyntaxNode);
                        }

                        //nagate relational and equality operators
                        if (options.Contains("2"))
                        {
                            mutantCreator.MutatorType = "REOM";
                            var mathOperatorMutator = new RelationalAndEqualityOpMutator
                                (classSyntaxNode, mutantCreator);
                            mathOperatorMutator.Visit(classSyntaxNode);
                        }

                        //mutate non basic conditionals
                        if (options.Contains("3"))
                        {
                            //replace complex boolean expressions with true or false
                            mutantCreator.MutatorType = "RNBCM";
                            var mathOperatorMutator = new RemoveNonBasicConditionalsMutator
                                (classSyntaxNode, mutantCreator);
                            mathOperatorMutator.Visit(classSyntaxNode);
                        }

                        //mutate math operators
                        if (options.Contains("4"))
                        {
                            mutantCreator.MutatorType = "MOM";
                            var mathOperatorMutator = new MathOperatorsMutator
                                (classSyntaxNode, mutantCreator, projectSemanticModel);
                            mathOperatorMutator.Visit(classSyntaxNode);
                        }

                        //if (options.Contains("1"))
                        //{
                        //    //apply additive and multiplicative mutations
                        //    var mathOperatorMutator = new AdditiveAndMultiplicativeOp
                        //        (classSyntaxNode, mutantCreator);
                        //    mathOperatorMutator.Visit(classSyntaxNode);
                        //}

                        //if (options.Contains("2"))
                        //{
                        //    //apply assignment expression mutation
                        //    var assignmentExprMutator = new AssignmentExprMutator
                        //    (classSyntaxNode, mutantCreator, projectSemanticModel);
                        //    assignmentExprMutator.Visit(classSyntaxNode);
                        //}

                        //if (options.Contains("3"))
                        //{
                        //    //apply realtional and equity mutations
                        //    var relationalAndEquityOp = new RelationalAndEqualityOp
                        //    (classSyntaxNode, mutantCreator);
                        //    relationalAndEquityOp.Visit(classSyntaxNode);
                        //}

                        //if (options.Contains("4"))
                        //{
                        //    //apply this keyword statement deletion muttion
                        //    var classFields = namespaceClasses.ElementAt(0)
                        //    .DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

                        //    var classFieldsIdentifiers = new List<string>();
                        //    foreach (FieldDeclarationSyntax field in classFields)
                        //    {
                        //        classFieldsIdentifiers.Add(
                        //            field.Declaration.Variables.First().Identifier.ToString());
                        //    }

                        //    var thisKeywordStatementDeletion = new ThisStatementDeletion
                        //        (classSyntaxNode, mutantCreator, classFieldsIdentifiers);
                        //    thisKeywordStatementDeletion.Visit(classSyntaxNode);
                        //}

                        generatedMutants.AddRange(mutantCreator.GetMutatedClasses());
                    }
                }
            }

            return generatedMutants;
        }
    }
}