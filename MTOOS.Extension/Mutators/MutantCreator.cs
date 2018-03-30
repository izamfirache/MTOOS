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
        private SyntaxNode _originalClassRootNode;
        private List<GeneratedMutant> GeneratedMutants;

        public MutantCreator(string className, SyntaxNode originalClassRootNode)
        {
            _className = className;
            _originalClassRootNode = originalClassRootNode;
            GeneratedMutants = new List<GeneratedMutant>();
        }

        public void CreateNewMutant(SyntaxNode classSyntaxNode)
        {
            var mutantName = string.Format("{0}Mutant{1}", _className, _mutantVersion++);
            var classNameMutator = new ClassIdentifierMutator(_className, mutantName);

            var finalMutantCodeRoot = classNameMutator.Visit(classSyntaxNode);

            GeneratedMutants.Add(new GeneratedMutant()
            {
                Id = Guid.NewGuid(),
                MutantName = mutantName,
                OriginalClassName = _className,
                OriginalCodeRoot = _originalClassRootNode,
                MutatedCodeRoot = finalMutantCodeRoot,
                OriginalProgramCode = _originalClassRootNode.ToFullString(),
                MutatedCode = finalMutantCodeRoot.ToFullString()
            });
        }

        public List<GeneratedMutant> GetMutatedClasses()
        {
            return GeneratedMutants;
        }
    }
}
