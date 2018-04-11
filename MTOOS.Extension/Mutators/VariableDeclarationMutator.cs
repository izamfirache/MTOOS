﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Helpers;
using MTOOS.Extension.Models;
using System.Collections.Generic;

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

        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var nodeSemanticModel = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
            var typeInfo = nodeSemanticModel.GetTypeInfo(node);
            
            var replaceValueSyntaxNode =
                _randomTypeGenerator.ResolveType(typeInfo.Type.Name);

            //if (replaceValueSyntaxNode != null)
            //{
            //    //var newAssignmentNode =
            //    //    SyntaxFactory.VariableDeclaration(
            //    //        node.,
            //    //        node.Left,
            //    //        replaceValueSyntaxNode).NormalizeWhitespace();

            //    //var mutatedClassRoot = _classRootNode.ReplaceNode(node, newAssignmentNode);
            //    //_mutantCreator.CreateNewMutant(mutatedClassRoot, false);
            //}

            return node;
        }
    }
}
