using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using MTOOS.Extension.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public List<GeneratedMutant> GeneratedMutantList = new List<GeneratedMutant>();
        private List<Project> SoulutionProjects;
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

            Project sourceCodeProject;
            Project unitTestProject;
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
                        GeneratedMutantList = mutationTestingManager
                            .PerformMutationTestingOnProject(dte, sourceCodeProject,
                                unitTestProject, CheckedOptions);
                        MutantList.ItemsSource = GeneratedMutantList;
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
    }
}