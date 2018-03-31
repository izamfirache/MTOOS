using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MTOOS.Extension.Mutators
{
    public class AdditiveAndMultiplicativeOp : CSharpSyntaxRewriter
    {
        private SyntaxNode _namespaceRootNode;
        private MutantCreator _mutantCreator;

        public AdditiveAndMultiplicativeOp(SyntaxNode namespaceRootNode, MutantCreator mutantCreator)
        {
            _namespaceRootNode = namespaceRootNode;
            _mutantCreator = mutantCreator;
        }
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.None);
            if (token.IsKind(SyntaxKind.MinusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.PlusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }
            
            //TODO: exclude string concatenation
            if (token.IsKind(SyntaxKind.PlusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.MinusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (token.IsKind(SyntaxKind.SlashToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }
            
            if (token.IsKind(SyntaxKind.AsteriskToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.SlashToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (token.IsKind(SyntaxKind.PercentToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (!newToken.IsKind(SyntaxKind.None))
            {
                var mutatedNamespaceRoot = _namespaceRootNode.ReplaceToken(token, newToken);
                _mutantCreator.CreateNewMutant(mutatedNamespaceRoot, false);
            }

            return token;
        }
    }
}