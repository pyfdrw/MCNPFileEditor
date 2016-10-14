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
    /// InputJumpNum.xaml 的交互逻辑
    /// </summary>
    public partial class InputJumpNum : Window
    {
        public bool isFullOperation = false;
        public int NUM = -1;
        public InputJumpNum()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NUM = Convert.ToInt32(textBox.Text);
                isFullOperation = true;
                Close();
            }
            catch
            {
                MessageBox.Show("检查输入");
            }
        }
    }
}
