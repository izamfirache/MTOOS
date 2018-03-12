using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.TestMutators
{
    public class UnitTestClassMutator : CSharpSyntaxRewriter
    {
        //this will mutate the unit test classes by replacing the CUT name with the mutant name
        //this way the mutated version of the code will be called
        //this will be done at runtime and only 'live' mutants will be saved
        private string _className;
        private string _mutatedClassName;
        private string _mutatedUnitTestClassname;
        private string _unitTestClassName;
        public UnitTestClassMutator(string className, string mutatedClassName, 
            string unitTestClassName, string mutatedUnitTestClassname)
        {
            _className = className;
            _mutatedClassName = mutatedClassName;
            _mutatedUnitTestClassname = mutatedUnitTestClassname;
            _unitTestClassName = unitTestClassName;
        }
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                if (token.Value.ToString() == _unitTestClassName)
                {
                    return SyntaxFactory.Identifier(_mutatedUnitTestClassname);
                }

                if (token.Value.ToString() == _className)
                {
                    return SyntaxFactory.Identifier(_mutatedClassName);
                }
            }

            return token;
        }
    }
}