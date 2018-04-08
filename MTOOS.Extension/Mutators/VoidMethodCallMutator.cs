using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;

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

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)//???
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node);

            if (typeInfo.Type.Name.ToLower() == "void")
            {
                var mutatedClassRoot = 
                    _classRootNode.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            return node;
        }
    }
}
