using IOCTalk.StreamAnalyzer.Implementation;
using Microsoft.Win32;
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

namespace IOCTalk.StreamAnalyzer
{
    /// <summary>
    /// Interaction logic for FlowRateAnalysisControl.xaml
    /// </summary>
    public partial class FlowRateAnalysisControl : UserControl
    {
        public FlowRateAnalysisControl()
        {
            InitializeComponent();
        }

        private void ButtonExportFileTimeRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridFlowRates.SelectedItem is FlowRate)
            {
                FlowRate flowRate = DataGridFlowRates.SelectedItem as FlowRate;
                StreamSession session = (StreamSession)DataContext;


                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Select Target Export File";
                openFile.CheckFileExists = false;
                openFile.CheckPathExists = true;
                openFile.DefaultExt = ".txt";
                openFile.FileName = System.IO.Path.GetFileNameWithoutExtension(MainWindow.Analyzer.LastFilePath) + "_part_" + flowRate.Time.ToString("hhmmss") + ".txt";

                var result = openFile.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    string targetPath = openFile.FileName;

                    this.ButtonExportFileTimeRange.IsEnabled = false;
                    MainWindow.Instance.ShowPleaseWait();
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            MainWindow.Analyzer.ExportFlowRateRows(session, flowRate, targetPath);
                        }
                        catch (Exception ex)
                        {
                            MainWindow.Instance.ShowError(ex.ToString());
                        }
                    }).ContinueWith(new Action<Task>((Task t) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MainWindow.Instance.HidePleaseWait();
                            this.ButtonExportFileTimeRange.IsEnabled = true;
                            MessageBox.Show("File part successfully exported to: " + targetPath, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }));
                    }));
                }
            }
            else
            {
                MessageBox.Show("Please select a flow rate!");
            }
        }
    }
}
