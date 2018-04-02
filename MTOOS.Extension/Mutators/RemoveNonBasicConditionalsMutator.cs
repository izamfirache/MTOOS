using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MTOOS.Extension.Mutators
{
    public class RemoveNonBasicConditionalsMutator : CSharpSyntaxRewriter
    {
        //for every if statement for which the condition's expression does not contain
        //relational or equality operators (<, >, <=, >=, ==, !=) 
        //as the main operator between the only two members (if(a == b) if(a >= b)...)

        //replace the condition with true and false
        //this will fit Linq operations with lists: if(randomList.Any(p => p.Name == "John"))
        //or string operations: if(someString.Contains("str"))
        //or any other invocation that returns bool: if(SomeFunctionThatReturnsBool())

        private SyntaxNode _classRootNode;
        private MutantCreator _mutantCreator;
        
        public RemoveNonBasicConditionalsMutator(SyntaxNode classRootNode, MutantCreator mutantCreator)
        {
            _classRootNode = classRootNode;
            _mutantCreator = mutantCreator;
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            //check if the condition does not involve realational or equality operators
            //as the main operator between the only two members in that expression

            if (!(node.Condition is BinaryExpressionSyntax))
            {
                var trueLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                var falseLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

                var trueConditionNode = node.ReplaceNode(node.Condition, trueLiteralExpression);
                var falseConditionNode = node.ReplaceNode(node.Condition, falseLiteralExpression);

                var classMutatedWithTrueCondition =
                    _classRootNode.ReplaceNode(node, trueConditionNode);
                _mutantCreator.CreateNewMutant(classMutatedWithTrueCondition, false);

                var classMutatedWithFalseCondition =
                    _classRootNode.ReplaceNode(node, falseConditionNode);
                _mutantCreator.CreateNewMutant(classMutatedWithFalseCondition, false);
            }

            return node;
        }

        private bool CheckIfNodeIsBasicIfStatement(ExpressionSyntax condition)
        {
            return false;
        }
    }
}
