using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MCNPFileEditor.DataClassAndControl;

namespace MCNPFileEditor.CrossSectionImageShow
{
    /// <summary>
    /// OutputWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OutputWindow : Window, INotifyPropertyChanged
    {
        // 体模信息
        PhantomsCollection phantomsCollection;
        Phantom selectedPhantom;
        
        // 输出文件usercode的设置信息
        private usercodeProperties oneUsercodeProperties = new usercodeProperties();

        public usercodeProperties OneUsercodeProperties
        {
            set
            {
                oneUsercodeProperties = value;
                OnPropertyChanged(new PropertyChangedEventArgs("OneUsercodeProperties"));
            }
            get { return oneUsercodeProperties; }
        }

        // 输出文件路径设置信息
        private outputFileName oneOutputFileName = new outputFileName();

        public outputFileName OneOutputFileName
        {
            set
            {
                oneOutputFileName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("OneOutputFileName"));
            }
            get { return oneOutputFileName; }
        }

        // 输出文件runsh设置信息
        private runshParameters oneRunshParameters = new runshParameters();

        public runshParameters OneRunshParameters
        {
            set
            {
                oneRunshParameters = value;
                OnPropertyChanged(new PropertyChangedEventArgs("OneRunshParameters"));
            }
            get { return oneRunshParameters; }
        }

        public OutputWindow(PhantomsCollection phantomsCollection, Phantom selectedPhantom)
        {
            InitializeComponent();
            this.phantomsCollection = phantomsCollection;
            this.selectedPhantom = selectedPhantom;
            UserCodeTabItem.DataContext = OneUsercodeProperties;
            runshTabItem.DataContext = OneRunshParameters;
            PathStackPanel.DataContext = OneOutputFileName;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        // 以MCNP方式输出
        private void OutputMcClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string makrDirName = DateTime.Now.Year.ToString() + DateTime.Now.Month + DateTime.Now.Day + DateTime.Now.Hour +
                    DateTime.Now.Minute + DateTime.Now.Second;
                Directory.CreateDirectory(makrDirName); // 当前目录下面创建文件夹
                if (OutputWayCheckBox.IsChecked.HasValue && OutputWayCheckBox.IsChecked.Value)  // 导出所有的体模
                {
                    foreach (Phantom phantom in phantomsCollection.AllPhantoms)
                    {
                        phantom.OutPutPhantom(System.IO.Path.Combine(makrDirName, phantom.PhantomName));
                    }
                }
                else // 值导出当前选定的体模
                {
                    selectedPhantom.OutPutPhantom(System.IO.Path.Combine(makrDirName, selectedPhantom.PhantomName));
                }


                MessageBox.Show("完成，并且输出文件位于" + makrDirName);
            }
            catch (Exception exception)
            {
                MessageBox.Show("失败了");
            }
        }

        // 以Archer方式输出
        private void OutputArcherClick(object sender, RoutedEventArgs e)
        {
            try
            {
                TaskProgressBar.IsIndeterminate = true;

                string makrDirName = DateTime.Now.Year.ToString() + DateTime.Now.Month + DateTime.Now.Day + DateTime.Now.Hour +
                                     DateTime.Now.Minute + "_" + OneUsercodeProperties.ct_scanner_motion_type;
                Directory.CreateDirectory(makrDirName); // 当前目录下面创建文件夹
                // 按照不同光谱和Scanner创建文件夹
                string spectrumString = SpectrumTextBox.Text;
                string scannerString = ScannerTextBox.Text;
                string[] spectrums = spectrumString.Split(new char[2] {',', ' '},
                    StringSplitOptions.RemoveEmptyEntries);
                string[] scanners = scannerString.Split(new char[2] { ',', ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (OutputWayCheckBox.IsChecked.HasValue && OutputWayCheckBox.IsChecked.Value)  // 导出所有的体模
                {
                    foreach (string spectrum in spectrums)
                    {
                        foreach (string scanner in scanners)
                        {
                            string newFolderName = spectrum + "kVp" + scanner;
                            string newFolderPath = System.IO.Path.Combine(makrDirName, newFolderName);
                            Directory.CreateDirectory(newFolderPath); // 按照光谱扫描部件创建文件夹

                            oneRunshParameters.Spectrum = Convert.ToInt32(spectrum);
                            oneRunshParameters.Scanner = scanner;

                            foreach (Phantom phantom in phantomsCollection.AllPhantoms)
                            {
                                string thePhantomDir = System.IO.Path.Combine(newFolderPath, phantom.PhantomName);
                                Directory.CreateDirectory(thePhantomDir); // 创建体模文件夹
                                phantom.OutPutPhantomForArcher(System.IO.Path.Combine(thePhantomDir, phantom.PhantomName));
                                phantom.OutputTallyForArcher(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.TallyFilePath));
                                phantom.Outputuniverse_to_materialForArcher(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.universe_to_materialFilePath));
                                phantom.OutputrunshForArcher(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.RunshFilePath), OneUsercodeProperties, oneOutputFileName, oneRunshParameters);
                                OneUsercodeProperties.Output(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.UsercodeFilePath));

                                ProgressLabel.Content = "Finish " + phantom.PhantomName + " kVp = " + spectrum +
                                                        "scanner + " + scanner;
                                TaskProgressBar.IsIndeterminate = true;
                            }
                        }
                    }

                    ProgressLabel.Content = "Finish all!";
                    TaskProgressBar.IsIndeterminate = false;
                }
                else // 只导出当前选定的体模
                {
                    foreach (string spectrum in spectrums)
                    {
                        foreach (string scanner in scanners)
                        {
                            string newFolderName = spectrum + "kVp" + scanner;
                            string newFolderPath = System.IO.Path.Combine(makrDirName, newFolderName);
                            Directory.CreateDirectory(newFolderPath); // 按照光谱扫描部件创建文件夹

                            foreach (Phantom phantom in phantomsCollection.AllPhantoms)
                            {
                                string thePhantomDir = System.IO.Path.Combine(newFolderPath, selectedPhantom.PhantomName);
                                Directory.CreateDirectory(thePhantomDir); // 创建体模文件夹
                                selectedPhantom.OutPutPhantomForArcher(System.IO.Path.Combine(thePhantomDir, selectedPhantom.PhantomName));
                                selectedPhantom.OutputTallyForArcher(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.TallyFilePath));
                                selectedPhantom.Outputuniverse_to_materialForArcher(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.universe_to_materialFilePath));
                                selectedPhantom.OutputrunshForArcher(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.RunshFilePath), OneUsercodeProperties, oneOutputFileName, oneRunshParameters);
                                OneUsercodeProperties.Output(System.IO.Path.Combine(thePhantomDir, OneOutputFileName.UsercodeFilePath));

                                ProgressLabel.Content = "Finish " + phantom.PhantomName + " kVp = " + spectrum +
                                                        "scanner + " + scanner;
                            }
                        }
                    }

                    ProgressLabel.Content = "Finish all!";
                    TaskProgressBar.IsIndeterminate = false;
                }
                
                MessageBox.Show("完成，并且输出文件位于" + makrDirName);
            }
            catch (Exception exception)
            {
                MessageBox.Show("失败了");
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
