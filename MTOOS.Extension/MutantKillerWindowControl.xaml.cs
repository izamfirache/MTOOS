namespace MTOOS.Extension
{
    using MTOOS.Extension.Models;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MutantKillerWindowControl.
    /// </summary>
    public partial class MutantKillerWindowControl : UserControl
    {
        public Dictionary<string, bool> CheckedOptions;
        public List<Mutant> GeneratedMutantList;

        /// <summary>
        /// Initializes a new instance of the <see cref="MutantKillerWindowControl"/> class.
        /// </summary>
        public MutantKillerWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void StartMutationTesting(object sender, RoutedEventArgs e)
        {
            CheckedOptions = new Dictionary<string, bool>()
            {
                {
                    AdditiveAndMultiplicativeOp.Name,
                    AdditiveAndMultiplicativeOp.IsChecked == true
                },
                {
                    AssignmentExprMutator.Name,
                    AssignmentExprMutator.IsChecked == true
                },
                {
                    RelationalAndEqualityOp.Name,
                    RelationalAndEqualityOp.IsChecked == true
                },
                {
                    ThisStatementDeletion.Name,
                    ThisStatementDeletion.IsChecked == true
                }
            };

            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "MutantKillerWindow");

            //MutantList.ItemsSource = items;
        }
    }
}