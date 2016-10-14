using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace MCNPFileEditor.CrossSectionImageShow
{
    /// <summary>
    /// subWindowSliceInfo.xaml 的交互逻辑
    /// </summary>
    public partial class subWindowSliceInfo : Window
    {
        public DataClassAndControl.Phantom selectedPhantom;
        ObservableCollection<SingleOrganCount> organCount = new ObservableCollection<SingleOrganCount>();
        string singleSliceInfoToClipboard = "";
        string allSliceInfoToClipboard = "";

        int upperBound = 0;
        int lowerBound = 0;

        int workSurface = 0; // 0 XY 1 YZ 2 XZ, 0 default

        int sliceSelected = 0; // 输入的层数

        // 这层体素总数和密度总数
        int voxelNumSum = 0;
        double voxelDensitySum = 0;

        public subWindowSliceInfo()
        {
            InitializeComponent();
            // this.selectedPhantom = (DataClassAndControl.Phantom)this.DataContext;
        }

        public subWindowSliceInfo(DataClassAndControl.Phantom selectedPhantom)
        {
            this.selectedPhantom = selectedPhantom;
            InitializeComponent();
            refreshInfo();
            // this.selectedPhantom = (DataClassAndControl.Phantom)this.DataContext;
        }

        public void refreshInfo()
        {
            if (this.selectedPhantom == null)
            {
                MessageBox.Show("体模为空", "WRONG");
                this.Close();
            }

            for (int i = 0; i < 200; i++)
            {
                organCount.Add(new SingleOrganCount());
                organCount[i].OrganIndex = i;
                if (selectedPhantom.CellsCollectionInAPhantom.AllCells[i] != null)
                {
                    organCount[i].OrganDensity = selectedPhantom.CellsCollectionInAPhantom.AllCells[i].DensityValue;
                }
                else
                    organCount[i].OrganDensity = 0;
            }

            organCountListView.ItemsSource = organCount.Where(x => (x.OrganCount != 0));

            upperBound = selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ;
            lowerBound = selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ;
            rangeTextLabel.Content = " " + "( " + lowerBound + "~" + upperBound + " )" + "";

            singleVoxelDimLabel.Content = ("X Y Z ( " + selectedPhantom.RepeatStructureInAPhantom.ResolutionX.ToString() + " X "
                + selectedPhantom.RepeatStructureInAPhantom.ResolutionY.ToString() + " X "
                + selectedPhantom.RepeatStructureInAPhantom.ResolutionZ.ToString() + " )");
        }

        // 刷新信息
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sliceSelected = Convert.ToInt32(sliceNowText.Text);
                if (sliceSelected < lowerBound)
                {
                    sliceSelected = lowerBound;
                    sliceNowText.Text = lowerBound.ToString();
                }
                else if (sliceSelected > upperBound)
                {
                    sliceSelected = upperBound;
                    sliceNowText.Text = upperBound.ToString();
                }

                RefreshSliceOrganInfo();

                inCountLabel.Content = voxelNumSum.ToString();
                inDensitySumLabel.Content = voxelDensitySum.ToString();

                organCountListView.ItemsSource = organCount.Where(x => (x.OrganCount != 0));
            }
            catch
            {
                MessageBox.Show("请输入数字", "Wrong");
            }
        }

        // 输出到剪切板
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            allSliceInfoToClipboard = "";
            singleSliceInfoToClipboard = "";
            MessageBoxResult selectMessage = SelectOutRange();
            RefreshOutputString();
            switch (selectMessage)
            {
                case MessageBoxResult.None:
                    break;
                case MessageBoxResult.OK:
                    break;
                case MessageBoxResult.Cancel:
                    break;
                case MessageBoxResult.Yes:
                    Clipboard.SetText(allSliceInfoToClipboard);
                    MessageBox.Show("所需信息已经粘贴到粘贴板");
                    break;
                case MessageBoxResult.No:
                    Clipboard.SetText(singleSliceInfoToClipboard);
                    MessageBox.Show("所需信息已经粘贴到粘贴板");
                    break;
                default:
                    break;
            }
        }

        // 输出到文件
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OutputInfo();
        }

        // 确定是否输出所有信息或者是输出当前层的信息
        MessageBoxResult SelectOutRange()
        {
            string messageBoxText = "保存所有层的信息" + Environment.NewLine + "(Yes 保存所有层， No 只保存当前选择的层， Cancel 取消操作) ?";
            string caption = "选择";
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;

            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            return result;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            workSurface = comboBox.SelectedIndex;
            if (workSurface == 0)
            {
                upperBound = selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ;
                lowerBound = selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ;
                rangeTextLabel.Content = " " + "( " + lowerBound + "~" + upperBound + " )" + "";
            }
            else if (workSurface == 1)
            {
                upperBound = selectedPhantom.RepeatStructureInAPhantom.UpperBoundX;
                lowerBound = selectedPhantom.RepeatStructureInAPhantom.LowerBoundX;
                rangeTextLabel.Content = " " + "( " + lowerBound + "~" + upperBound + " )" + "";
            }
            else if (workSurface == 2)
            {
                upperBound = selectedPhantom.RepeatStructureInAPhantom.UpperBoundY;
                lowerBound = selectedPhantom.RepeatStructureInAPhantom.LowerBoundY;
                rangeTextLabel.Content = " " + "( " + lowerBound + "~" + upperBound + " )" + "";
            }
            else
            {
                MessageBox.Show("无法切换工作平面");
            }
        }

        private void RefreshSliceOrganInfo()
        {
            voxelNumSum = 0;
            voxelDensitySum = 0;
            if (workSurface == 0)
            {
                int i = sliceSelected - lowerBound;
                for (int j = 0; j < selectedPhantom.RepeatStructureInAPhantom.UpperBoundY - selectedPhantom.RepeatStructureInAPhantom.LowerBoundY + 1; j++)
                {
                    for (int k = 0; k < selectedPhantom.RepeatStructureInAPhantom.UpperBoundX - selectedPhantom.RepeatStructureInAPhantom.LowerBoundX + 1; k++)
                    {
                        if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k] != 150)
                        {
                            voxelNumSum++;
                            voxelDensitySum = (voxelDensitySum + selectedPhantom.CellsCollectionInAPhantom.AllCells[selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].DensityValue);
                        }
                    }
                }
            }
            else if (workSurface == 1)
            {
                int k = sliceSelected - lowerBound;
                for (int i = 0; i < selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ - selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ + 1; i++)
                {
                    for (int j = 0; j < selectedPhantom.RepeatStructureInAPhantom.UpperBoundY - selectedPhantom.RepeatStructureInAPhantom.LowerBoundY + 1; j++)
                    {
                        if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k] != 150)
                        {
                            voxelNumSum++;
                            voxelDensitySum = (voxelDensitySum + selectedPhantom.CellsCollectionInAPhantom.AllCells[selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].DensityValue);
                        }
                    }
                }
            }
            else if (workSurface == 2)
            {
                int j = sliceSelected - lowerBound;
                for (int i = 0; i < selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ - selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ + 1; i++)
                {
                    for (int k = 0; k < selectedPhantom.RepeatStructureInAPhantom.UpperBoundX - selectedPhantom.RepeatStructureInAPhantom.LowerBoundX + 1; k++)
                    {
                        if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k] != 150)
                        {
                            voxelNumSum++;
                            voxelDensitySum = (voxelDensitySum + selectedPhantom.CellsCollectionInAPhantom.AllCells[selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].DensityValue);
                        }
                    }
                }
            }
        }

        private void RefreshOutputString()
        {
            singleSliceInfoToClipboard = "";
            allSliceInfoToClipboard = "";

            singleSliceInfoToClipboard = (sliceSelected.ToString() + Environment.NewLine + voxelNumSum.ToString() + "  " + voxelDensitySum.ToString());

            if (workSurface == 0)
            {
                for (int i = 0; i < selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ - selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ + 1; i++)
                {
                    int voxelNumSumtmp = 0;
                    double voxelDensitySumtmp = 0;
                    for (int j = 0; j < selectedPhantom.RepeatStructureInAPhantom.UpperBoundY - selectedPhantom.RepeatStructureInAPhantom.LowerBoundY + 1; j++)
                    {
                        for (int k = 0; k < selectedPhantom.RepeatStructureInAPhantom.UpperBoundX - selectedPhantom.RepeatStructureInAPhantom.LowerBoundX + 1; k++)
                        {
                            if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k] != 150)
                            {
                                voxelNumSumtmp++;
                                voxelDensitySumtmp = (voxelDensitySumtmp + selectedPhantom.CellsCollectionInAPhantom.AllCells[selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].DensityValue);
                            }
                        }
                    }
                    allSliceInfoToClipboard += (i.ToString() + Environment.NewLine + voxelNumSumtmp.ToString() + "  " + voxelDensitySumtmp.ToString() + Environment.NewLine);
                }
            }
            else if (workSurface == 1)
            {
                for (int k = 0; k < selectedPhantom.RepeatStructureInAPhantom.UpperBoundX - selectedPhantom.RepeatStructureInAPhantom.LowerBoundX + 1; k++)
                {
                    int voxelNumSumtmp = 0;
                    double voxelDensitySumtmp = 0;
                    for (int i = 0; i < selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ - selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ + 1; i++)
                    {
                        for (int j = 0; j < selectedPhantom.RepeatStructureInAPhantom.UpperBoundY - selectedPhantom.RepeatStructureInAPhantom.LowerBoundY + 1; j++)
                        {
                            if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k] != 150)
                            {
                                voxelNumSumtmp++;
                                voxelDensitySumtmp = (voxelDensitySumtmp + selectedPhantom.CellsCollectionInAPhantom.AllCells[selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].DensityValue);
                            }
                        }
                    }
                    allSliceInfoToClipboard += (k.ToString() + Environment.NewLine + voxelNumSumtmp.ToString() + "  " + voxelDensitySumtmp.ToString() + Environment.NewLine);
                }
            }
            else if (workSurface == 2)
            {
                for (int j = 0; j < selectedPhantom.RepeatStructureInAPhantom.UpperBoundY - selectedPhantom.RepeatStructureInAPhantom.LowerBoundY + 1; j++)
                {
                    int voxelNumSumtmp = 0;
                    double voxelDensitySumtmp = 0;
                    for (int i = 0; i < selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ - selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ + 1; i++)
                    {
                        for (int k = 0; k < selectedPhantom.RepeatStructureInAPhantom.UpperBoundX - selectedPhantom.RepeatStructureInAPhantom.LowerBoundX + 1; k++)
                        {
                            if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k] != 150)
                            {
                                voxelNumSumtmp++;
                                voxelDensitySumtmp = (voxelDensitySumtmp + selectedPhantom.CellsCollectionInAPhantom.AllCells[selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].DensityValue);
                            }
                        }
                    }
                    allSliceInfoToClipboard += (j.ToString() + Environment.NewLine + voxelNumSumtmp.ToString() + "  " + voxelDensitySumtmp.ToString() + Environment.NewLine);
                }
            }
        }

        private void OutputInfo()
        {
            System.Windows.Forms.SaveFileDialog newSD = new System.Windows.Forms.SaveFileDialog();
            newSD.DefaultExt = ".txt";
            newSD.CheckPathExists = true;
            if (newSD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                allSliceInfoToClipboard = "";
                singleSliceInfoToClipboard = "";
                MessageBoxResult selectMessage = SelectOutRange();
                RefreshOutputString();
                switch (selectMessage)
                {
                    case MessageBoxResult.None:
                        break;
                    case MessageBoxResult.OK:
                        break;
                    case MessageBoxResult.Cancel:
                        break;
                    case MessageBoxResult.Yes:
                        // Clipboard.SetText(allSliceInfoToClipboard);
                        File.WriteAllText(newSD.FileName, allSliceInfoToClipboard);
                        MessageBox.Show("所需信息已经输出到文件");
                        break;
                    case MessageBoxResult.No:
                        // Clipboard.SetText(singleSliceInfoToClipboard);
                        File.WriteAllText(newSD.FileName, singleSliceInfoToClipboard);
                        MessageBox.Show("所需信息已经输出到文件");
                        break;
                    default:
                        break;
                }
            }
        }
    }

    class SingleOrganCount : INotifyPropertyChanged
    {
        int organIndex = 0;
        public int OrganIndex
        {
            set
            {
                organIndex = value;
                if (null != PropertyChanged)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("organIndex"));
                }
            }
            get
            {
                return organIndex;
            }
        }

        int organCount = 0;
        public int OrganCount
        {
            set
            {
                organCount = value;
                if (null != PropertyChanged)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("organCount"));
                }
            }
            get
            {
                return organCount;
            }
        }

        double organDensity = 0;
        public double OrganDensity
        {
            set
            {
                organDensity = value;
                if (null != PropertyChanged)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("organDensity"));
                }
            }
            get
            {
                return organDensity;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
