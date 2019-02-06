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
    public class MathOperatorsMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;

        public MathOperatorsMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
        }
        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            SyntaxToken expressionOperatorToken = node.OperatorToken;
            SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.None);

            // - becomes +
            if (expressionOperatorToken.IsKind(SyntaxKind.MinusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.PlusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }
            
            // + becomes -
            if (expressionOperatorToken.IsKind(SyntaxKind.PlusToken))
            {
                //make sure it is not a string concatenation expression
                //if you want to ask about the SymbolInfo for a given Node, get the semantic model for that node first
                var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
                var typeInfo = nodeSemanticModel.GetTypeInfo(node);

                if (typeInfo.Type.Name != "String")
                {
                    newToken = SyntaxFactory.Token(SyntaxKind.MinusToken)
                        .WithTrailingTrivia(SyntaxFactory.Space);
                }
                //TODO: find a way to mutate string concatenation expressions as well
            }

            // / becomes *
            if (expressionOperatorToken.IsKind(SyntaxKind.SlashToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }
            
            // * becomes /
            if (expressionOperatorToken.IsKind(SyntaxKind.AsteriskToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.SlashToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // % becomes *
            if (expressionOperatorToken.IsKind(SyntaxKind.PercentToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // & becomes |
            if (expressionOperatorToken.IsKind(SyntaxKind.AmpersandToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.BarToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // | becomes &
            if (expressionOperatorToken.IsKind(SyntaxKind.BarToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AmpersandToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // ^ becomes &
            if (expressionOperatorToken.IsKind(SyntaxKind.CaretToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AmpersandToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // << becomes >>
            if (expressionOperatorToken.IsKind(SyntaxKind.LessThanLessThanToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // >> becomes <<
            if (expressionOperatorToken.IsKind(SyntaxKind.GreaterThanGreaterThanToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.LessThanLessThanToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (!newToken.IsKind(SyntaxKind.None))
            {
                var mutatedBinaryExressionNode = node.ReplaceToken(expressionOperatorToken, newToken);
                var mutatedClassRoot = _classRootNode.ReplaceNode(node, mutatedBinaryExressionNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            return node;
        }

        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            SyntaxToken postFixUnaryOperator = node.OperatorToken;
            SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.None);

            // ++ becomes --
            if (postFixUnaryOperator.IsKind(SyntaxKind.PlusPlusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.MinusMinusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // -- becomes ++
            if (postFixUnaryOperator.IsKind(SyntaxKind.MinusMinusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.PlusPlusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (!newToken.IsKind(SyntaxKind.None))
            {
                var mutatedBinaryExressionNode = node.ReplaceToken(postFixUnaryOperator, newToken);
                var mutatedClassRoot = _classRootNode.ReplaceNode(node, mutatedBinaryExressionNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }
            return node;
        }

        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            SyntaxToken preFixUnaryOperator = node.OperatorToken;
            SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.None);

            // ++ becomes --
            if (preFixUnaryOperator.IsKind(SyntaxKind.PlusPlusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.MinusMinusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            // -- becomes ++
            if (preFixUnaryOperator.IsKind(SyntaxKind.MinusMinusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.PlusPlusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (!newToken.IsKind(SyntaxKind.None))
            {
                var mutatedBinaryExressionNode = node.ReplaceToken(preFixUnaryOperator, newToken);
                var mutatedClassRoot = _classRootNode.ReplaceNode(node, mutatedBinaryExressionNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }
            return node;
        }
    }
}