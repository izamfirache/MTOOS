using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Mutators
{
    public class RelationalAndEqualityOp : CSharpSyntaxRewriter
    {
        private SyntaxNode _namespaceRootNode;
        private MutantCreator _mutantCreator;

        public RelationalAndEqualityOp(SyntaxNode namespaceRootNode, MutantCreator mutantCreator)
        {
            _namespaceRootNode = namespaceRootNode;
            _mutantCreator = mutantCreator;
        }
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            // < becomes >
            SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.None);
            if (token.IsKind(SyntaxKind.LessThanToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.GreaterThanToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // > becomes <
            if (token.IsKind(SyntaxKind.GreaterThanToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.LessThanToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // <= becomes >=
            if (token.IsKind(SyntaxKind.LessThanEqualsToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // >= becomes <=
            if (token.IsKind(SyntaxKind.GreaterThanEqualsToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

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

            // && becomes ||
            if (token.IsKind(SyntaxKind.AmpersandAmpersandToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.BarBarToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // || becomes &&
            if (token.IsKind(SyntaxKind.BarBarToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AmpersandAmpersandToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // mutant creation
            if (!newToken.IsKind(SyntaxKind.None))
            {
                var mutatedNamespaceRoot = _namespaceRootNode.ReplaceToken(token, newToken);
                _mutantCreator.CreateNewMutant(mutatedNamespaceRoot);
            }

            return token;
        }
    }
}
