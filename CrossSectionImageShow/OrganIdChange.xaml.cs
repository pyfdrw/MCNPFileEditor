using MCNPFileEditor.DataClassAndControl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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

namespace MCNPFileEditor
{
    /// <summary>
    /// OrganIdChange.xaml 的交互逻辑
    /// </summary>
    public partial class OrganIdChange : Window
    {
        bool isRefDataImported = false;
        bool isCellInitilized = false;
        DataTable dataTable = null;
        ObservableCollection<string> openPhantomList = new ObservableCollection<string>();
        ReplaceIDRecoedHelperForAllPhantom recoedHelperForAllPhantom;
        PhantomsCollection phantomsCollectionUsed = null;

        public OrganIdChange()
        {
            InitializeComponent();
        }

        public OrganIdChange(PhantomsCollection phantomsCollection)
        {
            InitializeComponent();
            if(phantomsCollection != null)
            {
                parseJson("./CrossSectionImageShow/RefOrganName.json");
                if (isRefDataImported)
                {
                    refOrganNameGrid.ItemsSource = dataTable.DefaultView;
                }

                openPhantomList.Clear();

                // 设置下拉框的数据来源
                foreach (var item in phantomsCollection.AllPhantoms)
                {
                    openPhantomList.Add(item.PhantomName);
                }

                PhantomList.ItemsSource = openPhantomList;

                // 给每个体模添加辅助的列表
                recoedHelperForAllPhantom = new ReplaceIDRecoedHelperForAllPhantom(phantomsCollection);

                //
                phantomsCollectionUsed = phantomsCollection;
            }
        }

        // 解析Json文件
        void parseJson(string jsonPath)
        {
            // 不存在器官参考名称和ID的JSON文件
            if(!File.Exists(jsonPath))
            {
                MessageBox.Show("缺少包含器官名称和编号的JSON文件，" +
                    "将不会显示在参考窗口中显示这部分信息，" +
                    "FILE PATH WAS SET TO "
                                + jsonPath);
                return;
            }

            TextReader textReader = new StreamReader(jsonPath);
            string jsonText = textReader.ReadToEnd();

            DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(jsonText);

            dataTable = dataSet.Tables["OrganList"];

            isRefDataImported = true;
        }

        public class ReplaceIDRecoedHelperForAllPhantom : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public ObservableCollection<ReplaceIDRecoedHelper> ReplaceIDRecoedHelpers;

            public ReplaceIDRecoedHelperForAllPhantom(PhantomsCollection phantomsCollection)
            {
                if(phantomsCollection?.AllPhantoms != null)
                {
                    ReplaceIDRecoedHelpers = new ObservableCollection<ReplaceIDRecoedHelper>();
                    foreach (var item in phantomsCollection.AllPhantoms)
                    {
                        ReplaceIDRecoedHelpers.Add(new ReplaceIDRecoedHelper(item.CellsCollectionInAPhantom));
                    }
                }
            }
        }

        public class ReplaceIDRecoedHelper : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public ObservableCollection<CellHelper> CellHelpers;

            public ReplaceIDRecoedHelper(CellsCollection cellsCollection)
            {
                if(cellsCollection?.AllCells != null)
                {
                    CellHelpers = new ObservableCollection<CellHelper>();
                    foreach (var item in cellsCollection.AllCells)
                    {
                        if (item == null)
                            continue;
                        CellHelpers.Add(new CellHelper()
                        {
                            OrganName = item.OrganName,
                            CellIndex = item.CellIndex,
                            CellIndexNew = item.CellIndex
                        });
                    }

                }
            }

            // 获取输入的变化列表
            public Dictionary<int, int> GetReplaceList()
            {
                Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();
                if (null != CellHelpers)
                {
                    foreach (var item in CellHelpers)
                    {
                        if (item == null)
                            continue;
                        else
                        {
                            // 有输入的数值
                            if(item.CellIndex != item.CellIndexNew)
                            {
                                keyValuePairs.Add(item.CellIndex, item.CellIndexNew);
                            }
                        }
                    }
                }

                return keyValuePairs;
            }

