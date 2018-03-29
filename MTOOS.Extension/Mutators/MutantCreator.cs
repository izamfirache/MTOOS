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
        private string _className;
        private int _mutantVersion = 0;
        private SyntaxNode _originalRootNode;
        private List<GeneratedMutant> GeneratedMutants;

        public MutantCreator(string className, SyntaxNode originalRootNode)
        {
            _className = className;
            _originalRootNode = originalRootNode;
            GeneratedMutants = new List<GeneratedMutant>();
        }

        public void CreateNewMutant(SyntaxNode mutatedNamespaceRoot)
        {
            var mutantName = string.Format("{0}Mutant{1}", _className, _mutantVersion++);
            var classNameMutator = new ClassIdentifierMutator(_className, mutantName);

            var finalMutantCodeRoot = classNameMutator.Visit(mutatedNamespaceRoot);

            GeneratedMutants.Add(new GeneratedMutant()
            {
                Id = Guid.NewGuid(),
                MutantName = mutantName,
                OriginalClassName = _className,
                OriginalCodeRoot = _originalRootNode,
                MutatedCodeRoot = finalMutantCodeRoot,
                OriginalProgramCode = _originalRootNode.ToFullString(),
                MutatedCode = finalMutantCodeRoot.ToFullString()
            });
        }

        public List<GeneratedMutant> GetMutatedClasses()
        {
            return GeneratedMutants;
        }
    }
}
