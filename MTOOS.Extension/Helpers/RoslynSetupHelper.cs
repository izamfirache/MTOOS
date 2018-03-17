using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Helpers
{
    public class RoslynSetupHelper
    {
        public MSBuildWorkspace CreateWorkspace()
        {
            // start Roslyn workspace
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            return workspace;
        }

        public Solution GetSolutionToAnalyze(MSBuildWorkspace workspace, string pathToSolution)
        {
            // open solution we want to analyze
            Solution solutionToAnalyze = workspace.OpenSolutionAsync(pathToSolution).Result;
            return solutionToAnalyze;
        }

        public Project GetProjectToAnalyze(Solution solution, string projectName)
        {
            // get the project we want to analyze out
            Project projectToAnalyze = solution.Projects.Where((proj) => proj.Name == projectName).First();
            return projectToAnalyze;
        }

        public Compilation GetProjectAssembly(Project project)
        {
            // get the project's compiled assembly
            Compilation projectCompiledAssembly = project.GetCompilationAsync().Result;
            return projectCompiledAssembly;
        }

        public SemanticModel GetProjectSemanticModel(Project project)
        {
            var document = project.Documents.First();
            var rootNode = document.GetSyntaxRootAsync().Result;
            var semanticModel = document.GetSemanticModelAsync().Result;

            return semanticModel;
        }
    }
}
