namespace MTOOS.Extension
{
    using EnvDTE;
    using EnvDTE80;
    using MTOOS.Extension.Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MutantKillerWindowControl.
    /// </summary>
    public partial class MutantKillerWindowControl : UserControl
    {
        public List<string> CheckedOptions;
        public List<Mutant> GeneratedMutantList;

        /// <summary>
        /// Initializes a new instance of the <see cref="MutantKillerWindowControl"/> class.
        /// </summary>
        public MutantKillerWindowControl()
        {
            this.InitializeComponent();
            solutionProjectList.ItemsSource = GetSolutionProjects();
        }

        private List<ProjectPresentation> GetSolutionProjects()
        {
            var projects = new List<ProjectPresentation>();
            var dte = (DTE2)Microsoft.VisualStudio.Shell.ServiceProvider
                    .GlobalProvider.GetService(typeof(EnvDTE.DTE));

            foreach(Project p in dte.Solution.Projects)
            {
                projects.Add(new ProjectPresentation()
                {
                    Name = p.Name,
                    Type = p.Name.Contains("UnitTest") ? "UnitTest" : "SourceCode"
                });
            }

            return projects;
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
                foreach (ProjectPresentation p in projects)
                {
                    if (p.Type == "SourceCode")
                    {
                        foreach (Project sp in dte.Solution.Projects)
                        {
                            if (sp.Name == p.Name)
                            {
                                sourceCodeProject = sp;
                            }
                        }
                    }
                    else
                    {
                        foreach (Project sp in dte.Solution.Projects)
                        {
                            if (sp.Name == p.Name)
                            {
                                unitTestProject = sp;
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please check a source code project and a unit test project before starting mutation testing.",
                    "No projects selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            CheckedOptions = GetCheckedOptions();

            if (CheckedOptions.Count != 0 && projects.Count == 2)
            {
                MutationTestingManager mutationTestingManager = new MutationTestingManager();
                //MutantList.ItemsSource = mutationTestingManager
                //    .PerformMutationTestingOnProject(dte, "", CheckedOptions);
            }
            else
            {
                MessageBox.Show("Please check the mutations you want to perform.", 
                    "No options checked.", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private List<string> GetCheckedOptions()
        {
            var checkedOptions = new List<string>();
            if (AdditiveAndMultiplicativeOp.IsChecked == true) { checkedOptions.Add("1"); }
            if (AssignmentExprMutator.IsChecked == true) { checkedOptions.Add("2"); }
            if (RelationalAndEqualityOp.IsChecked == true) { checkedOptions.Add("2"); }
            if (ThisStatementDeletion.IsChecked == true) { checkedOptions.Add("2"); }

            return checkedOptions;
        }
    }
}