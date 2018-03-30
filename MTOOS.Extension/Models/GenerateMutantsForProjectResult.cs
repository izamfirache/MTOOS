using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Models
{
    public class GenerateMutantsForProjectResult
    {
        public List<GeneratedMutant> GeneratedMutants { get; set; }
        public List<NamespaceDeclarationSyntax> MutatedSyntaxTrees { get; set; }
    }
}
