using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Mutators
{
    public class RelationalAndEqualityOpMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;

        public RelationalAndEqualityOpMutator(SyntaxNode classRootNode, MutantCreator mutantCreator)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
        }
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.None);

            // == becomes !=
            if (token.IsKind(SyntaxKind.EqualsEqualsToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // != becomes ==
            if (token.IsKind(SyntaxKind.ExclamationEqualsToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // <= becomes >
            if (token.IsKind(SyntaxKind.LessThanEqualsToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.GreaterThanToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // >= becomes <
            if (token.IsKind(SyntaxKind.GreaterThanEqualsToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.LessThanToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // < becomes >=
            if (token.IsKind(SyntaxKind.LessThanToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // > becomes <=
            if (token.IsKind(SyntaxKind.GreaterThanToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // create a new mutant class
            if (!newToken.IsKind(SyntaxKind.None))
            {
                var mutatedClassRoot = _classRootNode.ReplaceToken(token, newToken);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            return token;
        }
    }
}
