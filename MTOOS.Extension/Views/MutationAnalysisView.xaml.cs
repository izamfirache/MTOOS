using EnvDTE;
using EnvDTE80;
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
                sourceCodeProject = GetSourceCodeProject(dte, projects);
                unitTestProject = GetUnitTestProject(dte, projects);
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

        private Project GetUnitTestProject(DTE2 dte, List<ProjectPresentation> projects)
        {
            foreach (ProjectPresentation p in projects)
            {
                if (p.Type == "UnitTest")
                {
                    foreach (Project sp in dte.Solution.Projects)
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

        private Project GetSourceCodeProject(DTE2 dte, List<ProjectPresentation> projects)
        {
            foreach (ProjectPresentation p in projects)
            {
                if (p.Type == "SourceCode")
                {
                    foreach (Project sp in dte.Solution.Projects)
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

        private List<ProjectPresentation> GetSolutionProjects()
        {
            var projects = new List<ProjectPresentation>();
            var dte = (DTE2)Microsoft.VisualStudio.Shell.ServiceProvider
                    .GlobalProvider.GetService(typeof(EnvDTE.DTE));

            foreach (Project p in dte.Solution.Projects)
            {
                projects.Add(new ProjectPresentation()
                {
                    Name = p.Name,
                    Type = p.Name.Contains("UnitTest") ? "UnitTest" : "SourceCode"
                });
            }

            return projects;
        }

        private List<string> GetCheckedOptions()
        {
            var checkedOptions = new List<string>();
            if (BoundaryOpMutator.IsChecked == true) { checkedOptions.Add("1"); }
            if (RelationalAndEqualityOpMutator.IsChecked == true) { checkedOptions.Add("2"); }
            if (RemoveNonBasicConditionalsMutator.IsChecked == true) { checkedOptions.Add("3"); }
            if (MathOperatorsMutator.IsChecked == true) { checkedOptions.Add("4"); }

            return checkedOptions;
        }
    }
}
