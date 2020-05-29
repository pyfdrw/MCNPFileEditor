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
    /// FlipSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FlipSelectWindow : Window
    {
        public SelectedAxis TheSelectedAxis = SelectedAxis.Cancel;
        public FlipSelectWindow()
        {
            InitializeComponent();
        }

        private void Apply_buttomClick(object sender, RoutedEventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
            {
                TheSelectedAxis = SelectedAxis.X;
            }
            else if (comboBox.SelectedIndex == 1)
            {
                TheSelectedAxis = SelectedAxis.Y;
            }
            else if (comboBox.SelectedIndex == 2)
            {
                TheSelectedAxis = SelectedAxis.Z;
            }
            else
            {
                TheSelectedAxis = SelectedAxis.Cancel;
            }
            this.Close();
        }

        public enum SelectedAxis
        {
            X,Y,Z,Cancel
        }
    }
}
