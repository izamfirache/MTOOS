using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MTOOS.Extension.Models;
using MTOOS.Extension.MutationAnalysis;
using VSLangProj;

namespace MTOOS.Extension
{
    public class MutationTestingManager
    {
        public List<GeneratedMutant> PerformMutationTestingOnProject(DTE2 dte, Project sourceCodeProject, 
            Project unitTestProject, List<string> options)
        {
            Solution2 solution = (Solution2)dte.Solution;
            var mutationAnalyzer = new SourceCodeMutator(solution);

             var sourceprojectMutationResult = 
                mutationAnalyzer.PerformMutationAnalysisOnSourceCodeProject(sourceCodeProject, options);

            var liveMutants = new List<GeneratedMutant>();
            //if (sourceprojectMutationResult.GeneratedMutants.Count != 0)
            //{
            //    var unitTestProjectMutation =
            //        MutateUnitTestProject(solution, sourceprojectMutationResult, unitTestProject, sourceCodeProject);

            //    foreach(var syntaxTree in unitTestProjectMutation.MutatedUnitTestProjectCompilation.SyntaxTrees)
            //    {
            //        MessageBox.Show(syntaxTree.GetRoot().ToFullString());
            //    }

            //    liveMutants = RunTheMutatedUnitTestSuiteUsingNUnitConsole
            //        (unitTestProjectMutation, dte, sourceprojectMutationResult.GeneratedMutants);

            //    MessageBox.Show("Mutation Testing done!");
            //}
            //else
            //{
            //    MessageBox.Show("Error at source code project mutation");
            //}

            return liveMutants;
        }

        private UnitTestMutationResult MutateUnitTestProject(Solution2 solution, 
            SourceCodeMutationResult sourceCodeMutationResult, Project unitTestProject, Project sourceCodeProject)
        {
            var unitTestsMutator = new UnitTestSuiteMutator(solution);
            var unitTestMutationResult = 
                unitTestsMutator.PerformMutationForUnitTestProject(unitTestProject, sourceCodeMutationResult, sourceCodeProject);

            return unitTestMutationResult;
        }

        private List<GeneratedMutant> RunTheMutatedUnitTestSuiteUsingNUnitConsole(
            UnitTestMutationResult unitTestProjectMutationResult, 
            DTE2 dte, 
            List<GeneratedMutant> mutationAnalysis)
        {
            string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            var packagesFolder = Path.Combine(solutionDir, "packages");
            var NUnitConsolePath = Path.Combine(packagesFolder,
                "NUnit.ConsoleRunner.3.8.0\\tools\\nunit3-console.exe"); // TODO: avoid the version dependency!!

            var nunitOutputFilePath = Path.GetDirectoryName(unitTestProjectMutationResult.OutputPath);
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = string.Format(@"""{0}""", NUnitConsolePath),
                Arguments = string.Format(@"--work=""{0}"" ""{1}""", nunitOutputFilePath,
                    unitTestProjectMutationResult.OutputPath),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (System.Diagnostics.Process exeProcess =
                    System.Diagnostics.Process.Start(processStartInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            var NUnitResultXmlFilePath = Path.Combine(Path.GetDirectoryName(
                        Path.GetDirectoryName(unitTestProjectMutationResult.OutputPath)),
                        @"Debug\\TestResult.xml");

            List<GeneratedMutant> liveMutants = new List<GeneratedMutant>();
            if (File.Exists(NUnitResultXmlFilePath))
            {
                liveMutants = AnalyzeNUnitTestResultFile(NUnitResultXmlFilePath, mutationAnalysis);
            }
            
            return liveMutants;
        }

        private List<GeneratedMutant> AnalyzeNUnitTestResultFile(string nUnitResultXmlFilePath, 
            List<GeneratedMutant> mutationInformation)
        {
            List<GeneratedMutant> mutationAnalysis = new List<GeneratedMutant>();
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(nUnitResultXmlFilePath);

            XmlNode testRunNode = xmlDocument.SelectSingleNode("test-run");
            XmlNode assemblyTestSuiteNode = testRunNode.SelectSingleNode("test-suite");
            XmlNode testSuiteNode = assemblyTestSuiteNode.SelectSingleNode("test-suite");

            if (testSuiteNode.Attributes["type"].Value == "TestSuite")
            {
                foreach (XmlNode testFixtureNode in testSuiteNode.SelectNodes("test-suite"))
                {
                    if (testFixtureNode.Attributes["name"].Value.Contains("Mutant"))
                    {
                        if (testFixtureNode.Attributes["result"].Value == "Passed")
                        {
                            foreach (GeneratedMutant mi in mutationInformation)
                            {
                                if (testFixtureNode.Attributes["name"].Value.Contains(mi.MutantName))
                                {
                                    mutationAnalysis.Add(new GeneratedMutant()
                                    {
                                        Id = mi.Id,
                                        MutantName = mi.MutantName,
                                        Status = testFixtureNode.Attributes["result"].Value,
                                        MutatedCode = mi.MutatedCode,
                                        OriginalProgramCode = mi.OriginalProgramCode,
                                        OriginalClassName = mi.OriginalClassName
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return mutationAnalysis;
        }
        private Project GetUnitTestProject(Solution2 solution)
        {
            foreach (Project p in solution.Projects)
            {
                if (p.Name.Contains("UnitTest")) //rethink this !!
                {
                    return p;
                }
            }
            return null;
        }
    }
}