using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Helpers
{
    public class RandomTypeGenerator
    {
        public  ExpressionSyntax ResolveExpressionType(string typeSymbol)
        {
            if (typeSymbol == "int32")
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

            if (typeSymbol == "bool")
            {
                return GetRandomInt() % 2 == 0 ?
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression) :
                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            }

            if (typeSymbol == "custom")
            {
                // TODO: add support for custom types using Roslyn if possible
                // Reflection otherwise
            }
            
            // TODO: think of another possible non-custom types

            return null;
        }

        public ExpressionSyntax GetEmptyValueForType(string typeSymbol)
        {
            if (typeSymbol == "int32")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0));
            }

            if (typeSymbol == "double")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0));
            }

            if (typeSymbol == "float")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0));
            }

            if (typeSymbol == "string")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(""));
            }

            if(typeSymbol == "ienumerable")
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("")); //TODO: create an empty List/Array
            }

            // TODO: think of another possible non-custom types

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
