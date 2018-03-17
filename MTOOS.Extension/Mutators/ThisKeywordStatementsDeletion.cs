using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Mutators
{
    public class ThisKeywordStatementsDeletion : CSharpSyntaxRewriter
    {
        private SyntaxNode _namespaceRootNode;
        private MutantCreator _mutantCreator;

        public ThisKeywordStatementsDeletion(SyntaxNode namespaceRootNode, MutantCreator mutantCreator)
        {
            _namespaceRootNode = namespaceRootNode;
            _mutantCreator = mutantCreator;
        }

        public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node)
        {
            // replace this.SomeState = SomeOtherState; expressions with an empty row
            var newNode = SyntaxFactory.EmptyStatement();

            var mutatedNamespaceRoot = _namespaceRootNode.ReplaceNode(node, newNode);
            _mutantCreator.CreateNewMutant(mutatedNamespaceRoot);

            return newNode;
        }
    }
}
