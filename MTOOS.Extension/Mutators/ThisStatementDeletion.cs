using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Mutators
{
    public class ThisStatementDeletion : CSharpSyntaxRewriter
    {
        private SyntaxNode _namespaceRootNode;
        private MutantCreator _mutantCreator;
        private List<string> _classFieldsIdentifiers;
        public ThisStatementDeletion(SyntaxNode namespaceRootNode, MutantCreator mutantCreator,
            List<string> classFieldsIdentifiers)
        {
            _namespaceRootNode = namespaceRootNode;
            _mutantCreator = mutantCreator;
            _classFieldsIdentifiers = classFieldsIdentifiers;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            if(node.Expression is AssignmentExpressionSyntax)
            {
                var assignmentNode = node.Expression as AssignmentExpressionSyntax;
                if(assignmentNode.Left is MemberAccessExpressionSyntax)
                {
                    var memberAccessExpression = assignmentNode.Left as MemberAccessExpressionSyntax;
                    if (memberAccessExpression.Expression is ThisExpressionSyntax)
                    {
                        var accessedMemberName = memberAccessExpression.Name.Identifier.ToString();
                        if (_classFieldsIdentifiers.Contains(accessedMemberName))
                        {
                            var mutatedNamespaceRoot = _namespaceRootNode.RemoveNode(node,
                                SyntaxRemoveOptions.KeepLeadingTrivia | 
                                SyntaxRemoveOptions.KeepTrailingTrivia);
                            _mutantCreator.CreateNewMutant(mutatedNamespaceRoot);

                            return node;
                        }
                    }

                    if (memberAccessExpression.Expression is IdentifierNameSyntax)
                    {
                        var identifierNameSyntax = memberAccessExpression.Expression as IdentifierNameSyntax;
                        var identifier = identifierNameSyntax.Identifier.ToString();
                        if (_classFieldsIdentifiers.Contains(identifier))
                        {
                            var mutatedNamespaceRoot = _namespaceRootNode.RemoveNode(node,
                                SyntaxRemoveOptions.KeepLeadingTrivia |
                                SyntaxRemoveOptions.KeepTrailingTrivia);
                            _mutantCreator.CreateNewMutant(mutatedNamespaceRoot);

                            return node;
                        }
                    }
                }

                if (assignmentNode.Left is IdentifierNameSyntax)
                {
                    var identifierNameSyntax = assignmentNode.Left as IdentifierNameSyntax;
                    var identifier = identifierNameSyntax.Identifier.ToString();
                    if (_classFieldsIdentifiers.Contains(identifier))
                    {
                        var mutatedNamespaceRoot = _namespaceRootNode.RemoveNode(node,
                            SyntaxRemoveOptions.KeepLeadingTrivia |
                            SyntaxRemoveOptions.KeepTrailingTrivia);
                        _mutantCreator.CreateNewMutant(mutatedNamespaceRoot);

                        return node;
                    }
                }
            }

            return node;
        }
    }
}
