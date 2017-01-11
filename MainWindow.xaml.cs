using MCNPFileEditor.DataClassAndControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Threading;
using System.Collections.ObjectModel;

namespace MCNPFileEditor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<string> OpenFileList = new ObservableCollection<string>();
        ObservableCollection<string> AddFileList = new ObservableCollection<string>();
        int RepOrganName = -1;
        string OrganNameFile = "";
        string mode;

        PhantomsCollection phantomsCollection;

        int finishWorkCount = 0; // 进度条指示

        public MainWindow()
        {
            InitializeComponent();
            InitializePara();
        }

        void InitializePara()
        {
            OrgannameFile.DataContext = OrganNameFile;
            
            if (((App)Application.Current).phantomsCollection == null)
            {
                ((App)Application.Current).phantomsCollection = new PhantomsCollection();
            }
            if (((App)Application.Current).phantomsCollection.AllPhantoms == null)
            {
                ((App)Application.Current).phantomsCollection.AllPhantoms = new System.Collections.ObjectModel.ObservableCollection<Phantom>();
            }

            phantomsCollection = ((App)Application.Current).phantomsCollection;
        }

        // 打开一系列文件
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileList.Clear();
            RepOrganName = Convert.ToInt32(RepCellIndexTextBox.Text);
            finishWorkCount = 0;
            if (ProcessMode.SelectedIndex == -1 || ProcessMode.SelectedIndex == 0)
            {
                mode = "simple";
            }
            else
            {
                mode = "complicate";
            }

            if (OrgannameFile.Content != null || !((string)OrgannameFile.Content).Equals(""))
            {
                // OrganNameFile = (string) OrgannameFile.Content;

                System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
                {
                    Multiselect = true,
                    RestoreDirectory = true,
                    CheckPathExists = true,
                    CheckFileExists = true,
                };

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (ofd.FileNames.Count() != 0)
                    {
                        foreach (var item in ofd.FileNames)
                        {
                            OpenFileList.Add(item);
                        }
                        
                        SelectedFileListView.ItemsSource = OpenFileList;
                        // BackgroundWorker backgroundWorker = (BackgroundWorker)this.FindResource("backgroundworker");
                        // workProgressBar.Maximum = OpenFileList.Count;
                        // foreach (var item in OpenFileList)
                        // {
                        //     backgroundWorker.RunWorkerAsync(item);
                        //     
                        //     workProgressBar.Value = finishWorkCount;
                        // }
                        List<Thread> threadList = new List<Thread>();
                        foreach (var item in OpenFileList)
                        {
                            string lengthText = borderExtendLengthText.Text;
                            short voidIndex = Convert.ToInt16(voidText.Text);
                            Thread newThread = new Thread(() => NewThread_DoWork(item, lengthText, voidIndex));
                            threadList.Add(newThread);
                            newThread.Start();
                        }

                        foreach (var item in threadList)
                        {
                            item.Join();
                            // finishWorkCount++;
                        }

                        PhantomsCollection.AutoDeleteRepeatPhantom(phantomsCollection);
                        PhantomsCollection.UniformOrganColorList(phantomsCollection);
                    }
                    else
                    {
                        MessageBox.Show("至少选取一个文件");
                        return;
                    }
                }
            }
            else
            {

            }
        }

        // 导入一组数据
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AddFileList.Clear();
            RepOrganName = Convert.ToInt32(RepCellIndexTextBox.Text);
            finishWorkCount = 0;
            if (ProcessMode.SelectedIndex == -1 || ProcessMode.SelectedIndex == 0)
            {
                mode = "simple";
            }
            else
            {
                mode = "complicate";
            }

            if (OrgannameFile.Content != null || !((string)OrgannameFile.Content).Equals(""))
            {
                // OrganNameFile = (string)OrgannameFile.Content;

                System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
                {
                    Multiselect = true,
                    RestoreDirectory = true,
                    CheckPathExists = true,
                    CheckFileExists = true,
                };

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (ofd.FileNames.Count() != 0)
                    {
                        foreach (var item in ofd.FileNames)
                        {
                            AddFileList.Add(item);
                        }

                        SelectedFileListView.ItemsSource = AddFileList;
                        // BackgroundWorker backgroundWorker = (BackgroundWorker)this.FindResource("backgroundworker");
                        // workProgressBar.Maximum = OpenFileList.Count;
                        // foreach (var item in OpenFileList)
                        // {
                        //     backgroundWorker.RunWorkerAsync(item);
                        //     
                        //     workProgressBar.Value = finishWorkCount;
                        // }
                        List<Thread> threadList = new List<Thread>();
                        foreach (var item in AddFileList)
                        {
                            string lengthText = borderExtendLengthText.Text;
                            short voidIndex = Convert.ToInt16(voidText.Text);
                            Thread newThread = new Thread(() => NewThread_DoWork(item, lengthText, voidIndex));
                            threadList.Add(newThread);
                            newThread.Start();
                        }

                        foreach (var item in threadList)
                        {
                            item.Join();
                            // finishWorkCount++;
                        }

                        PhantomsCollection.AutoDeleteRepeatPhantom(phantomsCollection);
                        PhantomsCollection.UniformOrganColorList(phantomsCollection);
                    }
                    else
                    {
                        MessageBox.Show("至少选取一个文件");
                        return;
                    }
                }
                else
                {

                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Multiselect = false,
                RestoreDirectory = true,
                CheckPathExists = true,
                CheckFileExists = true,
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(ofd.FileName))
                {
                    OrganNameFile = ofd.FileName;
                    
                    OrgannameFile.Content = System.IO.Path.GetFileName(OrganNameFile);
                }
                else
                {
                    return;
                }
            }
        }

        private void BackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            //Phantom newPhantom;
            //string inputPhantomFilePath = (string)e.Argument;

            //newPhantom = new Phantom(inputPhantomFilePath, RepOrganName, mode, OrganNameFile);

            //phantomsCollection.AllPhantoms.Add(newPhantom);
        }

        private void BackgroundWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {

        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //finishWorkCount++;
        }

        private void NewThread_DoWork(object obj, string extendLength, short voidIndex)
        {
            Phantom newPhantom;
            string inputPhantomFilePath = (string)obj;

            int extendLengthValue = -1;
            try
            {
                extendLengthValue = Convert.ToInt32(extendLength);
            }
            catch (Exception)
            {
                return;
            }

            newPhantom = new Phantom(inputPhantomFilePath, RepOrganName, mode, OrganNameFile);
            
            if (newPhantom.RepeatStructureInAPhantom != null && newPhantom.RepeatStructureInAPhantom.RepeatMatrix != null && extendLengthValue >= 1)
            {
                newPhantom.RepeatStructureInAPhantom.ExtendBorder(extendLengthValue, voidIndex);
            }

            lock (phantomsCollection)
            {
                phantomsCollection.AllPhantoms.Add(newPhantom);
                finishWorkCount++;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MCNPFileEditor.CrossSectionImageShow.CrossSectionMainWindow newCrossSectionMainWindow = new CrossSectionImageShow.CrossSectionMainWindow();
            newCrossSectionMainWindow.ShowDialog();
        }
    }
}
