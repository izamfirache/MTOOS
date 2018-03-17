using EnvDTE80;
using Microsoft.CodeAnalysis;
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
        private Dictionary<string, string> MutatedClassNames;
        private Solution2 _currentSolution;
        private string _className;
        private EnvDTE.Project _project;
        private int _mutantVersion = 0;
        public MutantCreator(Solution2 currentSolution, string className, EnvDTE.Project project)
        {
            _currentSolution = currentSolution;
            _className = className;
            _project = project;
            MutatedClassNames = new Dictionary<string, string>();
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
                    MutatedClassNames.Add(mutatedClassName, _className);
                    break;
                }
            }

            File.WriteAllText(mutatedClassPath, finalMutantCodeRoot.ToFullString(), Encoding.Default);
        }

        public Dictionary<string, string> GetMutatedClasses()
        {
            return MutatedClassNames;
        }
    }
}
