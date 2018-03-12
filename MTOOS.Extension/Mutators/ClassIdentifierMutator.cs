using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Mutators
{
    public class ClassIdentifierMutator : CSharpSyntaxRewriter
    {
        private string _className;
        private string _mutatedClassName;
        public ClassIdentifierMutator(string className, string mutatedClassName)
        {
            _className = className;
            _mutatedClassName = mutatedClassName;
        }
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                if (token.Value.ToString() == _className)
                {
                    return SyntaxFactory.Identifier(_mutatedClassName)
                        .WithTrailingTrivia(SyntaxFactory.Space);
                }
            }

            return token;
        }
    }
}