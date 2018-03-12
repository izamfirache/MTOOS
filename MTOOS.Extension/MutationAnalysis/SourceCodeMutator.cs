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
                        var className = namespaceClasses.ElementAt(0).Identifier.Value.ToString();

                        //additive operators (+, -, /, *, %)
                        var mathOperatorMutator = new AritmeticOperatorMutator(className, namespaceTreeRoot, project, _currentSolution);
                        mathOperatorMutator.Visit(namespaceTreeRoot);

                        MutatedClassNames = 
                            MutatedClassNames.Concat(mathOperatorMutator.GetMutatedClasses())
                            .ToDictionary(x => x.Key, x => x.Value);
                    }
                }
            }
            return MutatedClassNames;
        }
    }
}
