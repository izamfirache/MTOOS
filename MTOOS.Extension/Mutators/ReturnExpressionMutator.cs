using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;
using System.Collections.Generic;
using MTOOS.Extension.Models;

namespace MTOOS.Extension.Mutators
{
    public class ReturnExpressionMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;
        private RandomTypeGenerator _randomTypeGenerator;

        public ReturnExpressionMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel, RandomTypeGenerator randomTypeGenerator)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
            _randomTypeGenerator = randomTypeGenerator;
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node.Expression);

            ExpressionSyntax replaceValueSyntaxNode;
            if (typeInfo.Type != null)
            {
                replaceValueSyntaxNode =
                        _randomTypeGenerator.ResolveType(typeInfo.Type.ToString());

                //if (typeInfo.Type.IsAbstract) // TODO: rethink this, might be abstract class, not interface
                //{
                //    //get a type that implements that interface
                //    string toBeResolvedType =
                //        _randomTypeGenerator.GetTypeForInterface(typeInfo.Type.Name);

                //    replaceValueSyntaxNode = toBeResolvedType != null ?
                //    _randomTypeGenerator.ResolveType(toBeResolvedType) : null;
                //}
                //else
                //{
                //    replaceValueSyntaxNode =
                //        _randomTypeGenerator.ResolveType(typeInfo.Type.ToString());
                //}
            }
            else
            {
                replaceValueSyntaxNode = null;
            }

            //replace with random value
            if (replaceValueSyntaxNode != null)
            {
                var newReturnStatemenNode =
                    SyntaxFactory.ReturnStatement(replaceValueSyntaxNode)
                        .NormalizeWhitespace();

                var mutatedClassRoot = _classRootNode.ReplaceNode(node, newReturnStatemenNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }
            else
            {
                if (typeInfo.Type == null)
                {
                    return node;
                }
                if (typeInfo.Type != null && typeInfo.Type.Name == "T")
                {
                    return node;
                }

                if (typeInfo.Type.BaseType != null && typeInfo.Type.BaseType.Name == "Enum")
                {
                    return node;
                }

                // replace with return null;
                var nullReturnStatementNode = SyntaxFactory.ReturnStatement(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                        .NormalizeWhitespace();

                var mutatedClassRoot = _classRootNode.ReplaceNode(node, nullReturnStatementNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            return node;
        }
    }
}
