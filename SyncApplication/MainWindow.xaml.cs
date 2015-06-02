using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using MessageBox = System.Windows.MessageBox;

namespace SyncApplication
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
        {

        public StreamReader FileIpAddress = null;
        public string LineFromFitnesseFolder = null;
        public string eachLineFromIPAddressFile;


        public MainWindow()
            {
            InitializeComponent();
            }


        private void BrowseButton_Click(object sender, RoutedEventArgs e)
            {
            // Create OpenFileDialog
            //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            var dlg = new FolderBrowserDialog();

            // Set filter for file extension and default file extension
            //dlg.DefaultExt = ".txt";
            //dlg.Filter = "Text documents (.txt)|*.txt";

            // Display OpenFileDialog by calling ShowDialog method
            //Nullable<bool> result = dlg.ShowDialog();
            dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            //if (result != null)
            //{
            // Open document
            //string filename = dlg.FileName;
            //FileNameTextBox.Text = filename;
            browseTextbox.Text = dlg.SelectedPath;
            //}
            }


        private void SyncButton_Click(object sender, RoutedEventArgs e)
            {

            textBlock.Text = "";

            // Read the file and display it line by line.
            if (browseTextbox.Text != "")
                {

                try
                    {
                    FileIpAddress = new StreamReader("IP_Address.txt");
                    }
                catch
                    {
                    textBlock.Text = "Make sure that the file IP_Address.txt exists in the same location.\n";
                    return;
                    }
                try
                    {
                    var fileFitnesseFolder = new StreamReader("Desired_Folder_Structure.txt");
                    LineFromFitnesseFolder = fileFitnesseFolder.ReadLine();
                    }
                catch
                    {
                    textBlock.Text = "Make sure that the file Desired_Folder_Structure.txt exists in the same location.\n";
                    return;
                    }

                try
                    {
                    textBlock.Inlines.Add("Syncing : ");

                    while ((eachLineFromIPAddressFile = FileIpAddress.ReadLine()) != null)
                        {
                        if (eachLineFromIPAddressFile.Contains(@"\\")) { continue; }
                        var strCmdText = "/c xcopy " + browseTextbox.Text + @" \\" + eachLineFromIPAddressFile + @"\d$\" + @LineFromFitnesseFolder + " /e /y /EXCLUDE:Exclude.txt";

                        /*//Working but it does not know when the sync is complete.
                        Process.Start("cmd.exe", strCmdText);
                        textBlock.Inlines.Add(Line + ", ");
                        //*/

                        Thread childThread = new Thread(() => LaunchCommandPrompt(eachLineFromIPAddressFile, strCmdText));
                        childThread.Start();
                        textBlock.Inlines.Add(eachLineFromIPAddressFile + ", ");
                        Thread.Sleep(200);

                        }
                    textBlock.Text = textBlock.Text.Substring(0, textBlock.Text.Length - 2);
                    FileIpAddress.Close();
                    }
                catch
                    {
                    textBlock.Text = "Check the content of the two input text files. They may not be in right format.\n";
                    }
                }
            else
                {
                textBlock.Text = "Please select the folder by clicking browse first.\n";
                }
            }

        private void LaunchCommandPrompt(string ip, string strCmdTxt)
            {
            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = strCmdTxt
            };
            var process = Process.Start(processInfo);
            process.WaitForExit();
            this.Dispatcher.Invoke((Action)(() =>
                {
                    textBlock.Inlines.Add("\n" + ip + " synced.");
                }));
            }


        public string CurrentDirectory
            {
            get
                {
                return Environment.CurrentDirectory;
                }
            }

        private void buttonClearLog_Click(object sender, RoutedEventArgs e)
            {
            textBlock.Text = "";
            }

        private void btnReportSync_Click(object sender, RoutedEventArgs e)
            {
            //Get the latest result file
            var sortedFiles = new DirectoryInfo("sync").GetFiles()
                                                  .OrderBy(f => f.LastWriteTime)
                                                  .ToList();

            var tempCount = sortedFiles.Count - 1;
            const string modifiedFileNameString = "resultTaken";
            /*for (;sortedFiles[tempCount].Name.Contains(modifiedFileNameString);--tempCount )
            {

            }*/
            if (sortedFiles[tempCount].Name.Contains(modifiedFileNameString))
                {
                MessageBox.Show("No Run");
                return;
                }
            var resultFile = sortedFiles[tempCount].Name;
            MessageBox.Show(resultFile);

            //Load the latest result file
            var xml = new XmlDocument();
            xml.Load("sync\\" + resultFile);

            //Rename the file to a temp file
            File.Move("sync\\" + resultFile, "sync\\" + modifiedFileNameString + "_" + resultFile);

            //Get the right, wrong, exception count for the test case
            var root = xml.DocumentElement;
            if (root == null) return;
            var pageHistoryNodes = root.SelectNodes("//pageHistoryReference");

            int countRight = 0, countWrong = 0, countException = 0;

            foreach (var resultCounts in from XmlNode pageHistoryNode in pageHistoryNodes let xmlElement = pageHistoryNode["name"] where xmlElement != null && xmlElement.InnerText.Contains("LoginTest") let element = pageHistoryNode["counts"] where element != null select element.ChildNodes)
                {
                foreach (XmlNode result in resultCounts)
                    {
                    var count = Convert.ToInt32(result.InnerText);
                    switch (result.Name)
                        {
                        case "right":
                            countRight = count;
                            break;
                        case "wrong":
                            countWrong = count;
                            break;
                        case "ignores":
                            break;
                        case "exceptions":
                            countException = count;
                            break;
                        }
                    }

                //Decide Passed, Failed, Exception
                if (countException == 0 && countWrong == 0 && countRight != 0)
                    {
                    MessageBox.Show("Passed");
                    }
                else if (countException != 0)
                    {
                    MessageBox.Show("Excepton");
                    }
                else if (countWrong != 0)
                    {
                    MessageBox.Show("Failed");
                    }
                }
            }



        }

    }
