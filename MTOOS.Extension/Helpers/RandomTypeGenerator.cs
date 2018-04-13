using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Helpers
{
    public class RandomTypeGenerator
    {
        private RoslynSetupHelper _roslynHelper;
        private List<Class> _projectClasses;

        public RandomTypeGenerator(List<Class> projectClasses)
        {
            _roslynHelper = new RoslynSetupHelper();
            _projectClasses = projectClasses;
        }

        public ExpressionSyntax ResolveType(string typeName)
        {
            if (IsPrimitiveType(typeName.ToLower()))
            {
                var primitiveType = ResolvePrimitiveType(typeName.ToLower());
                return primitiveType ?? null;
            }
            else
            {
                var customType = ResolveCustomType(typeName);
                return customType ?? null;
            }
        }

        public ExpressionSyntax ResolvePrimitiveType(string typeSymbol)
        {
            switch (typeSymbol)
            {
                case "int32":
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(GetRandomInt()));
                case "int":
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(GetRandomInt()));
                case "double":
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(GetRandomDouble()));
                case "float":
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(GetRandomFloat()));
                case "string":
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(GetRandomString()));
                case "bool":
                    return GetRandomInt() % 2 == 0 ?
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression) :
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                case "boolean":
                    return GetRandomInt() % 2 == 0 ?
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression) :
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            }

            return null;
        }

        private ObjectCreationExpressionSyntax ResolveCustomType(string typeName)
        {
            if ((_projectClasses.Any(c => typeName.Contains(string.Format(".{0}", c.Name))) 
                && !typeName.Contains("List") && !typeName.Contains("Dictionary"))
                || _projectClasses.Any(c => c.Name == typeName))
            {
                var classInfo =
                    _projectClasses.Where(
                        c => typeName.Contains(string.Format(".{0}", c.Name))
                        || c.Name == typeName).FirstOrDefault();
                
                var ctorParameters = 
                    new SyntaxNodeOrToken[classInfo.Constructor.Parameters.Count 
                        + classInfo.Constructor.Parameters.Count - 1]; // includes commas
                int position = 0;
                foreach(MethodParameter param in classInfo.Constructor.Parameters)
                {
                    if (IsPrimitiveType(param.Type.ToLower()))
                    {
                        ctorParameters[position] = 
                            SyntaxFactory.Argument(ResolvePrimitiveType(param.Type.ToLower()));
                    }
                    else
                    {
                        ctorParameters[position] =
                            SyntaxFactory.Argument(ResolveCustomType(param.Type));
                    }

                    if(position != classInfo.Constructor.Parameters.Count) //not last ctor param
                    {
                        ctorParameters[position + 1] =
                            SyntaxFactory.Token(SyntaxKind.CommaToken);
                    }

                    position = position + 2;
                }

                return SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName(typeName))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList<ArgumentSyntax>(ctorParameters)));
            }
            else
            {
                //an array -- int[], string[] --  (TODO: find a better way to do this)
                if (typeName.Contains('[') && typeName.Contains(']'))
                {
                    return
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(typeName))
                            .WithInitializer(SyntaxFactory.InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression));
                }
                else
                {
                    //a list or dictionary
                    return
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(typeName))
                            .WithArgumentList(SyntaxFactory.ArgumentList());
                }
            }
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
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new StringBuilder();
            Random rnd = new Random();

            for (var i = 0; i < rnd.Next(5, 10); i++)
            {
                var c = pool[rnd.Next(0, pool.Length)];
                builder.Append(c);
            }
            return builder.ToString();
        }

        private bool IsPrimitiveType(string typeName)
        {
            if (typeName == "int32"
                || typeName == "int"
                || typeName == "double"
                || typeName == "float"
                || typeName == "string"
                || typeName == "bool"
                || typeName == "boolean")
                return true;

            return false;
        }
    }
}