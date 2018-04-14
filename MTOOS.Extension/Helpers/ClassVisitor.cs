using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MTOOS.Extension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Helpers
{
    public class ClassVisitor : CSharpSyntaxRewriter
    {
        public List<Class> ProjectClasses;
        public ClassVisitor()
        {
            ProjectClasses = new List<Class>();
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var newClass = new Class() { Name = node.Identifier.ToString() };
            
            ConstructorDeclarationSyntax constructor = 
                node.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

            newClass.Constructor = new ClassConstructor();
            if (constructor != null)
            {
                foreach (var cp in constructor.ParameterList.Parameters)
                {
                    newClass.Constructor.Parameters.Add(new MethodParameter()
                    {
                        Name = cp.Identifier.ToString(),
                        Type = cp.Type.ToString()
                    });
                }
            }

            ProjectClasses.Add(newClass);

            return node;
        }
    }
}
