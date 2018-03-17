using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;
using MTOOS.Extension.Mutators;
using MTOOS.Extension.TestMutators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MTOOS.Extension.MutationAnalysis
{
    public class UnitTestSuiteMutator
    {
        private Solution2 _currentSolution;
        private RoslynSetupHelper _roslynSetupHelper;
        private Dictionary<string, string> _mutatedClasses;
        public UnitTestSuiteMutator(Solution2 currentSolution, Dictionary<string, string> mutatedClasses)
        {
            _currentSolution = currentSolution;
            _roslynSetupHelper = new RoslynSetupHelper();
            _mutatedClasses = mutatedClasses;
        }

        public void PerformMutationForUnitTestProject(EnvDTE.Project project)
        {
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
                        var unitTestClassName = namespaceClasses.ElementAt(0).Identifier.Value.ToString();

                        foreach (string mutatedClassName in _mutatedClasses.Keys)
                        {
                            _mutatedClasses.TryGetValue(mutatedClassName, out string className);

                            //rethink this!! -- determine which one is the unit test project
                            if (unitTestClassName.Contains(className)) 
                            {
                                var mutatedUnitTestClassName = string.Format("{0}UnitTestMutant", mutatedClassName);
                                var unitTestClassMutator = new UnitTestClassMutator(className, mutatedClassName,
                                    unitTestClassName, mutatedUnitTestClassName);
                                var mutatedUnitTestClassNsRoot = unitTestClassMutator.Visit(namespaceTreeRoot);

                                CreateNewUnitTestClassMutant(project, mutatedUnitTestClassNsRoot.ToFullString(),
                                    mutatedUnitTestClassName);
                            }
                        }
                    }
                }
            }
        }

        private void CreateNewUnitTestClassMutant(EnvDTE.Project project, string mutatedCode, string mutatedClassName)
        {
            string mutatedUnitTestClassPath = string.Format(@"{0}\{1}\{2}\{3}.cs",
                Path.GetDirectoryName(_currentSolution.FileName), project.Name, 
                "MutationCompiledUnits", mutatedClassName);

            var classTemplatePath = _currentSolution.GetProjectItemTemplate("Class.zip", "csharp");

            foreach (EnvDTE.ProjectItem projItem in project.ProjectItems)
            {
                if (projItem.Name == "MutationCompiledUnits")
                {
                    projItem.ProjectItems.AddFromTemplate(classTemplatePath, 
                        string.Format("{0}.cs", mutatedClassName));
                    break;
                }
            }

            File.WriteAllText(mutatedUnitTestClassPath, mutatedCode, Encoding.Default);
        }
    }
}
