using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;
using MTOOS.Extension.Models;
using System.Collections.Generic;
using System.Windows;

namespace MTOOS.Extension.Mutators
{
    public class LocalVariableDeclarationMutator : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private SemanticModel _semanticModel;
        private RandomTypeGenerator _randomTypeGenerator;

        public LocalVariableDeclarationMutator(SyntaxNode classRootNode, MutantCreator mutantCreator,
            SemanticModel semanticModel, RandomTypeGenerator randomTypeGenerator)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _semanticModel = semanticModel;
            _randomTypeGenerator = randomTypeGenerator;
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var variableDeclarationSyntax = node.Declaration;
            VariableDeclaratorSyntax variableDeclarator = variableDeclarationSyntax.Variables[0];

            var nodeSemanticModel = 
                _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbolInfo = nodeSemanticModel.GetSymbolInfo(variableDeclarationSyntax.Type);
            var typeSymbol = symbolInfo.Symbol;

            ExpressionSyntax replaceValueSyntaxNode;
            if (typeSymbol.IsAbstract)
            {
                //get a type that implements that interface
                string toBeResolvedType =
                    _randomTypeGenerator.GetTypeForInterface(typeSymbol.ToString());

                replaceValueSyntaxNode = toBeResolvedType != null ?
                _randomTypeGenerator.ResolveType(toBeResolvedType) : null;
            }
            else
            {
                replaceValueSyntaxNode =
                _randomTypeGenerator.ResolveType(typeSymbol.ToString());
            }

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
