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
    /// RadiusSetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RadiusSetWindow : Window
    {
        public double RadiusSet = -1;

        public RadiusSetWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RadiusSet = Convert.ToDouble(textBlock.Text);
                if (RadiusSet < 0)
                {
                    RadiusSet = -1;
                    throw new Exception("半径不为负数");
                }
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("不合理的输入数据");
                // throw;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