            // 检查列表的正确性
            // 成功返回SUCCESS
            // 不成功返回失败的检验路径
            public static string CheckReplaceList(Dictionary<int, int> replaceList)
            {
                string checkString = "";

                List<int> checkedList = new List<int>();

                foreach (var item in replaceList)
                {
                    checkString = "";
                    checkString += (item.Key + "->");    
                    int entryID = item.Key;
                    int checkNow = entryID;
                    // checkedList.Add(entryID);   // 检测环的开始
                    while (true) // 查到没有数据或者是找到使用的ID重复时，跳出
                    {
                        checkNow = replaceList[checkNow];
                        if (!replaceList.ContainsKey(checkNow)) // 1 没有环这种情况 通过
                        {
                            checkString = "SUCCESS";
                            checkedList.Clear();
                            break;
                        }
                        else // if (replaceList.ContainsKey(checkNow))   没有到这条替换线的末尾
                        {
                            // 2 碰到了环，环位于结束于起始点 通过
                            if(checkNow == entryID)
                            {
                                checkString = "SUCCESS";
                                checkedList.Clear();
                                break;
                            }
                            // 3 碰到了环，环不位于起始点 不通过
                            else if(-1 != checkedList.FindIndex(x => x== checkNow) &&
                                checkNow != entryID)
                            {
                                checkString += (checkNow);
                                break;
                            }
                            // 其他情况则需要继续循环
                            else
                            {
                                checkString += (checkNow + "->");
                                checkedList.Add(checkNow);
                            }
                        }
                    }

                    if(checkString.Equals("SUCCESS")) // 这条线检测成功，继续进行下个循环
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                return checkString;
            }
        }

        public class CellHelper : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            // 器官名称
            string OrganName_;
            public string OrganName
            {
                set
                {
                    OrganName_ = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("OrganName"));
                    }
                }
                get
                {
                    return OrganName_;
                }
            }

            // Cell索引
            int CellIndex_;
            public int CellIndex
            {
                set
                {
                    CellIndex_ = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CellIndex_"));
                    }
                }
                get
                {
                    return CellIndex_;
                }
            }

            // Cell新的索引
            int CellIndexNew_;
            public int CellIndexNew
            {
                set
                {
                    CellIndexNew_ = value;
                    if(CellIndexNew_ != CellIndex_)
                    {
                        ShowColor.Color = Color.FromArgb(128, 255, 0, 0);
                    }
                    else
                    {
                        ShowColor.Color = Color.FromArgb(0, 0, 0, 0);
                    }
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CellIndexNew_"));
                    }
                }
                get
                {
                    return CellIndexNew_;
                }
            }

            // 指示当前条目是否已经被编辑
            SolidColorBrush ShowColor_ = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            public SolidColorBrush ShowColor
            {
                set
                {
                    ShowColor_ = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ShowColor_"));
                    }
                }
                get
                {
                    return ShowColor_;
                }
            }
        }

        int lastSelectedIndex = -10000;
        // 选定的体模发生变化，更改器官列表
        private void PhantomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 变换显示
            if(lastSelectedIndex != PhantomList.SelectedIndex && recoedHelperForAllPhantom?.ReplaceIDRecoedHelpers != null)
            {
                lastSelectedIndex = PhantomList.SelectedIndex;
                newOrganIdGrid.ItemsSource = recoedHelperForAllPhantom.ReplaceIDRecoedHelpers[lastSelectedIndex].CellHelpers;
            }
        }
        
        // 开始执行替换
        private void DoReplace_Button_Click(object sender, RoutedEventArgs e)
        {
            // 得到选定的需要更改的Phantom序列
            int selectedID = PhantomList.SelectedIndex;
            if (selectedID == -1)
                return;
            // 先生成列表 
            var dict = recoedHelperForAllPhantom.ReplaceIDRecoedHelpers[selectedID].GetReplaceList();

            // 首先检查输入的是否正确
            string checkResult = ReplaceIDRecoedHelper.CheckReplaceList(dict);
            if (checkResult.Equals("SUCCESS"))  // 成功了，执行替换
            {
                if(phantomsCollectionUsed != null)
                {
                    phantomsCollectionUsed.AllPhantoms[selectedID].ReplaceOrganIDs(dict);
                }
                MessageBox.Show("器官编号替换成功", "SUCCESS");
            }
            else // 没成功，不处理，输出提示信息
            {
                MessageBox.Show("检测替换使用的列表出错，最后一个出错的检测环为 " + checkResult);
            }
        }
    }
}
