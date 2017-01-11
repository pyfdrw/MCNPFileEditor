using MCNPFileEditor.DataClassAndControl;
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
    /// CrossSectionImageBigShow.xaml 的交互逻辑
    /// </summary>
    public partial class CrossSectionImageBigShow : Window
    {
        public CrossSectionImageBigShow()
        {
            InitializeComponent();
        }

        public CrossSectionImageBigShow(Phantom selectedPhantom, CrossSection selectedCrossSection)
        {
            InitializeComponent();

            FrontalCanvas.Height = selectedPhantom.RepeatStructureInAPhantom.DimZ;
            FrontalCanvas.Width = selectedPhantom.RepeatStructureInAPhantom.DimX;
            FrontalImage.Height = selectedPhantom.RepeatStructureInAPhantom.DimZ;
            FrontalImage.Width = selectedPhantom.RepeatStructureInAPhantom.DimX;

            Binding bindingFrontal = new Binding();
            bindingFrontal.Source = selectedCrossSection;
            bindingFrontal.Path = new PropertyPath("FrontalImg");
            BindingOperations.SetBinding(this.FrontalImage, Image.SourceProperty, bindingFrontal);

            // 设定转换矩阵
            double xScaleValue = 1;
            double yScaleValue = 1;
            double zScaleValue = 1;
            double minResolutionValue = selectedCrossSection.ResolutionX < selectedCrossSection.ResolutionY ? selectedCrossSection.ResolutionX : selectedCrossSection.ResolutionY;
            minResolutionValue = minResolutionValue < selectedCrossSection.ResolutionZ ? minResolutionValue : selectedCrossSection.ResolutionZ;

            xScaleValue = selectedCrossSection.ResolutionX / minResolutionValue;
            yScaleValue = selectedCrossSection.ResolutionY / minResolutionValue;
            zScaleValue = selectedCrossSection.ResolutionZ / minResolutionValue;

            //TransverseMatrixTransform

            //FrontalMatrixTransform
            Matrix mx2 = new Matrix(-xScaleValue, 0, 0, -zScaleValue, selectedCrossSection.FrontalWidth * xScaleValue, selectedCrossSection.FrontalHeight * zScaleValue);
            FrontalMatrixTransform.Matrix = mx2;
        }
    }
}
