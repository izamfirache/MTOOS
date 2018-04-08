using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;

namespace MTOOS.Extension.Mutators
{
    public class AssignmentExprMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;
        private RandomTypeGenerator _randomTypeGenerator;

        public AssignmentExprMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
            _randomTypeGenerator = new RandomTypeGenerator();
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node);
            var replaceValueSyntaxNode = 
                _randomTypeGenerator.ResolveExpressionType(typeInfo.Type.Name.ToLower());
                
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
    }
}
