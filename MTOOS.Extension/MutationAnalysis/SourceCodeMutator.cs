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

        public Dictionary<string, string> PerformMutationAnalysisOnProject(EnvDTE.Project project)
        {
            var MutatedClassNames = new Dictionary<string, string>();
            var solution = _roslynSetupHelper.GetSolutionToAnalyze(
                _roslynSetupHelper.CreateWorkspace(), _currentSolution.FileName);
            var projectAssembly = _roslynSetupHelper.GetProjectAssembly(
                _roslynSetupHelper.GetProjectToAnalyze(solution, project.Name));
            var projectSemanticModel = _roslynSetupHelper.GetProjectSemanticModel(
                _roslynSetupHelper.GetProjectToAnalyze(solution, project.Name));

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
                        var mutantCreator = new MutantCreator(_currentSolution, className, project);

                        //apply additive and multiplicative mutations
                        var mathOperatorMutator = new AdditiveAndMultiplicativeOp
                            (namespaceTreeRoot, mutantCreator);
                        mathOperatorMutator.Visit(namespaceTreeRoot);

                        //apply realtional and equity mutations
                        var relationalAndEquityOp = new RelationalAndEqualityOp
                            (namespaceTreeRoot, mutantCreator);
                        relationalAndEquityOp.Visit(namespaceTreeRoot);

                        //apply assignment expression mutation
                        var assignmentExprMutator = new AssignmentExprMutator
                            (namespaceTreeRoot, mutantCreator, projectSemanticModel);
                        assignmentExprMutator.Visit(namespaceTreeRoot);

                        ////apply this keyword statement deletion muttion
                        //var thisKeywordStatementDeletion = new ThisKeywordStatementsDeletion
                        //    (namespaceTreeRoot, mutantCreator);
                        //thisKeywordStatementDeletion.Visit(namespaceTreeRoot);

                        MutatedClassNames = 
                            MutatedClassNames.Concat(mutantCreator.GetMutatedClasses())
                            .ToDictionary(x => x.Key, x => x.Value);
                    }
                }
            }

            return MutatedClassNames;
        }
    }
}
