using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MTOOS.Extension.MutationAnalysis;
using VSLangProj;

namespace MTOOS.Extension
{
    internal sealed class MutationTestingCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("2dd225ff-809e-4ad3-9055-ff73831acf0f");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;
        
        private MutationTestingCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }
        
        public static MutationTestingCommand Instance
        {
            get;
            private set;
        }
        
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }
        
        public static void Initialize(Package package)
        {
            Instance = new MutationTestingCommand(package);
        }
        
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
            var selectedItems = dte.SelectedItems;

            if (selectedItems.Count == 1)
            {
                var mutationAnalyzer = new SourceCodeMutator((Solution2)dte.Solution);
                Project selectedProject = GetSelectedProject(dte);
                selectedProject.ProjectItems.AddFolder("Mutants");
                selectedProject.Save();

                if (selectedProject != null)
                {
                    var mutatedClasses = mutationAnalyzer.PerformMutationAnalysisOnProject(selectedProject);
                    if (mutatedClasses.Count != 0)
                    {
                        DialogResult dialogResult = MessageBox.Show("The mutation process is finished." +
                            " Do you want to run the existing unit tests over the mutants set ?", 
                            "Mutation done.", MessageBoxButtons.YesNo);

                        if (dialogResult == DialogResult.Yes)
                        {
                            dte.ExecuteCommand("CloseAll"); // rethink this !!

                            //TODO: to avoid messing up the original project with the mutants
                            //create a new project with the mutation analysis
                            //compile that project and add the reference to the unit test mutated classes

                            //build the selected project in order for the new types to be visible
                            SolutionBuild2 solutionBuild2 = (SolutionBuild2)selectedProject.DTE.Solution.SolutionBuild;
                            solutionBuild2.BuildProject(solutionBuild2.ActiveConfiguration.Name, 
                                selectedProject.UniqueName, true);

                            bool compiledOK = (solutionBuild2.LastBuildInfo == 0);
                            if (compiledOK)
                            {
                                MessageBox.Show("Original project compiled successfully after mutation.");
                                
                                Solution2 solution = (Solution2)dte.Solution;
                                Project unitTestProject = GetUnitTestProject(solution);
                                unitTestProject.ProjectItems.AddFolder("MutationCompiledUnits");
                                unitTestProject.Save();

                                var unitTestsMutator = new UnitTestSuiteMutator(solution, mutatedClasses);
                                unitTestsMutator.PerformMutationForUnitTestProject(unitTestProject);

                                solutionBuild2.BuildProject(solutionBuild2.ActiveConfiguration.Name,
                                    unitTestProject.UniqueName, true);

                                dte.ExecuteCommand("TestExplorer.ShowTestExplorer");
                                dte.ExecuteCommand("TestExplorer.RunAllTests");

                                dte.ExecuteCommand("CloseAll");

                                var unitTestProjectPath = unitTestProject.ConfigurationManager.
                                    ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();

                                RunTheMutatedUnitTestsUsingNUnitConsole(unitTestProject, dte);
                                MessageBox.Show("Done.");
                            }
                            else
                            {
                                MessageBox.Show("Compilation errors after mutation.");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(string.Format("No mutants generated. " +
                            "Might be a problem with the mutation process"));
                    }
                }
                else
                {
                    MessageBox.Show("Problem on getting the selected project metadata.");
                }
            }
            else if(selectedItems.Count == 2)
            {
                CompareFiles(dte);
            }
        }

        private void RunTheMutatedUnitTestsUsingNUnitConsole(Project unitTestProject, DTE2 dte)
        {
            var unitTestAssemblyPath = GetAssemblyPath(unitTestProject);
            string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            var packagesFolder = Path.Combine(solutionDir, "packages");
            var NUnitConsolePath = Path.Combine(packagesFolder, 
                "NUnit.ConsoleRunner.3.8.0\\tools\\nunit3-console.exe");
            
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = NUnitConsolePath,
                WindowStyle = ProcessWindowStyle.Normal,
                Arguments = "/k " + string.Format("{0}", unitTestAssemblyPath)
            };

            try
            {
                using (System.Diagnostics.Process exeProcess = System.Diagnostics.Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                    var NUnitResultXmlFilePath =
                        Path.Combine(Path.GetDirectoryName(
                            GetAssemblyPath(unitTestProject)), "TestResult.xml");

                    XElement testResult = XElement.Load(NUnitResultXmlFilePath);
                    MessageBox.Show(testResult.ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
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

        private Project GetSelectedProject(DTE2 dte)
        {
            var selectedProjectName = dte.SelectedItems.Item(1).Name;

            foreach(Project p in dte.Solution.Projects)
            {
                if(p.Name == selectedProjectName)
                {
                    return p;
                }
            }
            return null;
        }

        private void CompareFiles(DTE2 dte)
        {
            if (CanFilesBeCompared(dte, out string file1, out string file2))
            {
                dte.ExecuteCommand("Tools.DiffFiles", $"\"{file1}\" \"{file2}\"");
            }
        }

        private static bool CanFilesBeCompared(DTE2 dte, out string file1, out string file2)
        {
            var items = GetSelectedFiles(dte);

            file1 = items.ElementAtOrDefault(0);
            file2 = items.ElementAtOrDefault(1);

            return !string.IsNullOrEmpty(file1) && !string.IsNullOrEmpty(file2);
        }

        public static IEnumerable<string> GetSelectedFiles(DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;

            return from item in items.Cast<UIHierarchyItem>()
                   let pi = item.Object as ProjectItem
                   select pi.FileNames[1];
        }
    }
}