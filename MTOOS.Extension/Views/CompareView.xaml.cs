using EnvDTE;
using MTOOS.Extension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public CompareView(List<GeneratedMutant> liveMutantsList)
        {
            this.InitializeComponent();
            liveMutants.ItemsSource = liveMutantsList;
        }

        private void LiveMutantslListView_Click(object sender, RoutedEventArgs e)
        {
            liveMutant.Document.Blocks.Clear();
            originalProgram.Document.Blocks.Clear();

            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                var selectedMutant = (GeneratedMutant)item;

                //get the original program code
                var originalProgramLines = selectedMutant.OriginalProgramCode
                    .Split('\n').Where(line => line != "\r").ToArray();
                
                //get the live mutant code
                var liveMutantLines = selectedMutant.MutatedCode
                    .Split('\n').Where(line => line != "\r").ToArray();

                if (selectedMutant.HaveDeletedStatement)
                {
                    var formatedMutant = new string[liveMutantLines.Count() + 1];

                    //manually indent the code
                    char lastCh = liveMutantLines[0][liveMutantLines[0].Length - 2];
                    if(lastCh == '{')
                    {
                        //shift the lines with one position
                        for(int j = 1; j < liveMutantLines.Count(); j++)
                        {
                            formatedMutant[j + 1] = liveMutantLines[j];
                        }
                        formatedMutant[0] = liveMutantLines[0].Replace('{', ' ');
                        formatedMutant[1] = "{";
                    }

                    liveMutantLines = formatedMutant;
                    originalProgramLines[1].Replace('\r', ' ');
                }

                //compare and highlight differences
                for (int i = 0; i < originalProgramLines.Count(); i++)
                {
                    if (originalProgramLines[i] != "\r" && liveMutantLines[i] != "\r")
                    {
                        var originalProgramParagraph = new Paragraph(new Run(originalProgramLines[i]));
                        var liveMutantLineParagraph = new Paragraph(new Run(liveMutantLines[i]));

                        if (originalProgramLines[i] != liveMutantLines[i])
                        {
                            originalProgramParagraph.Foreground = Brushes.Red;
                            liveMutantLineParagraph.Foreground = Brushes.Red;
                        }

                        originalProgram.Document.Blocks.Add(originalProgramParagraph);
                        liveMutant.Document.Blocks.Add(liveMutantLineParagraph);
                    }
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            liveMutant.Document.Blocks.Clear();
            originalProgram.Document.Blocks.Clear();
        }

        private void RichTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var textToSync = (sender == originalProgram) ? liveMutant : originalProgram;

            textToSync.ScrollToVerticalOffset(e.VerticalOffset);
            textToSync.ScrollToHorizontalOffset(e.HorizontalOffset);
        }
    }
}
