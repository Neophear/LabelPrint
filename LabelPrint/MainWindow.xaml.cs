using bpac;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace LabelPrint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsBusy.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register("IsBusy", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadFromCSV(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "CSV filer|*.csv|Alle filer|*.*"
            };

            if (dialog.ShowDialog() ?? false)
            {
                try
                {
                    FileInfo fi = new FileInfo(dialog.FileName);

                    if (fi.Length > 10_485_760)
                        throw new Exception("Filen er over 10mb og kan ikke åbnes!");

                    using (var fs = fi.OpenText())
                    {
                        string l = "";
                        while ((l = fs.ReadLine()) != null)
                            wtxtInput.AppendText($"{l}\n");
                    }
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("Filen kunne ikke findes!");
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("Mappen kunne ikke findes!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void PrintLabels(object sender, RoutedEventArgs e)
        {
            IsBusy = true;

            List<(string name, string serialnumber, string classification)> input = new List<(string, string, string)>(wtxtInput.LineCount);

            for (int i = 0; i < wtxtInput.LineCount; i++)
            {
                string[] items = wtxtInput.GetLineText(i).Split(';');

                if (items.Length == 3)
                    input.Add((items[0], items[1], items[2]));
            }

            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += Worker_DoWork;
                worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
                worker.RunWorkerAsync(input);
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show(e.Error == null ? "Sendt til printeren!" : e.Error.Message);
            IsBusy = false;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<(string name, string serialnumber, string classification)> input = (List<(string, string, string)>)e.Argument;

            if (input.Count > 0)
            {
                try
                {
                    PrintLabels(input);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    throw new Exception("B-pac er ikke installeret!");
                }
            }
        }

        private void PrintLabels(List<(string name, string serialnumber, string classification)> list)
        {
            Document doc = new Document();
            string printerName = doc.GetPrinterName();

            if (doc.Printer.IsPrinterOnline(printerName))
            {
                //string strFilePath = @"\\10.10.10.10\admin\12. ITMat\bin\TRR-MAT-LABEL.lbx";
                string strFilePath = "TRR-MAT-LABEL.lbx";

                // Open template
                if (doc.Open(strFilePath))
                {
                    if (doc.GetObject("ComputerName") == null)
                        throw new Exception("Mangler objekt ComputerName i template");

                    if (doc.GetObject("Barcode") == null)
                        throw new Exception("Mangler objekt Barcode i template");

                    if (doc.GetObject("Classification") == null)
                        throw new Exception("Mangler objekt Classification i template");

                    // Print setting start
                    doc.StartPrint("", PrintOptionConstants.bpoQuality | PrintOptionConstants.bpoCutAtEnd | PrintOptionConstants.bpoAutoCut);

                    foreach (var label in list)
                    {
                        doc.GetObject("ComputerName").Text = label.name;
                        doc.GetObject("Barcode").Text = label.serialnumber;
                        doc.GetObject("Classification").Text = label.classification;

                        // Adds a print job (1 job is printed)
                        doc.PrintOut(1, PrintOptionConstants.bpoQuality | PrintOptionConstants.bpoCutAtEnd | PrintOptionConstants.bpoAutoCut);
                    }

                    // Print setting end (=Print start)
                    doc.EndPrint();

                    // Close template
                    doc.Close();
                }
                else
                    throw new Exception("Mangler template TRR-MAT-LABEL.lbx\n(skal ligge i samme mappe som applikationen)");
            }
            else
                throw new Exception($"{printerName} er offline");
        }

        [ValueConversion(typeof(bool), typeof(bool))]
        public class InverseBooleanConverter : IValueConverter
        {
            #region IValueConverter Members

            public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                if (targetType != typeof(bool))
                    throw new InvalidOperationException("The target must be a boolean");

                return !(bool)value;
            }

            public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }

            #endregion
        }
    }
}
