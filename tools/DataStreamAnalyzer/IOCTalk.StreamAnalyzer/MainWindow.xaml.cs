using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using IOCTalk.StreamAnalyzer.Implementation;
using System.Threading.Tasks;
using System.Diagnostics;

namespace IOCTalk.StreamAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static StreamAnalyzerService analyzer = new StreamAnalyzerService();
        private IList<StreamSession> streamSessions;

        private static MainWindow instance;

        public MainWindow()
        {
            InitializeComponent();

            instance = this;

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location);
            Title += fvi.ProductVersion;
        }



        public bool FilterTime
        {
            get { return (bool)GetValue(FilterTimeProperty); }
            set { SetValue(FilterTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FilterTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterTimeProperty =
            DependencyProperty.Register("FilterTime", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public static StreamAnalyzerService Analyzer
        {
            get { return analyzer; }
        }


        public bool FilterFlowRate
        {
            get { return (bool)GetValue(FilterFlowRateProperty); }
            set { SetValue(FilterFlowRateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FilterFlowRate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterFlowRateProperty =
            DependencyProperty.Register("FilterFlowRate", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));


        public static MainWindow Instance
        {
            get
            {
                return instance;
            }
        }


        private void ButtonAnalyzeDataStream_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select IOC-Talk Data Stream File";
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.DefaultExt = ".dlog";
            openFile.Filter = "IOC-Talk Data Stream Logs (.dlog)|*.dlog";

            var result = openFile.ShowDialog();

            if (result.HasValue && result.Value)
            {
                this.TabMainControl.IsEnabled = false;
                this.IsEnabled = false;
                this.DataContext = null;
                TimeSpan? roundTripTimeFilter = null;
                if (FilterTime)
                {
                    roundTripTimeFilter = TimeSpan.Parse(TextBlockMinRoundTripTime.Text);
                }
                int? flowRateFilter = null;
                if (FilterFlowRate)
                {
                    flowRateFilter = int.Parse(TextBloxMinFlowRateCount.Text);
                }
                string fileName = openFile.FileName;

                //// export test code
                FileExportFilter fileExport = null;
                //FileExportFilter fileExport = new FileExportFilter
                //{
                //    MessageNames = new string[] { "Post(x)" },
                //    ExportDirectory = $".{System.IO.Path.DirectorySeparatorChar}FileExportFilter",
                //    //GroupByKeys = new string[] { "Context", "DevId", "Protocol" },
                //    GroupByKeys = new string[] { "DevId", "Protocol" },
                //    IncludeResponse = true,
                //    Conditions = new FilterItem[]
                //    {
                //        new FilterItem
                //        {
                //            Key ="Context",
                //            Value = "x"
                //        }
                //    },
                //    SeparateOnKey = "Message",
                //    SeparateOnKeyValue = "",
                //    ExportOnlyKey = "Message",
                //    ExportWithSpaceSequence = 2
                //};

                ShowPleaseWait();
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        StringBuilder sbErrors;
                        streamSessions = analyzer.AnalyzeDataStreamSession(fileName, roundTripTimeFilter, flowRateFilter, OnProgressUpdate, fileExport, out sbErrors);
                        ShowResults(streamSessions, sbErrors);
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex.ToString());
                    }
                });
            }
        }

        public void ShowPleaseWait()
        {
            this.PleaseWaitLabel.Content = "Please wait";
            this.PleaseWaitLabel.Visibility = Visibility.Visible;
        }
        public void HidePleaseWait()
        {
            this.PleaseWaitLabel.Visibility = Visibility.Collapsed;
        }

        private void OnProgressUpdate(int percentage)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PleaseWaitLabel.Content = $"Please wait... {percentage} % done";
            }));
        }

        private void ShowResults(IList<StreamSession> streamSessions, StringBuilder sbErrors)
        {
            if (Dispatcher.CheckAccess())
            {
                ComboBoxSessions.ItemsSource = null;
                ComboBoxSessions.ItemsSource = streamSessions;
                ComboBoxSessions.IsEnabled = true;
                this.TextBoxErrors.Text = sbErrors.ToString();
                this.IsEnabled = true;
                this.PleaseWaitLabel.Visibility = Visibility.Collapsed;
                this.ButtonMergeSessions.IsEnabled = true;
            }
            else
            {
                Dispatcher.BeginInvoke(new Action<IList<StreamSession>, StringBuilder>(ShowResults), streamSessions, sbErrors);
            }
        }

        private void ShowResultsAllSessions(IList<StreamSession> streamSessions)
        {
            if (Dispatcher.CheckAccess())
            {
                ComboBoxSessions.ItemsSource = null;
                ComboBoxSessions.ItemsSource = streamSessions;
                ComboBoxSessions.IsEnabled = true;
                this.IsEnabled = true;
                HidePleaseWait();
                this.ButtonMergeSessions.IsEnabled = false;

                ComboBoxSessions.SelectedIndex = streamSessions.Count - 1;
            }
            else
            {
                Dispatcher.BeginInvoke(new Action<IList<StreamSession>>(ShowResultsAllSessions), streamSessions);
            }
        }


        public void ShowError(string error)
        {
            if (Dispatcher.CheckAccess())
            {
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.IsEnabled = true;
                this.PleaseWaitLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                Dispatcher.BeginInvoke(new Action<string>(ShowError), error);
            }
        }

        public void ShowInfo(string message)
        {
            if (Dispatcher.CheckAccess())
            {
                MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action<string>(ShowInfo), message);
            }
        }


        private void ComboBoxSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StreamSession session = ComboBoxSessions.SelectedItem as StreamSession;

            if (session != null)
            {
                this.DataContext = session;
                TabMainControl.IsEnabled = true;
            }
        }

        private void ButtonMergeSessions_Click(object sender, RoutedEventArgs e)
        {
            ShowPleaseWait();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    analyzer.MergeSessions(streamSessions);
                    ShowResultsAllSessions(streamSessions);
                }
                catch (Exception ex)
                {
                    ShowError(ex.ToString());
                }
            });

            this.ButtonMergeSessions.IsEnabled = false;
        }

        private void ButtonExportSessionMsg_Click(object sender, RoutedEventArgs e)
        {
            StreamSession session = (StreamSession)DataContext;

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select Target Export File";
            openFile.CheckFileExists = false;
            openFile.CheckPathExists = true;
            openFile.DefaultExt = ".txt";
            openFile.FileName = System.IO.Path.GetFileNameWithoutExtension(MainWindow.Analyzer.LastFilePath) + "_sessionID_" + session.SessionId + ".txt";

            var result = openFile.ShowDialog();

            if (result.HasValue && result.Value)
            {
                string targetPath = openFile.FileName;

                this.ButtonExportSessionMsg.IsEnabled = false;
                MainWindow.Instance.ShowPleaseWait();
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        MainWindow.Analyzer.ExportSessionRows(session, targetPath);
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
                        this.ButtonExportSessionMsg.IsEnabled = true;
                        MessageBox.Show("File part successfully exported to: " + targetPath, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }));
                }));
            }
        }
    }
}
