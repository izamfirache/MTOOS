using Microsoft.CodeAnalysis;
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

        public SyntaxNode OriginalCodeRoot { get; set; }
        public SyntaxNode MutatedCodeRoot { get; set; }
        
        public string OriginalProgramCode { get; set; }
        public string MutatedCode { get; set; }

        public bool IsCompiled { get; set; }
        public string AssemblyPath { get; set; }

        public override string ToString()
        {
            return MutantName + " - " + Status;
        }
    }
}
