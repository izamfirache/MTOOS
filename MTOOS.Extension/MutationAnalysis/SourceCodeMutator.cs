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

            //get info about source code project's types
            var projectClasses = new List<Class>();
            foreach (var syntaxTree in projectAssembly.SyntaxTrees)
            {
                var classVisitor = new ClassVisitor();
                classVisitor.Visit(syntaxTree.GetRoot());
                projectClasses.AddRange(classVisitor.ProjectClasses);
            }

            var generatedMutants = 
                GenerateMutantsForProject(projectAssembly, projectSemanticModel, 
                    options, workspace, projectClasses, solution, projectToAnalyze.OutputFilePath);
            
            return new SourceCodeMutationResult()
            {
                GeneratedMutants = generatedMutants,
                OutputPath = projectToAnalyze.OutputFilePath
            };
        }

        private List<GeneratedMutant> GenerateMutantsForProject(
            Compilation projectAssembly,
            SemanticModel projectSemanticModel,
            List<string> options,
            Workspace workspace,
            List<Class> projectClasses,
            Solution solution,
            string projectOutputFilePath)
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

                        // might be null if the project is not compiled ???
                        var assembly = Assembly.LoadFile(projectOutputFilePath); 
                        Type[] _assemblyExportedTypes = assembly.GetExportedTypes();
                        var typeResolver = new RandomTypeGenerator(projectClasses, _assemblyExportedTypes);

                        //mutate boundary operators
                        if (options.Contains("1"))
                        {
                            mutantCreator.MutatorType = "BOM";
                            var boundaryOpMutator = new BoundaryOpMutator
                                (classSyntaxNode, mutantCreator);
                            boundaryOpMutator.Visit(classSyntaxNode);
                        }

                        //nagate relational and equality operators
                        if (options.Contains("2"))
                        {
                            mutantCreator.MutatorType = "REOM";
                            var relationalAndEqOpMutator = new RelationalAndEqualityOpMutator
                                (classSyntaxNode, mutantCreator);
                            relationalAndEqOpMutator.Visit(classSyntaxNode);
                        }

                        //mutate non basic conditionals
                        if (options.Contains("3"))
                        {
                            //replace complex boolean expressions with true or false
                            mutantCreator.MutatorType = "RNBCM";
                            var conditionaleMutator = new RemoveNonBasicConditionalsMutator
                                (classSyntaxNode, mutantCreator);
                            conditionaleMutator.Visit(classSyntaxNode);
                        }

                        //mutate math operators
                        if (options.Contains("4"))
                        {
                            mutantCreator.MutatorType = "MOM";
                            var mathOperatorMutator = new MathOperatorsMutator
                                (classSyntaxNode, mutantCreator, projectSemanticModel);
                            mathOperatorMutator.Visit(classSyntaxNode);
                        }

                        //mutate assignment expressions
                        if (options.Contains("5"))
                        {
                            //replace an assignment expression right part with the
                            //default value for the desired type
                            mutantCreator.MutatorType = "AEM";
                            var assignmentExprMutator = new AssignmentExprMutator
                                (classSyntaxNode, mutantCreator, projectSemanticModel, typeResolver);
                            assignmentExprMutator.Visit(classSyntaxNode);
                        }

                        //mutate return statements
                        if (options.Contains("6"))
                        {
                            //replace an return statement 
                            //value with a random generated value for that type
                            mutantCreator.MutatorType = "REM";
                            var returnStatementMutator = new ReturnExpressionMutator
                                (classSyntaxNode, mutantCreator, projectSemanticModel, typeResolver);
                            returnStatementMutator.Visit(classSyntaxNode);
                        }

                        //mutate void method calls
                        if (options.Contains("7"))
                        {
                            //remove void mthod calls statements
                            mutantCreator.MutatorType = "VMCM";
                            var voidMethodCallMutator = new VoidMethodCallMutator
                                (classSyntaxNode, mutantCreator, projectSemanticModel);
                            voidMethodCallMutator.Visit(classSyntaxNode);
                        }

                        if (options.Contains("8") && !options.Contains("5"))
                        {
                            //deletes all class's members assignments (global variables assignments)
                            mutantCreator.MutatorType = "CMAD";
                            var classFields = namespaceClasses.ElementAt(0)
                            .DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

                            var classFieldsIdentifiers = new List<string>();
                            foreach (FieldDeclarationSyntax field in classFields)
                            {
                                classFieldsIdentifiers.Add(
                                    field.Declaration.Variables.First().Identifier.ToString());
                            }

                            var classMembersAssignmentsDeletion = new ClassMemberAssignDel
                                (classSyntaxNode, mutantCreator, classFieldsIdentifiers);
                            classMembersAssignmentsDeletion.Visit(classSyntaxNode);
                        }

                        //mutate local variable declaration with initializer statements
                        if (options.Contains("9"))
                        {
                            mutantCreator.MutatorType = "LVDM";
                            var voidMethodCallMutator = new LocalVariableDeclarationMutator
                                (classSyntaxNode, mutantCreator, projectSemanticModel, typeResolver);
                            voidMethodCallMutator.Visit(classSyntaxNode);
                        }

                        generatedMutants.AddRange(mutantCreator.GetMutatedClasses());
                    }
                }
            }

            return generatedMutants;
        }
    }
}