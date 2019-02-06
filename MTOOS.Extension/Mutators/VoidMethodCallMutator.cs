using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;
using System;

namespace MTOOS.Extension.Mutators
{
    public class VoidMethodCallMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;

        public VoidMethodCallMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node);

            if (typeInfo.Type.Name.ToLower() == "void")
            {
                try
                {
                    //get the parent node -- usually an ExpressionStatement node
                    var voidInvocationParentNode = node.Parent;
                    var mutatedClassRoot =
                        _classRootNode.RemoveNode(voidInvocationParentNode, SyntaxRemoveOptions.KeepNoTrivia);
                    _mutantCreator.CreateNewMutant(mutatedClassRoot, true);
                }
                catch (Exception e)
                {

                }
            }

            return node;
        }
    }
}
