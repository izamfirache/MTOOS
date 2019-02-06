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
    public class ClassMemberAssignDel : CSharpSyntaxRewriter
    {
        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        private List<string> _classFieldsIdentifiers;
        public ClassMemberAssignDel(SyntaxNode classRootNode, MutantCreator mutantCreator,
            List<string> classFieldsIdentifiers)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
            _classFieldsIdentifiers = classFieldsIdentifiers;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            bool isRemovableNode = false;
            if(node.Left is MemberAccessExpressionSyntax)
            {
                var memberAccessExpression = node.Left as MemberAccessExpressionSyntax;
                if (memberAccessExpression.Expression is ThisExpressionSyntax)
                {
                    var accessedMemberName = memberAccessExpression.Name.Identifier.ToString();
                    if (_classFieldsIdentifiers.Contains(accessedMemberName))
                    {
                        isRemovableNode = true;
                    }
                }

                if (memberAccessExpression.Expression is IdentifierNameSyntax)
                {
                    var identifierNameSyntax = memberAccessExpression.Expression as IdentifierNameSyntax;
                    var identifier = identifierNameSyntax.Identifier.ToString();
                    if (_classFieldsIdentifiers.Contains(identifier))
                    {
                        isRemovableNode = true;
                    }
                }
            }

            if (node.Left is IdentifierNameSyntax)
            {
                var identifierNameSyntax = node.Left as IdentifierNameSyntax;
                var identifier = identifierNameSyntax.Identifier.ToString();
                if (_classFieldsIdentifiers.Contains(identifier))
                {
                    isRemovableNode = true;
                }
            }

            if (isRemovableNode)
            {
                var mutatedNamespaceRoot = _classRootNode.RemoveNode(node.Parent,
                        SyntaxRemoveOptions.KeepLeadingTrivia |
                        SyntaxRemoveOptions.KeepTrailingTrivia);
                _mutantCreator.CreateNewMutant(mutatedNamespaceRoot, true);
            }

            return node;
        }
    }
}
