using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// InputReplaceOrgans.xaml 的交互逻辑
    /// </summary>
    public partial class InputReplaceOrgans : Window, INotifyPropertyChanged
    {
        public List<int> specifiedOrganList;
        public bool isFullyOperate = false;

        public event PropertyChangedEventHandler PropertyChanged;
        string organs_;
        public string organs
        {
            get
            {
                return organs_;
            }
            set
            {
                organs_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(" "));
                }
            }
        }

        public InputReplaceOrgans()
        {
            InitializeComponent();
            SetParas();
        }
        void SetParas()
        {
            organs = "";
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string organs = specifiedOrganListText.Text;
            organs = organs.Replace(',', ' ');
            organs = organs.Replace('，', ' ');
            organs = organs.Replace('\r', ' ');
            organs = organs.Replace('\n', ' ');

            string[] organsList = organs.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                List<int> specifiedOrganListTmp = new List<int>();
                foreach (var item in organsList)
                {
                    specifiedOrganListTmp.Add(Convert.ToInt32(item));
                }
                specifiedOrganList = specifiedOrganListTmp;
                isFullyOperate = true;
                Close();
            }
            catch
            {
                MessageBox.Show("输入整数数字，以空格作为分隔符");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in specifiedOrganList)
            {
                organs += (item.ToString() + " ");
            }
            specifiedOrganListText.Text = organs;
        }
    }
}
