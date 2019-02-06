using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using MTOOS.Extension.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MTOOS.Extension.Views
{
    /// <summary>
    /// Interaction logic for MutationAnalysisView.xaml
    /// </summary>
    public partial class MutationAnalysisView : UserControl
    {
        public List<string> CheckedOptions;
        public MutationAnalysisResult MutationAnalysisResult = new MutationAnalysisResult();
        private List<Project> SoulutionProjects;
        private Project sourceCodeProject;
        private Project unitTestProject;
        private int MutationAnalysisNumber = 0;
        /// <summary>
        /// Initializes a new instance of the <see cref="MutantKillerWindowControl"/> class.
        /// </summary>
        public MutationAnalysisView()
        {
            this.InitializeComponent();
            solutionProjectList.ItemsSource = GetSolutionProjects();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        public void StartMutationTesting(object sender, RoutedEventArgs e)
        {
            var dte = (DTE2)Microsoft.VisualStudio.Shell.ServiceProvider
                    .GlobalProvider.GetService(typeof(EnvDTE.DTE));

            var projects = new List<ProjectPresentation>();
            foreach (var item in solutionProjectList.SelectedItems)
            {
                var selectedProject = (ProjectPresentation)item;
                selectedProject.IsSelected = true;
                projects.Add(selectedProject);
            }
            
            if (projects.Count == 2)
            {
                sourceCodeProject = GetSourceCodeProject(projects);
                unitTestProject = GetUnitTestProject(projects);
                if (sourceCodeProject != null && unitTestProject != null)
                {
                    CheckedOptions = GetCheckedOptions();
                    if (CheckedOptions.Count != 0)
                    {
                        MutationTestingManager mutationTestingManager = new MutationTestingManager();
                        MutationAnalysisResult = mutationTestingManager
                            .PerformMutationTestingOnProject(dte, sourceCodeProject,
                                unitTestProject, CheckedOptions);
                        liveMutantsListBox.ItemsSource = MutationAnalysisResult.LiveMutants;
                        generatedMutantsListBox.ItemsSource = MutationAnalysisResult.GeneratedMutants;
                        decimal totalNrOfMutants = MutationAnalysisResult.GeneratedMutants.Count;
                        decimal liveMutants = MutationAnalysisResult.LiveMutants.Count;
                        decimal killedMutants = totalNrOfMutants - liveMutants;
                        decimal mutationScoreValue = killedMutants / totalNrOfMutants;

                        mutationScore.Text = mutationScoreValue.ToString("N2");
                        totalNrOfMutantsTextBlock.Text = totalNrOfMutants.ToString("N1");
                        liveMutantsTextBlock.Text = liveMutants.ToString("N1");
                        killedMutantsTextBlock.Text = killedMutants.ToString("N1");
                    }
                    else
                    {
                        MessageBox.Show("Please check the mutations you want to perform.",
                            "No options checked.", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please check a source code project and a unit test project before starting mutation testing.",
                    "No projects selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Project GetUnitTestProject(List<ProjectPresentation> projects)
        {
            foreach (ProjectPresentation p in projects)
            {
                if (p.Type == "UnitTest")
                {
                    foreach (Project sp in SoulutionProjects)
                    {
                        if (sp.Name == p.Name)
                        {
                            return sp;
                        }
                    }
                }
            }

            return null;
        }

        private Project GetSourceCodeProject(List<ProjectPresentation> projects)
        {
            foreach (ProjectPresentation p in projects)
            {
                if (p.Type == "SourceCode")
                {
                    foreach (Project sp in SoulutionProjects)
                    {
                        if (sp.Name == p.Name)
                        {
                            return sp;
                        }
                    }
                }
            }
            return null;
        }

        private static DTE2 GetActiveIDE()
        {
            // Get an instance of currently running Visual Studio IDE.
            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            return dte2;
        }

        private List<ProjectPresentation> GetSolutionProjects()
        {
            var projects = new List<ProjectPresentation>();
            List<Project> list = new List<Project>();
            Projects dteProjects = GetActiveIDE().Solution.Projects;
            var item = dteProjects.GetEnumerator();

            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            foreach (Project p in list)
            {
                projects.Add(new ProjectPresentation()
                {
                    Name = p.Name,
                    Type = p.Name.Contains("Tests") || p.Name.Contains("Test") ? "UnitTest" : "SourceCode"
                });
            }

            SoulutionProjects = list;
            return projects;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }

        private List<string> GetCheckedOptions()
        {
            var checkedOptions = new List<string>();
            if (BoundaryOpMutator.IsChecked == true) { checkedOptions.Add("1"); }
            if (RelationalAndEqualityOpMutator.IsChecked == true) { checkedOptions.Add("2"); }
            if (RemoveNonBasicConditionalsMutator.IsChecked == true) { checkedOptions.Add("3"); }
            if (MathOperatorsMutator.IsChecked == true) { checkedOptions.Add("4"); }
            if (AssignmentExprMutator.IsChecked == true) { checkedOptions.Add("5"); }
            if (ReturnExpressionMutator.IsChecked == true) { checkedOptions.Add("6"); }
            if (VoidMethodCallMutator.IsChecked == true) { checkedOptions.Add("7"); }
            if (ClassMemberAssignDel.IsChecked == true) { checkedOptions.Add("8"); }
            if (VariableDeclarationMutator.IsChecked == true) { checkedOptions.Add("9"); }

            return checkedOptions;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BoundaryOpMutator.IsChecked = true;
            RelationalAndEqualityOpMutator.IsChecked = true;
            RemoveNonBasicConditionalsMutator.IsChecked = true;
            MathOperatorsMutator.IsChecked = true;
            AssignmentExprMutator.IsChecked = true;
            ReturnExpressionMutator.IsChecked = true;
            VoidMethodCallMutator.IsChecked = true;
            ClassMemberAssignDel.IsChecked = true;
            VariableDeclarationMutator.IsChecked = true;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (generatedMutantsListBox.SelectedItems[0] != null)
            {
                var selectedMutant = (GeneratedMutant)generatedMutantsListBox.SelectedItems[0];
                var selectedMutantName = selectedMutant.MutantName;

                var mutant = MutationAnalysisResult.GeneratedMutants
                    .Where(m => m.MutantName == selectedMutantName)
                    .FirstOrDefault();

                if (mutant != null)
                {
                    CompareMutantWithOriginalCode(mutant);
                }
                else
                {
                    MessageBox.Show(string.Format("Mutant {0} not found.",
                        generatedMutantsListBox.SelectedItems[0]));
                }
            }
            else
            {
                MessageBox.Show("Please select a mutant from the generated mutants list.");
            }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (liveMutantsListBox.SelectedItems[0] != null)
            {
                var selectedMutant = (GeneratedMutant)liveMutantsListBox.SelectedItems[0];
                var selectedMutantName = selectedMutant.MutantName;

                var mutant = MutationAnalysisResult.LiveMutants
                    .Where(m=>m.MutantName == selectedMutantName)
                    .FirstOrDefault();

                if(mutant != null)
                {
                    CompareMutantWithOriginalCode(mutant);
                }
                else
                {
                    MessageBox.Show(string.Format("Mutant {0} not found.", 
                        liveMutantsListBox.SelectedItems[0]));
                }
            }
            else
            {
                MessageBox.Show("Please select a mutant from the live mutants list.");
            }
        }

        private void CompareMutantWithOriginalCode(GeneratedMutant mutant)
        {
            var dte = GetActiveIDE();

            if (unitTestProject != null && sourceCodeProject != null)
            {
                //get the unit test project
                //create folder there called MutationAnalysis
                //get the path to that folder
                var mutationAnalysisFolderPath =
                    Path.Combine(Path.GetDirectoryName(unitTestProject.FullName),
                    "MutationAnalysis");

                if (!Directory.Exists(mutationAnalysisFolderPath))
                {
                    unitTestProject.ProjectItems.AddFolder("MutationAnalysis");
                    unitTestProject.Save();
                }

                //get the mutated and the original code 
                var mutantCode = mutant.MutatedCode;
                var originalCode = mutant.OriginalProgramCode;

                MutationAnalysisNumber = MutationAnalysisNumber + 1;

                //create two temporary classes with the code
                //called MutantCode and OriginalCode in the MutationAnalysis folder
                var mutantCodePath = Path.Combine(mutationAnalysisFolderPath,
                    string.Format("MutantCode{0}.txt", MutationAnalysisNumber));
                var originalCodePath = Path.Combine(mutationAnalysisFolderPath,
                    string.Format("OriginalCode{0}.txt", MutationAnalysisNumber));

                File.WriteAllText(mutantCodePath, mutantCode);
                File.WriteAllText(originalCodePath, originalCode);

                unitTestProject.ProjectItems.AddFromFile(mutantCodePath);
                unitTestProject.ProjectItems.AddFromFile(originalCodePath);
                unitTestProject.Save();

                CompareFiles(dte, mutantCodePath, originalCodePath);
            }
        }

        private void CompareFiles(DTE2 dte, string mutantCodePath, string originalCodePath)
        {
            if (!string.IsNullOrEmpty(mutantCodePath) && !string.IsNullOrEmpty(originalCodePath))
            {
                dte.ExecuteCommand("Tools.DiffFiles", $"\"{mutantCodePath}\" \"{originalCodePath}\"");
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var dte = GetActiveIDE();
            //dte.ExecuteCommand("CloseAll");

            if (unitTestProject == null && sourceCodeProject == null)
            {
                var projects = new List<ProjectPresentation>();
                foreach (var item in solutionProjectList.SelectedItems)
                {
                    var selectedProject = (ProjectPresentation)item;
                    selectedProject.IsSelected = true;
                    projects.Add(selectedProject);
                }
                sourceCodeProject = GetSourceCodeProject(projects);
                unitTestProject = GetUnitTestProject(projects);
            }

            if (unitTestProject != null && sourceCodeProject != null)
            {
                //delete the mutated source code class
                foreach (ProjectItem item in sourceCodeProject.ProjectItems)
                {
                    var itemName = item.Name;
                    if (itemName == "SourceCodeMutants.cs")
                    {
                        item.Delete();
                        break;
                    }
                }
                sourceCodeProject.Save();

                //delete mutated unit test project
                foreach (ProjectItem item in unitTestProject.ProjectItems)
                {
                    var itemName = item.Name;
                    if (itemName == "UnitTestMutants.cs")
                    {
                        item.Delete();
                    }

                    //delete mutation analysis folder
                    if (itemName == "MutationAnalysis")
                    {
                        MutationAnalysisNumber = 0;
                        item.Delete();
                    }
                }
                unitTestProject.Save();

                //recompile projects
                SolutionBuild2 solutionBuild2 = (SolutionBuild2)unitTestProject.DTE.Solution.SolutionBuild;
                solutionBuild2.BuildProject(solutionBuild2.ActiveConfiguration.Name,
                    unitTestProject.UniqueName, true);
                bool unitTestCompiledOK = (solutionBuild2.LastBuildInfo == 0);

                SolutionBuild2 solutionBuild = (SolutionBuild2)sourceCodeProject.DTE.Solution.SolutionBuild;
                solutionBuild.BuildProject(solutionBuild.ActiveConfiguration.Name,
                    sourceCodeProject.UniqueName, true);
                bool sourceCodeCompiledOK = (solutionBuild.LastBuildInfo == 0);

                if (unitTestCompiledOK && sourceCodeCompiledOK)
                {
                    MessageBox.Show("MTOOS deletion done! No Errors!");
                }
                else
                {
                    MessageBox.Show("Error while MTOOS deletion!");
                }
            }
            else
            {
                MessageBox.Show("Select the source code and the unit test project!");
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            MutationAnalysisResult.GeneratedMutants = new List<GeneratedMutant>();
            MutationAnalysisResult.LiveMutants = new List<GeneratedMutant>();

            liveMutantsListBox.ItemsSource = MutationAnalysisResult.LiveMutants;
            generatedMutantsListBox.ItemsSource = MutationAnalysisResult.GeneratedMutants;
        }
    }
}