using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;
using MTOOS.Extension.Models;
using System.Collections.Generic;

namespace MTOOS.Extension.Mutators
{
    public class AssignmentExprMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;
        private RandomTypeGenerator _randomTypeGenerator;

        public AssignmentExprMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel, RandomTypeGenerator randomTypeGenerator)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
            _randomTypeGenerator = randomTypeGenerator;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node);

            ExpressionSyntax replaceValueSyntaxNode;
            if (typeInfo.Type.IsAbstract) // TODO: rethink this, might be abstract class, not interface
            {
                //get a type that implements that interface
                string toBeResolvedType = 
                    _randomTypeGenerator.GetTypeForInterface(typeInfo.Type.Name);
                
                replaceValueSyntaxNode = toBeResolvedType != null ?
                _randomTypeGenerator.ResolveType(toBeResolvedType) : null;
            }
            else
            {
                replaceValueSyntaxNode =
                _randomTypeGenerator.ResolveType(typeInfo.Type.Name);
            }
            
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
