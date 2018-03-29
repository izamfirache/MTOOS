namespace MTOOS.Extension
{
    using EnvDTE;
    using EnvDTE80;
    using MTOOS.Extension.Models;
    using MTOOS.Extension.Views;
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
        private MutationAnalysisView _mutationAnalysisView;
        private CompareView _compareView;

        /// <summary>
        /// Initializes a new instance of the <see cref="MutantKillerWindowControl"/> class.
        /// </summary>
        public MutantKillerWindowControl()
        {
            this.InitializeComponent();
            _mutationAnalysisView = new MutationAnalysisView();
            content.Content = _mutationAnalysisView; // set the default view
        }

        private void MutationAnalysisItem_Click(object sender, RoutedEventArgs e)
        {
            content.Content = _mutationAnalysisView;
        }

        private void CompareItem_Click(object sender, RoutedEventArgs e)
        {
            if(_mutationAnalysisView.GeneratedMutantList.Count != 0)
            {
                _compareView = new CompareView(_mutationAnalysisView.GeneratedMutantList);
                content.Content = _compareView;
            }
            else
            {
                _compareView = new CompareView(new List<GeneratedMutant>());
                content.Content = _compareView;
            }
        }

        private void MenuItem_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }
}