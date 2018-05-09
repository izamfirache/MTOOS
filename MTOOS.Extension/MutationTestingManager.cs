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
        public MutationAnalysisResult PerformMutationTestingOnProject(DTE2 dte, Project sourceCodeProject, 
            Project unitTestProject, List<string> options)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Solution2 solution = (Solution2)dte.Solution;
            var sourceCodeMutator = new SourceCodeMutator(solution);

             var sourceprojectMutationResult = 
                sourceCodeMutator.PerformMutationAnalysisOnSourceCodeProject(sourceCodeProject, options);

            var liveMutants = new List<GeneratedMutant>();
            if (sourceprojectMutationResult.GeneratedMutants.Count != 0)
            {
                var unitTestProjectMutation =
                    MutateUnitTestProject(solution, sourceprojectMutationResult,
                        unitTestProject, sourceCodeProject);

                liveMutants = RunTheMutatedUnitTestSuiteUsingNUnitConsole
                    (unitTestProjectMutation, dte, sourceprojectMutationResult.GeneratedMutants);
            }
            else
            {
                MessageBox.Show("Error at source code project mutation");
            }

            stopwatch.Stop();
            MessageBox.Show(string.Format("Done. Execution time: {0} ms. {1} mutants generated. " +
                "{2} live mutants.",
                stopwatch.ElapsedMilliseconds.ToString(), sourceprojectMutationResult.GeneratedMutants.Count,
                liveMutants.Count));

            return new MutationAnalysisResult()
            {
                GeneratedMutants = sourceprojectMutationResult.GeneratedMutants,
                LiveMutants = liveMutants
            };
        }

        private UnitTestMutationResult MutateUnitTestProject(Solution2 solution, 
            SourceCodeMutationResult sourceCodeMutationResult, Project unitTestProject, 
            Project sourceCodeProject)
        {
            var unitTestsMutator = new UnitTestSuiteMutator(solution);
            var unitTestMutationResult = 
                unitTestsMutator.PerformMutationForUnitTestProject(unitTestProject, sourceCodeMutationResult, 
                sourceCodeProject);

            return unitTestMutationResult;
        }

        private List<GeneratedMutant> RunTheMutatedUnitTestSuiteUsingNUnitConsole(
            UnitTestMutationResult unitTestProjectMutationResult, 
            DTE2 dte, 
            List<GeneratedMutant> mutationAnalysis)
        {
            string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            var packagesFolder = Path.Combine(solutionDir, "packages");

            // TODO: avoid the version dependency!!
            var NUnitConsolePath = Path.Combine(packagesFolder,
                "NUnit.ConsoleRunner.3.7.0\\tools\\nunit3-console.exe"); 

            var nunitOutputFilePath = Path.GetDirectoryName(unitTestProjectMutationResult.OutputPath);
            var arguments = string.Format(@"--work=""{0}"" ""{1}""", nunitOutputFilePath,
                    unitTestProjectMutationResult.OutputPath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = string.Format(@"""{0}""", NUnitConsolePath),
                Arguments = arguments,
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

            var NUnitResultXmlFilePath = Path.Combine(
                Path.GetDirectoryName(unitTestProjectMutationResult.OutputPath),
                        @"TestResult.xml");

            List<GeneratedMutant> liveMutants = new List<GeneratedMutant>();
            if (File.Exists(NUnitResultXmlFilePath))
            {
                liveMutants = AnalyzeNUnitTestResultFile(NUnitResultXmlFilePath, mutationAnalysis);
            }
            
            return liveMutants;
        }

        private List<GeneratedMutant> AnalyzeNUnitTestResultFile(string nUnitResultXmlFilePath, 
            List<GeneratedMutant> generatedMutants)
        {
            List<GeneratedMutant> liveMutants = new List<GeneratedMutant>();
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(nUnitResultXmlFilePath);

            XmlNode testRunNode = xmlDocument.SelectSingleNode("test-run");
            XmlNode assemblyTestSuiteNode = testRunNode.SelectSingleNode("test-suite");

            foreach (XmlNode testSuiteNode in assemblyTestSuiteNode.SelectNodes("test-suite"))
            {
                if (testSuiteNode.Attributes["type"].Value == "TestSuite")
                {
                    foreach (XmlNode testFixtureNode in testSuiteNode.SelectNodes("test-suite"))
                    {
                        if (testFixtureNode.Attributes["type"].Value == "TestFixture")
                        {
                            if (testFixtureNode.Attributes["name"].Value.Contains("Mutant"))
                            {
                                if (testFixtureNode.Attributes["result"].Value == "Passed")
                                {
                                    foreach (GeneratedMutant mi in generatedMutants)
                                    {
                                        if (testFixtureNode.Attributes["name"].Value.Contains(mi.MutantName))
                                        {
                                            //if(liveMutants.Any(m=>m.MutantName == mi.MutantName))
                                            liveMutants.Add(new GeneratedMutant()
                                            {
                                                Id = mi.Id,
                                                MutantName = mi.MutantName,
                                                Status = testFixtureNode.Attributes["result"].Value,
                                                MutatedCode = mi.MutatedCode,
                                                OriginalProgramCode = mi.OriginalProgramCode,
                                                OriginalClassName = mi.OriginalClassName,
                                                HaveDeletedStatement = mi.HaveDeletedStatement,
                                                MutatorType = mi.MutatorType
                                            });

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return liveMutants;
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