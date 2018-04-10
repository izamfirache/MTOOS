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
            SemanticModel semanticModel, List<Class> projectClasses)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
            _randomTypeGenerator = new RandomTypeGenerator(projectClasses);
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node);
            var randomValueSyntaxNode =
                _randomTypeGenerator.ResolveType(typeInfo.Type.Name.ToLower());

            //TODO: also replace the return value with an empty value for that type
            //0 for numbers, '' for string, empty lists, empty arrays.

            //replace with random value
            if (randomValueSyntaxNode != null)
            {
                var newReturnStatemenNode =
                    SyntaxFactory.ReturnStatement(randomValueSyntaxNode)
                        .NormalizeWhitespace();

                var mutatedClassRoot = _classRootNode.ReplaceNode(node, newReturnStatemenNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            //replace with empty value
            var emptyValueSyntaxNode =
                _randomTypeGenerator.GetEmptyValueForType(typeInfo.Type.Name.ToLower());
            if (emptyValueSyntaxNode != null)
            {
                var newReturnStatemenNode =
                    SyntaxFactory.ReturnStatement(emptyValueSyntaxNode)
                        .NormalizeWhitespace();

                var mutatedClassRoot = _classRootNode.ReplaceNode(node, newReturnStatemenNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            return node;
        }
    }
}
