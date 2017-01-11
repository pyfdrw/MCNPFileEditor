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
    /// InputBound.xaml 的交互逻辑
    /// </summary>
    public partial class InputBound : Window
    {
        public double UpperBoundValue = 1000;
        public double LowerBoundValue = -1;

        public bool IsOperationEffective = false;

        public InputBound()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (upperboundTextBox.Text != null && lowerboundTextBox.Text != null)
            {
                try
                {
                    UpperBoundValue = Convert.ToDouble(upperboundTextBox.Text);
                    LowerBoundValue = Convert.ToDouble(lowerboundTextBox.Text);

                    // 确认输入，并且完成输入了
                    IsOperationEffective = true;
                    this.Close();
                }
                catch (Exception)
                {
                    MessageBox.Show("请输入有效的数字");
                }
            }
        }
    }
}
