using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;
using MTOOS.Extension.Models;
using System.Collections.Generic;
using System.Windows;

namespace MTOOS.Extension.Mutators
{
    public class VariableDeclarationMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;
        private RandomTypeGenerator _randomTypeGenerator;

        public VariableDeclarationMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel, List<Class> projectClasses)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
            _randomTypeGenerator = new RandomTypeGenerator(projectClasses);
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var variableDeclarationSyntax = node.Declaration;
            VariableDeclaratorSyntax variableDeclarator = variableDeclarationSyntax.Variables[0];

            var nodeSemanticModel = 
                _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbolInfo = nodeSemanticModel.GetSymbolInfo(variableDeclarationSyntax.Type);
            var typeSymbol = symbolInfo.Symbol;
            
            var replaceValueSyntaxNode =
                _randomTypeGenerator.ResolveType(typeSymbol.ToString());

            if (replaceValueSyntaxNode != null)
            {

                var newLocalVariableDeclarationNode =
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            variableDeclarationSyntax.Type)
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                SyntaxFactory.VariableDeclarator(
                                    variableDeclarator.Identifier)
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(replaceValueSyntaxNode)))))
                    .NormalizeWhitespace();

                var mutatedClassRoot = _classRootNode.ReplaceNode(node, newLocalVariableDeclarationNode);
                _mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            }

            return node;
        }
    }
}
