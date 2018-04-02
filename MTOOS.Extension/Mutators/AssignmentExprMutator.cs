using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Mutators
{
    public class AssignmentExprMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _namespaceRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;

        public AssignmentExprMutator(SyntaxNode namespaceRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel)
        {
            _namespaceRootNode = namespaceRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
        }

        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var variableType = node.Type;
            var variables = node.Variables;

            var symbolInfo = _semanticModel.GetSymbolInfo(node.Type);
            var typeSymbol = symbolInfo.Symbol.ToString();

            var replaceValueSyntaxNode = ResolveExpressionType(typeSymbol);

            if (replaceValueSyntaxNode != null)
            {
                // if not empty take the first one ??
                VariableDeclaratorSyntax variableDeclaratorSyntax = variables.First();
                var variableName = variableDeclaratorSyntax.Identifier.Value.ToString();
                var variableAssignmentStatement = variableDeclaratorSyntax.Initializer;

                var variableDeclarationWithoutAssignmentNode = SyntaxFactory.VariableDeclaration(variableType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(variableName))
                        .WithTrailingTrivia(SyntaxFactory.Space)
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(replaceValueSyntaxNode
                            .WithLeadingTrivia(SyntaxFactory.Space))));

                var mutatedNamespaceRoot = _namespaceRootNode.ReplaceNode(node, variableDeclarationWithoutAssignmentNode);
                _mutantCreator.CreateNewMutant(mutatedNamespaceRoot, false);
            }
            else
            {
                //variable declaration without assignment statement -- nothing to mutate
                return node;
            }

            return node;
        }

        private ExpressionSyntax ResolveExpressionType(string typeSymbol)
        {
            if(typeSymbol == "int")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(GetRandomInt()));
            }

            if(typeSymbol == "string")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(GetRandomString()));
            }

            if(typeSymbol == "bool")
            {
                return GetRandomInt() % 2 == 0 ? 
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression) :
                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            }

            return null;
        }

        private int GetRandomInt()
        {
            Random rnd = new Random();
            return rnd.Next(0, 10000);
        }

        private string GetRandomString()
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789!?@#$%^&*()[]{}";
            var builder = new StringBuilder();
            Random rnd = new Random();

            for (var i = 0; i < rnd.Next(15, 20); i++)
            {
                var c = pool[rnd.Next(0, pool.Length)];
                builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
