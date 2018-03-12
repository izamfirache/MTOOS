using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MTOOS.Extension.Mutators
{
    public class AritmeticOperatorMutator : CSharpSyntaxRewriter
    {
        private string _className;
        private SyntaxNode _namespaceRootNode;
        private EnvDTE.Project _project;
        private Solution2 _currentSolution;
        private int _mutantVersion = 0;
        private Dictionary<string, string> MutatedClassNames;
        public AritmeticOperatorMutator(string className, SyntaxNode namespaceRootNode, 
            EnvDTE.Project project, Solution2 currentSolution)
        {
            _className = className;
            _namespaceRootNode = namespaceRootNode;
            _project = project;
            _currentSolution = currentSolution;
            MutatedClassNames = new Dictionary<string, string>();
        }
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.None);
            if (token.IsKind(SyntaxKind.MinusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.PlusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }
            
            if (token.IsKind(SyntaxKind.PlusToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.MinusToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (token.IsKind(SyntaxKind.SlashToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (token.IsKind(SyntaxKind.AsteriskToken))
            {
                newToken = SyntaxFactory.Token(SyntaxKind.SlashToken)
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (!newToken.IsKind(SyntaxKind.None))
            {
                var mutatedNamespaceRoot = _namespaceRootNode.ReplaceToken(token, newToken);
                
                string mutatedClassName = string.Format("{0}Mutant{1}", _className, _mutantVersion);
                var classNameMutator = new ClassIdentifierMutator(_className, 
                    string.Format("{0}Mutant{1}", _className, _mutantVersion));
                var finalMutantCodeRoot = classNameMutator.Visit(mutatedNamespaceRoot);

                CreateNewMutant(_project, finalMutantCodeRoot.ToFullString(), mutatedClassName);
                _mutantVersion += 1;
            }

            return token;
        }

        private void CreateNewMutant(EnvDTE.Project project, string mutatedCode, string mutatedClassName)
        {
            string mutatedClassPath = string.Format(@"{0}\..\{1}\{2}\{3}.cs",
                _currentSolution.FileName, project.Name, "Mutants", mutatedClassName);

            var classTemplatePath = _currentSolution.GetProjectItemTemplate("Class.zip", "csharp");

            foreach (EnvDTE.ProjectItem projItem in project.ProjectItems)
            {
                if (projItem.Name == "Mutants")
                {
                    projItem.ProjectItems.AddFromTemplate(classTemplatePath, string.Format("{0}.cs", mutatedClassName));
                    MutatedClassNames.Add(mutatedClassName, _className);
                    break;
                }
            }

            File.WriteAllText(mutatedClassPath, mutatedCode, Encoding.Default);
        }

        public Dictionary<string, string> GetMutatedClasses()
        {
            return MutatedClassNames;
        }
    }
}