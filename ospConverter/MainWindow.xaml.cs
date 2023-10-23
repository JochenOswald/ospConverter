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
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.IO.Pipes;
using System.Runtime.Intrinsics.Arm;

namespace ospConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        BackgroundWorker bgConverter = new BackgroundWorker();
        int dgmWidth = 1;

        public MainWindow()
        {
            InitializeComponent();

            bgConverter.DoWork += bgConverter_DoWork;
            bgConverter.RunWorkerCompleted += bgConverter_RunWorkerCompleted;
            bgConverter.ProgressChanged += bgConverter_ProgressChanged;
            bgConverter.WorkerReportsProgress = true;
            bgConverter.WorkerSupportsCancellation = true;

        }

        private void bgConverter_DoWork(object sender, DoWorkEventArgs e)
        {
            string filePath;
            string fileName;
            int fileCounter = 0;
            string outputFileName = System.IO.Path.GetTempFileName();
            bool skipOverwrite = false;

            List<object> genericlist = e.Argument as List<object>;
            List<string> fileList = genericlist[0] as List<string>;
            bool mergeFiles = (bool)genericlist[1];
            string mergedFileName = (string)genericlist[2];
            string tempFileName = (string)genericlist[3];

            string downloadFolder = windowsHelpers.GetPath("{374DE290-123F-4565-9164-39C4925E467B}", windowsHelpers.KnownFolderFlags.DontVerify, false) + "\\ospConverter" + DateTime.Now.ToString("yyyyMMdd-hhmmss");

            if (mergeFiles)
            {
                File.Create(tempFileName).Dispose();
                if (File.Exists(mergedFileName))
                {
                    MessageBoxResult isOverwrite = MessageBox.Show(string.Format("Soll die Datei {0} überschrieben werden?", mergedFileName),"ospConverter", MessageBoxButton.YesNo);
                    if (isOverwrite == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            foreach (String file in fileList.ToList())
            {
                if ((bgConverter.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    string convertFileName = file;
                    if (file.Substring(0,1) == "<") // Wenn URL
                    {
                        XNamespace ad = "urn:ietf:params:xml:ns:metalink";
                        XElement metalink = XElement.Parse(file) as XElement;
                        string checkSum = metalink.Element(ad + "hash").Value.ToString();
                        string checkSumType = metalink.Element(ad + "hash").Attribute("type").Value.ToString();
                        IEnumerable<XElement> urls = from el in metalink.Elements(ad + "url")
                                        select el;
                        bool downloadComplete = false;
                        Directory.CreateDirectory(downloadFolder);
                        var client = new WebClient();
                        foreach (XElement url in urls)
                        {
                            string urlString = url.Value;
                            try
                            {
                                string downloadFileName = downloadFolder + "\\" + metalink.Attribute("name").Value;
                                client.DownloadFile(urlString, downloadFileName);
                                convertFileName = downloadFileName;
                                downloadComplete = compareChecksum(downloadFileName, checkSum,checkSumType);
                            }
                            catch
                            {

                            }
                        }
                    }
                    if (!File.Exists(convertFileName))
                    {
                        MessageBox.Show(string.Format("Datei {0} kann nicht gefunden werden. Die Umsetzung wird abgebrochen.", convertFileName));
                        e.Cancel = true;
                        break;
                    }
                    filePath = Path.GetDirectoryName(convertFileName);
                    fileName = Path.GetFileNameWithoutExtension(convertFileName);
                    if (mergeFiles == false)
                    {
                        outputFileName = Path.Combine(filePath, fileName) + ".txt";
                        if (File.Exists(outputFileName))
                        {
                            MessageBoxResult isOverwrite = MessageBox.Show(string.Format("Soll die Datei {0} überschrieben werden?", outputFileName), "ospConverter", MessageBoxButton.YesNo);
                            if (isOverwrite == MessageBoxResult.No)
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                    }

                    try
                    {
                        FileStream outputStream = File.OpenWrite(outputFileName);
                        outputStream.Close();
                    }
                    catch
                    {
                        MessageBox.Show(string.Format("Die Datei {0} kann nicht zum Schreiben geöffnet werden. Die Umsetzung wird abgebrochen.", convertFileName));
                    }

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "gdal\\gdal_translate.exe";
                    startInfo.Arguments = "-of XYZ -tr " + dgmWidth + " " + dgmWidth + " " + convertFileName + " " + outputFileName;

                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                    if (mergeFiles )
                    {
                        using (Stream input = File.OpenRead(outputFileName))
                        using (Stream output = new FileStream(tempFileName, FileMode.Append, FileAccess.Write, FileShare.None))
                        {
                            input.CopyTo(output); // Using .NET 4
                        }
                        File.Delete(outputFileName);
                    }
                    fileCounter++;
                    bgConverter.ReportProgress(fileCounter);
                }
            }
            string dings = Path.GetExtension(mergedFileName);
            if (Path.GetExtension(mergedFileName).ToLower() == ".sdf")
            {
                convertToStanet(tempFileName, mergedFileName);
            }
        }

        private void convertToStanet(string tempFileName, string outputFileName)
        {
            StreamWriter output = new StreamWriter(outputFileName);
            output.WriteLine("DAKTEXNET");
            output.WriteLine("VERV1.0.0");
            output.WriteLine("FMTDELI");
            output.WriteLine("ZWEDatenkonvertierung von GeoTiff");
            output.WriteLine("NUT");
            output.WriteLine("QUEospConverter");
            output.WriteLine("MEDW");
            output.WriteLine("DAT" + DateTime.Now.ToString("dd.MM.yyyy"));
            output.WriteLine("ZEI" + DateTime.Now.ToString("hh:mm:ss"));
            output.WriteLine("TTR;");
            output.WriteLine("DTR.");
            output.WriteLine("NEB");
            output.WriteLine("NEN");
            output.WriteLine("");
            output.WriteLine("REM Feldbeschreibungen Höhenfixpunkte");
            output.WriteLine("FEL;FIX;FIX;XRECHTS;N; 14;  5;Koordinate x;m");
            output.WriteLine("FEL;FIX;FIX;YHOCH;N; 14;  5;Koordinate y;m");
            output.WriteLine("FEL;FIX;FIX;GEOH;N; 10;  3;Höhe;mNN");
            output.WriteLine("FEL;FIX;FIX;LAYER;C;  5;  0;Layer;");

            using (StreamReader input = new StreamReader(tempFileName))
            {
                while (input.Peek() >= 0)
                {
                    string[] lineVals = input.ReadLine().Split(' ');
                    output.WriteLine("FIX;" + lineVals[0].ToString() + ";" + lineVals[1].ToString() + ";" + lineVals[2].ToString() + ";$ELEVPOINT_0");
                }
            }
            output.Close();
        }

        private void bgConverter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //object result = e.Result;
            Convert.Content = "Konvertieren";
            ConvertProgress.Visibility = Visibility.Hidden;
            ConvertProgress.Value = 0;
        }

        private void bgConverter_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ConvertProgress.Value = e.ProgressPercentage;
        }

        private void File_Drop(object sender, DragEventArgs e)
        {
            List<string> files = new List<string>();
            if (FileView.ItemsSource != null)
            {
                files = FileView.ItemsSource.Cast<string>().ToList();
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string fileName in fileNames)
                {
                    if (Path.HasExtension(fileName))
                    {
                        if (Path.GetExtension(fileName) == ".tif")
                        {
                            files.Add(fileName);
                        }
                        if (Path.GetExtension(fileName) ==".meta4")
                        {
                            XElement downloadFile = XElement.Load(fileName);
                            XNamespace ad = "urn:ietf:params:xml:ns:metalink";
                            IEnumerable<XElement> metalinks= from el in downloadFile.Elements(ad + "file")
                                                          select el;

                            foreach (XElement metalink in metalinks)
                            {
                                files.Add(metalink.ToString());
                            }
                        }
                    }
                    else //keine Erweiterung, sollte also ein Pfad sein
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(fileName);
                        foreach (FileInfo filePath in directoryInfo.GetFiles("*.tif"))
                        {
                            files.Add(filePath.FullName);
                        }
                    }

                }
            }
            if (files.Count > 0)
            {
                InfoArea.Visibility = Visibility.Hidden;
                FileArea.Visibility = Visibility.Visible;
                Convert.IsEnabled= true;
            }
            FileView.ItemsSource = files.Distinct();
        }


        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            List<string> files;
            files = FileView.ItemsSource.Cast<string>().ToList() as List<string>;
            var rowItem = (sender as Button).DataContext as string;
            var itemToRemove = files.Single(r => r == rowItem);
            files.Remove(itemToRemove);

            FileView.ItemsSource = null;
            FileView.ItemsSource = files;

            if (files.Count == 0)
            { 
                FileArea.Visibility = Visibility.Hidden;
                InfoArea.Visibility = Visibility.Visible;
                Convert.IsEnabled = false;
            }
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            string convertFileName;
            if (Convert.Content.ToString() == "Konvertieren")
            {
                if (MergeFiles.IsChecked == true && MergedFileName.Text.Length == 0)
                {
                    MergedFileBorder.Visibility = Visibility.Visible;
                    MergedFileName.ToolTip = "Dateiname wird benötigt";
                    return;
                }
                //wenn Erweiterung = Stanet-Datei, dann MergedFileName=temporär, Stanet-File-Variable = MergedFileName.Text
                if (Path.GetExtension(MergedFileName.Text) == ".sdf")
                {
                    convertFileName = Path.GetTempFileName();
                }
                else
                {
                    convertFileName = MergedFileName.Text;
                }
                Convert.Content = "Abbrechen";
                var checkedValue = RasterWidth.Children.OfType<RadioButton>()
                 .FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value);

                Int32.TryParse(checkedValue.Tag.ToString(), out dgmWidth);
                ConvertProgress.Maximum = FileView.Items.Count;
                ConvertProgress.Visibility = Visibility.Visible;
                List<object> arguments = new List<object>();
                arguments.Add(FileView.ItemsSource.Cast<string>().ToList());      //argument 1
                arguments.Add(MergeFiles.IsChecked); //argument 2
                arguments.Add(MergedFileName.Text);
                arguments.Add(convertFileName);
                bgConverter.RunWorkerAsync(argument: arguments);
                // Wenn Fertig und Stanet-Datei
            }
            else
            {
                bgConverter.CancelAsync();
                Convert.Content = "Konvertieren";
            }
            
        }

        private void InfoDialog_Click(object sender, RoutedEventArgs e)
        {
            InfoDialog infoDlg = new InfoDialog();
            infoDlg.ShowDialog();
        }

        private void FileSelect_Click(object sender, RoutedEventArgs e)
        {
            string fileName = getSaveFile();
            if (fileName != "")
            {
                MergeFiles.IsChecked = true;
                MergedFileName.Text = fileName;
            }
        }

        private string getSaveFile()
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Textdokumente|*.txt|Koordinatendateien|*.xyz|Stanet-Datei|*.sdf|Alle Dateien|*.*";
            saveFileDialog.InitialDirectory=MergedFileName.Text;
            saveFileDialog.ShowDialog();
            return saveFileDialog.FileName;
        }

        private void MergedFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            MergedFileName.ToolTip = null;
            MergedFileBorder.Visibility = Visibility.Hidden;
        }

        private bool compareChecksum(string downloadFileName, string expectedHash, string hashType)
        {
            if (hashType == "sha-256")
            {
                SHA256 sha256 = SHA256.Create();
                FileInfo downloadFileInfo = new FileInfo(downloadFileName);
                FileStream downloadFileStream = downloadFileInfo.Open(FileMode.Open);
                byte[] hashValue = sha256.ComputeHash(downloadFileStream);
                string actualHash = PrintByteArray(hashValue).ToLower();
                downloadFileStream.Close();
                if (actualHash.ToLower() == expectedHash.ToLower())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                MessageBox.Show("Integrität der Datei kann nicht geprüft werden.");
                return false;
            }
        }

        private static string PrintByteArray(byte[] array)
        {
            string hash = "";
            for (int i = 0; i < array.Length; i++)
            {
                hash = hash + string.Format($"{array[i]:X2}");
            }
            return hash;
        }
    }
}
