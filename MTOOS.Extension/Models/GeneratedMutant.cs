using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Models
{
    public class GeneratedMutant
    {
        public Guid Id { get; set; }
        public string MutantName { get; set; }
        public string OriginalClassName { get; set; }
        public string Status { get; set; }
        public string MutatorType { get; set; }

        public ClassDeclarationSyntax OriginalCodeRoot { get; set; }
        public ClassDeclarationSyntax MutatedCodeRoot { get; set; }
        
        public string OriginalProgramCode { get; set; }
        public string MutatedCode { get; set; }

        public bool IsCompiled { get; set; }
        public string AssemblyPath { get; set; }

        public bool HaveDeletedStatement { get; set; }

        public override string ToString()
        {
            return MutantName + " - " + MutatorType;
        }
    }
}
