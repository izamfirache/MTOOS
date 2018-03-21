﻿using System;
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
        public List<Mutant> PerformMutationTestingOnProject(DTE2 dte, Solution2 solution, string projectToAnalyze, 
            List<string> options)
        {
            var liveMutants = new List<Mutant>();

            var mutationAnalyzer = new SourceCodeMutator(solution);
            Project selectedProject = GetSelectedProject(solution, projectToAnalyze);
            selectedProject.ProjectItems.AddFolder("Mutants");
            selectedProject.Save();
            
            var mutatedClasses = mutationAnalyzer.PerformMutationAnalysisOnProject(selectedProject, options);
            if (mutatedClasses.Count != 0)
            {
                //rethink this !! -- reload or rebuild solution/project
                dte.ExecuteCommand("CloseAll");

                //TODO: to avoid messing up the original project with the mutants
                //create a new project with the mutation analysis result
                //compile that project and add the reference to the unit test mutated classes

                //build the selected project in order for the new types to be visible
                SolutionBuild2 solutionBuild2 = (SolutionBuild2)selectedProject.DTE.Solution.SolutionBuild;
                solutionBuild2.BuildProject(solutionBuild2.ActiveConfiguration.Name,
                    selectedProject.UniqueName, true);

                bool compiledOK = (solutionBuild2.LastBuildInfo == 0);
                if (compiledOK)
                {
                    var mutatedUnitTestProject = MutateUnitTestProject(solution, solutionBuild2, mutatedClasses);

                    dte.ExecuteCommand("CloseAll");

                    liveMutants = RunTheMutatedUnitTestsUsingNUnitConsole(mutatedUnitTestProject, dte);

                    DeleteAllNonRelevantFiles(mutatedUnitTestProject, selectedProject, solutionBuild2, liveMutants);

                    dte.ExecuteCommand("TestExplorer.ShowTestExplorer");
                    dte.ExecuteCommand("TestExplorer.RunAllTests");

                    MessageBox.Show("Mutation testing Done. Please check the mutants in the selected" +
                        " project. Select the original file and the mutant in Solution Explorer, " +
                        "right click and press 'Test by mutation...'. This way you will see the untested " +
                        "areas in your code.",
                        "Mutation Testing Done.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Compilation errors after mutation.");
                }
            }
            else
            {
                MessageBox.Show(string.Format("No mutants generated. " +
                    "Might be a problem with the mutation process."));
            }

            return liveMutants;
        }

        

        private Project MutateUnitTestProject(Solution2 solution, SolutionBuild2 solutionBuild, 
            Dictionary<string, string> mutatedClasses)
        {
            Project unitTestProject = GetUnitTestProject(solution);
            unitTestProject.ProjectItems.AddFolder("MutationCompiledUnits");
            unitTestProject.Save();

            var unitTestsMutator = new UnitTestSuiteMutator(solution, mutatedClasses);
            unitTestsMutator.PerformMutationForUnitTestProject(unitTestProject);

            solutionBuild.BuildProject(solutionBuild.ActiveConfiguration.Name,
                unitTestProject.UniqueName, true);

            return unitTestProject;
        }

        private Project GetSelectedProject(Solution2 solution, string projectName)
        {
            foreach (Project p in solution.Projects)
            {
                if (p.Name == projectName)
                {
                    return p;
                }
            }
            return null;
        }

        private List<Mutant> RunTheMutatedUnitTestsUsingNUnitConsole(Project unitTestProject, DTE2 dte)
        {
            var unitTestAssemblyPath = GetAssemblyPath(unitTestProject);
            string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            var packagesFolder = Path.Combine(solutionDir, "packages");
            var NUnitConsolePath = Path.Combine(packagesFolder,
                "NUnit.ConsoleRunner.3.8.0\\tools\\nunit3-console.exe"); // TODO: avoid the version dependency!!
            var nunitOutputFilePath = Path.GetDirectoryName(unitTestAssemblyPath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = string.Format(@"""{0}""", NUnitConsolePath),
                Arguments = string.Format(@"--work=""{0}"" ""{1}""", nunitOutputFilePath, unitTestAssemblyPath),
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
                        GetAssemblyPath(unitTestProject)), "TestResult.xml");

            List<Mutant> liveMutants = new List<Mutant>();
            if (File.Exists(NUnitResultXmlFilePath))
            {
                liveMutants = AnalyzeNUnitTestResultFile(NUnitResultXmlFilePath);
            }

            return liveMutants;
        }

        private List<Mutant> AnalyzeNUnitTestResultFile(string nUnitResultXmlFilePath)
        {
            List<Mutant> mutationAnalysis = new List<Mutant>();
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
                            mutationAnalysis.Add(new Mutant()
                            {
                                Id = Guid.NewGuid(),
                                Name = testFixtureNode.Attributes["name"].Value,
                                Status = testFixtureNode.Attributes["result"].Value
                            });
                        }
                    }
                }
            }

            return mutationAnalysis;
        }

        private void DeleteAllNonRelevantFiles(Project unitTestProject, Project selectedProject,
            SolutionBuild2 solutionBuild, List<Mutant> liveMutants)
        {
            //delete all the unit test mutated classes since they are relevant only
            //to run the unit tests over the mutants
            foreach (ProjectItem projItem in unitTestProject.ProjectItems)
            {
                if (projItem.Name == "MutationCompiledUnits")
                {
                    //remove it from the VS project
                    projItem.Remove();

                    //remove it from the disk also
                    var path = Path.Combine(Path.GetDirectoryName(unitTestProject.FullName), "MutationCompiledUnits");
                    Directory.Delete(path, true);
                    break;
                }
            }
            unitTestProject.Save();
            solutionBuild.BuildProject(solutionBuild.ActiveConfiguration.Name,
                unitTestProject.UniqueName, true);

            //delete all 'killed' mutants since the code they mutated is properly tested
            //and save only the 'live' ones to highlight the untested code
            foreach (ProjectItem projItem in selectedProject.ProjectItems)
            {
                if (projItem.Name == "Mutants")
                {
                    foreach (ProjectItem pi in projItem.ProjectItems)
                    {
                        var mutantName = pi.Name.Replace(".cs", "");
                        if (!liveMutants.Any(m => m.Name.Contains(mutantName)))
                        {
                            //remove it from the VS project
                            pi.Remove();

                            //remove it from the disk also
                            var path = Path.Combine(Path.GetDirectoryName(selectedProject.FullName),
                                string.Format(@"{0}\\{1}", "Mutants", pi.Name));
                            File.Delete(path);
                        }
                    }
                    break;
                }
            }
            selectedProject.Save();
            solutionBuild.BuildProject(solutionBuild.ActiveConfiguration.Name,
                selectedProject.UniqueName, true);
        }

        private string GetAssemblyPath(Project project)
        {
            string fullPath = project.Properties.Item("FullPath").Value.ToString();
            string outputPath = project.ConfigurationManager.ActiveConfiguration.
                Properties.Item("OutputPath").Value.ToString();

            string outputDir = Path.Combine(fullPath, outputPath);
            string outputFileName = project.Properties.Item("OutputFileName").Value.ToString();
            string assemblyPath = Path.Combine(outputDir, outputFileName);

            return assemblyPath;
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
