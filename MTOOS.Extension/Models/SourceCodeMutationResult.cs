using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Models
{
    public class SourceCodeMutationResult
    {
        public List<GeneratedMutant> GeneratedMutants { get; set; }
        public string OutputPath { get; set; }

        public List<UsingDirectiveSyntax> Usings { get; set; }
    }
}
