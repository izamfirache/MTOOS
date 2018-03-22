using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using MTOOS.Extension.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Mutators
{
    public class MutantCreator
    {
        private List<MutationInformation> MutatedClassNames;
        private Solution2 _currentSolution;
        private string _className;
        private EnvDTE.Project _project;
        private int _mutantVersion = 0;
        private SyntaxNode _originalRootNode;
        private MSBuildWorkspace _workspace;
        public MutantCreator(Solution2 currentSolution, string className, EnvDTE.Project project, SyntaxNode originalRootNode,
            MSBuildWorkspace workspace)
        {
            _currentSolution = currentSolution;
            _className = className;
            _project = project;
            MutatedClassNames = new List<MutationInformation>();
            _originalRootNode = originalRootNode;
            _workspace = workspace;
        }

        public void CreateNewMutant(SyntaxNode mutatedNamespaceRoot)
        {
            string mutatedClassName = string.Format("{0}Mutant{1}", _className, _mutantVersion);
            var classNameMutator = new ClassIdentifierMutator(_className,
                string.Format("{0}Mutant{1}", _className, _mutantVersion));
            _mutantVersion = _mutantVersion + 1;

            var finalMutantCodeRoot = classNameMutator.Visit(mutatedNamespaceRoot);

            string mutatedClassPath = string.Format(@"{0}\..\{1}\{2}\{3}.cs",
                _currentSolution.FileName, _project.Name, "Mutants", mutatedClassName);

            var classTemplatePath = _currentSolution.GetProjectItemTemplate("Class.zip", "csharp");

            foreach (EnvDTE.ProjectItem projItem in _project.ProjectItems)
            {
                if (projItem.Name == "Mutants")
                {
                    projItem.ProjectItems.AddFromTemplate(classTemplatePath, string.Format("{0}.cs", 
                        mutatedClassName));
                    MutatedClassNames.Add(new MutationInformation()
                    {
                        ClassName = _className,
                        MutantName = mutatedClassName,
                        MutatedCode = Formatter.Format(finalMutantCodeRoot, _workspace).ToFullString(),
                        OriginalProgramCode = Formatter.Format(_originalRootNode, _workspace).ToFullString()
                    });
                    break;
                }
            }

            File.WriteAllText(mutatedClassPath, finalMutantCodeRoot.ToFullString(), Encoding.Default);
        }

        public List<MutationInformation> GetMutatedClasses()
        {
            return MutatedClassNames;
        }
    }
}
