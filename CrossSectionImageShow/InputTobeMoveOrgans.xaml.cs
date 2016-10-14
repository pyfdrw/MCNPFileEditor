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
using System.Windows.Shapes;

namespace MCNPFileEditor.CrossSectionImageShow
{
    /// <summary>
    /// InputTobeMoveOrgans.xaml 的交互逻辑
    /// </summary>
    public partial class InputTobeMoveOrgans : Window
    {
        public List<int> OrgansTobeMove;
        public string organsMoveDirection = "X";
        public int organsMoceDis = 0;
        public int additionOrgan = 119; // 补充空位的器官
        public bool shouldForceReplace = true;
        public bool isFullyOperation = false;

        public InputTobeMoveOrgans()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (OrgansTobeMove == null)
            {
                MessageBox.Show("替换器官列表初始化失败");
                Close();
            }

            specifyOrgansText.Text = "";
            for (int i = 0; i < OrgansTobeMove.Count; i++)
            {
                specifyOrgansText.Text += (OrgansTobeMove[i].ToString() + " ");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (specifyDirComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Selected one direction");
                return;
            }
            try
            {
                if(specifyDirComboBox.SelectedIndex == 0)
                {
                    organsMoveDirection = "X";
                }
                else if(specifyDirComboBox.SelectedIndex == 1)
                {
                    organsMoveDirection = "Y";
                }
                else
                {
                    organsMoveDirection = "Z";
                }
                organsMoceDis = Convert.ToInt32(specifyDisTextBox.Text);
                additionOrgan = Convert.ToInt32(specifyAdditionTissueTextBox.Text);
                shouldForceReplace = shouldForceReplaceCombo.IsChecked ?? false;
                List<string> organsList = new List<string>();
                string[] list1 = specifyOrgansText.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in list1)
                {
                    organsList.AddRange(item.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }

                OrgansTobeMove.Clear();

                foreach (var item in organsList)
                {
                    OrgansTobeMove.Add(Convert.ToInt32(item));
                }

                isFullyOperation = true;
                Close();
            }
            catch
            {
                MessageBox.Show("请检查输入");
                return;
            }
        }
    }
}
