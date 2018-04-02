using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private ClassDeclarationSyntax _originalClassRootNode;
        private List<GeneratedMutant> GeneratedMutants;
        public string MutatorType { get; set; }

        public MutantCreator(string className, ClassDeclarationSyntax originalClassRootNode)
        {
            _className = className;
            _originalClassRootNode = originalClassRootNode;
            GeneratedMutants = new List<GeneratedMutant>();
        }

        public void CreateNewMutant(SyntaxNode classSyntaxNode, bool isDeletionOperator)
        {
            var mutantName = string.Format("{0}Mutant{1}", _className, _mutantVersion++);
            var classNameMutator = new ClassIdentifierMutator(_className, mutantName);

            var finalMutantCodeRoot = (ClassDeclarationSyntax)classNameMutator.Visit(classSyntaxNode);

            if (!isDeletionOperator)
            {
                finalMutantCodeRoot = finalMutantCodeRoot.NormalizeWhitespace();
                _originalClassRootNode = _originalClassRootNode.NormalizeWhitespace();
            }

            GeneratedMutants.Add(new GeneratedMutant()
            {
                Id = Guid.NewGuid(),
                MutantName = mutantName,
                OriginalClassName = _className,
                OriginalCodeRoot = _originalClassRootNode,
                MutatedCodeRoot = finalMutantCodeRoot,
                OriginalProgramCode = _originalClassRootNode.ToFullString(),
                MutatedCode = finalMutantCodeRoot.ToFullString(),
                HaveDeletedStatement = isDeletionOperator,
                MutatorType = MutatorType
            });
        }

        public List<GeneratedMutant> GetMutatedClasses()
        {
            return GeneratedMutants;
        }
    }
}
