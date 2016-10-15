using MCNPFileEditor.DataClassAndControl;
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
    /// CellColorChange.xaml 的交互逻辑
    /// </summary>
    public partial class CellColorChange : Window
    {
        public Color CellColor;
        public bool isAccepted = false;

        public CellColorChange()
        {
            InitializeComponent();
        }

        public CellColorChange(Cell CellShouldBeChanged)
        {
            CellColor = new Color();
            CellColor.A = CellShouldBeChanged.CellColor.A;
            CellColor.R = CellShouldBeChanged.CellColor.R;
            CellColor.G = CellShouldBeChanged.CellColor.G;
            CellColor.B = CellShouldBeChanged.CellColor.B;
                        
            this.Content = this.CellColor;
            InitializeComponent();

            ASlider.Value = CellColor.A;
            RSlider.Value = CellColor.R;
            GSlider.Value = CellColor.G;
            BSlider.Value = CellColor.B;

            cellLabel.Background = new SolidColorBrush(CellColor);
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            isAccepted = true;
            this.Close();
        }

        private void ASlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CellColor.A = (byte)ASlider.Value;
            cellLabel.Background = new SolidColorBrush(CellColor);
        }

        private void RSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CellColor.R = (byte)RSlider.Value;
            cellLabel.Background = new SolidColorBrush(CellColor);
        }

        private void GSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CellColor.G = (byte)GSlider.Value;
            cellLabel.Background = new SolidColorBrush(CellColor);
        }

        private void BSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CellColor.B = (byte)BSlider.Value;
            cellLabel.Background = new SolidColorBrush(CellColor);
        }
    }
}
