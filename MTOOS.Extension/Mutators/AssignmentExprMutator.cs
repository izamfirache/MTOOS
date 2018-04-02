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
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;

        public AssignmentExprMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node);
            var replaceValueSyntaxNode = ResolveExpressionType(typeInfo.Type.Name.ToLower());
                
            if (replaceValueSyntaxNode != null)
            {
                var newAssignmentNode =
                    SyntaxFactory.AssignmentExpression(
                        node.Kind(),
                        node.Left,
                        replaceValueSyntaxNode).NormalizeWhitespace();

                var mutatedClassRoot = _classRootNode.ReplaceNode(node, newAssignmentNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            return node;
        }

        private ExpressionSyntax ResolveExpressionType(string typeSymbol)
        {
            if(typeSymbol == "int32")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(GetRandomInt()));
            }

            if (typeSymbol == "double")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(GetRandomDouble()));
            }

            if (typeSymbol == "float")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(GetRandomFloat()));
            }

            if (typeSymbol == "string")
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

        private long GetRandomLong()
        {
            long min = 10000000000001;
            long max = 99999999999999;
            Random random = new Random();
            long randomNumber = min + random.Next() % (max - min);

            return randomNumber;
        }

        private float GetRandomFloat()
        {
            Random rnd = new Random();
            double sample = rnd.NextDouble();
            double range = float.MaxValue - float.MinValue;
            double scaled = (sample * range) + float.MinValue;
            float floatNumber = (float)scaled;

            return floatNumber;
        }

        private double GetRandomDouble()
        {
            Random rnd = new Random();
            return rnd.NextDouble();
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
