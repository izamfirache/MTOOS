using EnvDTE;
using MTOOS.Extension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MTOOS.Extension.Views
{
    /// <summary>
    /// Interaction logic for StudentView.xaml
    /// </summary>
    public partial class CompareView : UserControl
    {
        public CompareView(List<Mutant> liveMutantsList)
        {
            this.InitializeComponent();
            liveMutants.ItemsSource = liveMutantsList;
        }

        private void LiveMutantslListView_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                var selectedMutant = (Mutant)item;

                //get the original program code
                originalProgram.AppendText(selectedMutant.OriginalProgramCode);

                //get the live mutant code
                liveMutant.AppendText(selectedMutant.MutatedCode);
            }
        } 
    }
}
