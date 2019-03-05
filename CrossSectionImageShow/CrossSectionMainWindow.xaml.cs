using MCNPFileEditor.DataClassAndControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;
using ClipperLib;

namespace MCNPFileEditor.CrossSectionImageShow
{
    /// <summary>
    /// CrossSectionMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CrossSectionMainWindow : Window
    {
        PhantomsCollection phantomsCollection;
        ObservableCollection<string> openPhantomList = new ObservableCollection<string>();
        Phantom selectedPhantom;
        CrossSection selectedCrossSection;

        // 所放位置的器官或组织信息，所需要的原始数据
        CrossectionSectionMouseLocationOrganInfo newCrossectionSectionMouseLocationOrganInfo = new CrossectionSectionMouseLocationOrganInfo();

        // 双击选定的cell信息
        Cell selectedCell;

        // 选定的勾画工具
        MaskType? selectedMaskType = null;
        MaskType? selectedMaskTypePre = null;  // 记录上一个是什么类型

        // BackgroundWorker
        BackgroundWorker behindWorker;
        MCNPFileEditor.CrossSectionImageShow.CrossSectionSelectedOrganInfoWindow newCrossSectionSelectedOrganInfoWindow;

        public CrossSectionMainWindow()
        {
            InitializeComponent();
            SetPara();
        }

        /// <summary>
        /// 设定初始参数
        /// </summary>
        void SetPara()
        {
            phantomsCollection = ((App)Application.Current).phantomsCollection;

            if (phantomsCollection != null)
            {
                foreach (var item in phantomsCollection.AllPhantoms)
                {
                    openPhantomList.Add(item.PhantomName);
                }
            }
            else
            {
                throw new MemberAccessException("体模列表为空");
            }

            PhantomCollectionComboBox.ItemsSource = openPhantomList;

            //TransverseCoordImage
            var TransCoordImg = MCNPFileEditor.Properties.Resources.TranverseCoord;
            MemoryStream TransCoordmemory = new MemoryStream();
            TransCoordImg.Save(TransCoordmemory, System.Drawing.Imaging.ImageFormat.Png);
            ImageSourceConverter TransCoordconverter = new ImageSourceConverter();
            ImageSource TransCoordsource = (ImageSource)TransCoordconverter.ConvertFrom(TransCoordmemory);
            TransverseCoordImage.Source = TransCoordsource;

            //FrontalCoordImage
            var FrontCoordImg = MCNPFileEditor.Properties.Resources.FrontalCoord;
            MemoryStream FrontCoordmemory = new MemoryStream();
            FrontCoordImg.Save(FrontCoordmemory, System.Drawing.Imaging.ImageFormat.Png);
            ImageSourceConverter FrontCoordconverter = new ImageSourceConverter();
            ImageSource FrontCoordsource = (ImageSource)FrontCoordconverter.ConvertFrom(FrontCoordmemory);
            FrontalCoordImage.Source = FrontCoordsource;

            //SagittalCoordImage
            var SagitCoordImg = MCNPFileEditor.Properties.Resources.SagittalCoord;
            MemoryStream SagitCoordmemory = new MemoryStream();
            SagitCoordImg.Save(SagitCoordmemory, System.Drawing.Imaging.ImageFormat.Png);
            ImageSourceConverter SagitCoordconverter = new ImageSourceConverter();
            ImageSource SagitCoordsource = (ImageSource)SagitCoordconverter.ConvertFrom(SagitCoordmemory);
            SagittalCoordImage.Source = SagitCoordsource;

            // 设定路有时间
            TransverseCanvas.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(TransverseImage_MouseMove), true);
            FrontalCanvas.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(FrontalImage_MouseMove), true);
            SagittalCanvas.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(SagittalImage_MouseMove), true);

            // ListView 双击事件
            // SelectedPhantomCellinfoListView.AddHandler(ListViewItem.MouseDoubleClickEvent, new MouseButtonEventHandler(SelectedPhantomCellinfoListView_MouseDoubleClick), true);
        }

        /// <summary>
        /// 选定体模的时候，更新数据，更新绑定，使用BackgroundWorker类
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhantomCollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = PhantomCollectionComboBox.SelectedIndex;
            // 选定不为空时
            if (-1 != selectedIndex)
            {
                selectedPhantom = phantomsCollection.AllPhantoms[selectedIndex];
                SelectedPhantomCellinfoListView.ItemsSource = selectedPhantom.CellsCollectionInAPhantom.AllCells.Where(x => (x != null && x.IsCellEffective == true));

                // 更新截面图像和画板的尺寸
                TransverseImage.Height = selectedPhantom.RepeatStructureInAPhantom.DimY;
                TransverseImage.Width = selectedPhantom.RepeatStructureInAPhantom.DimX;
                // TransverseSketchCanvas.Height = selectedPhantom.RepeatStructureInAPhantom.DimY;
                // TransverseSketchCanvas.Width = selectedPhantom.RepeatStructureInAPhantom.DimX;
                
                FrontalImage.Height = selectedPhantom.RepeatStructureInAPhantom.DimZ;
                FrontalImage.Width = selectedPhantom.RepeatStructureInAPhantom.DimX;
                
                SagittalImage.Height = selectedPhantom.RepeatStructureInAPhantom.DimZ;
                SagittalImage.Width = selectedPhantom.RepeatStructureInAPhantom.DimY;

                // 生成CrossSection对象与默认的截面图像
                selectedCrossSection = new CrossSection(selectedPhantom);

                // 设置截面图像和画板绑定
                Binding bindingTransverse = new Binding();
                bindingTransverse.Source = selectedCrossSection;
                bindingTransverse.Path = new PropertyPath("TransverseImg");
                BindingOperations.SetBinding(this.TransverseImage, Image.SourceProperty, bindingTransverse);

                Binding bindingFrontal = new Binding();
                bindingFrontal.Source = selectedCrossSection;
                bindingFrontal.Path = new PropertyPath("FrontalImg");
                BindingOperations.SetBinding(this.FrontalImage, Image.SourceProperty, bindingFrontal);

                Binding bindingSagittal = new Binding();
                bindingSagittal.Source = selectedCrossSection;
                bindingSagittal.Path = new PropertyPath("SagittalImg");
                BindingOperations.SetBinding(this.SagittalImage, Image.SourceProperty, bindingSagittal);

                // 设定转换矩阵
                double xScaleValue = 1;
                double yScaleValue = 1;
                double zScaleValue = 1;
                double minResolutionValue = selectedCrossSection.ResolutionX < selectedCrossSection.ResolutionY ? selectedCrossSection.ResolutionX : selectedCrossSection.ResolutionY;
                minResolutionValue = minResolutionValue < selectedCrossSection.ResolutionZ ? minResolutionValue : selectedCrossSection.ResolutionZ;

                xScaleValue = selectedCrossSection.ResolutionX / minResolutionValue;
                yScaleValue = selectedCrossSection.ResolutionY / minResolutionValue;
                zScaleValue = selectedCrossSection.ResolutionZ / minResolutionValue;

                TransverseCanvas.Height = selectedPhantom.RepeatStructureInAPhantom.DimY * yScaleValue;
                TransverseCanvas.Width = selectedPhantom.RepeatStructureInAPhantom.DimX * xScaleValue;
                FrontalCanvas.Height = selectedPhantom.RepeatStructureInAPhantom.DimZ * zScaleValue;
                FrontalCanvas.Width = selectedPhantom.RepeatStructureInAPhantom.DimX * xScaleValue;
                SagittalCanvas.Height = selectedPhantom.RepeatStructureInAPhantom.DimZ * zScaleValue;
                SagittalCanvas.Width = selectedPhantom.RepeatStructureInAPhantom.DimY * yScaleValue;
                
                //TransverseMatrixTransform
                Matrix mx1 = new Matrix(-xScaleValue, 0, 0, -yScaleValue, selectedCrossSection.TransverseWidth * xScaleValue, selectedCrossSection.TransverseHeight * yScaleValue);
                TransverseMatrixTransform.Matrix = mx1;

                //FrontalMatrixTransform
                Matrix mx2 = new Matrix(-xScaleValue, 0, 0, -zScaleValue, selectedCrossSection.FrontalWidth * xScaleValue, selectedCrossSection.FrontalHeight * zScaleValue);
                FrontalMatrixTransform.Matrix = mx2;
                //ScaleTransform frontalScaleTransform = new ScaleTransform(xScaleValue, zScaleValue);
                //FrontalTransformGroup.Children.Clear();
                // FrontalTransformGroup.Children.Add(mx2);

                //SagittalMatrixTransform
                Matrix mx3 = new Matrix(-yScaleValue, 0, 0, -zScaleValue, selectedCrossSection.SagittalWidth * yScaleValue, selectedCrossSection.SagittalHeight * zScaleValue);
                //ScaleTransform sagittalScaleTransform = new ScaleTransform(yScaleValue, zScaleValue);
                //SagittalTransformGroup.Children.Clear();
                //SagittalTransformGroup.Children.Add(sagittalScaleTransform);
                SagittalMatrixTransform.Matrix = mx3;

                // 层数显示绑定
                TransverseInfo.DataContext = selectedCrossSection;
                FrontalInfo.DataContext = selectedCrossSection;
                SagittalInfo.DataContext = selectedCrossSection;

                // 提示信息绑定
                // TransverseToolTip.DataContext = TransverseCrossectionSectionMouseLocationOrganInfo;
                // FrontalToolTip.DataContext = FrontalCrossectionSectionMouseLocationOrganInfo;
                // SagittalToolTip.DataContext = SagittalCrossectionSectionMouseLocationOrganInfo;

                // 鼠标点位置器官信息窗口显示
                // WPF 单线程亲和模型 STA 这意味着整个用户界面都为单个线程拥有
                //// if (behindWorker == null)
                //// {
                ////     behindWorker = (BackgroundWorker)this.FindResource("backgroundWorker");
                ////     behindWorker.RunWorkerAsync();
                //// }
                if (newCrossSectionSelectedOrganInfoWindow == null)
                {
                    newCrossSectionSelectedOrganInfoWindow = new CrossSectionSelectedOrganInfoWindow();
                    newCrossSectionSelectedOrganInfoWindow.Owner = this;
                    newCrossSectionSelectedOrganInfoWindow.DataContext = newCrossectionSectionMouseLocationOrganInfo;
                    newCrossSectionSelectedOrganInfoWindow.Show();
                }

                // 设置鼠标样式
                TransverseCanvas.Cursor = Cursors.Cross;
                FrontalCanvas.Cursor = Cursors.Cross;
                SagittalCanvas.Cursor = Cursors.Cross;

                // 保存未保存的靶区
                SavePreviousInfo("Transverse");
                SavePreviousInfo("Frontal");
                SavePreviousInfo("Sagittal");

                // 删除其他多余元素
                DelteAllPolygon(TransverseCanvas);
                DelteAllPolygon(FrontalCanvas);
                DelteAllPolygon(SagittalCanvas);

                // 初始化部分元素
                RestoreInitialSketchPara("Transverse");
                RestoreInitialSketchPara("Frontal");
                RestoreInitialSketchPara("Sagittal");

                // 重新计算靶区
                selectedCrossSection.RefreshSketch("Transverse");
                selectedCrossSection.RefreshSketch("Frontal");
                selectedCrossSection.RefreshSketch("Sagittal");

                // 重新靶区显示
                RefreshCanvasSketch("Transverse");
                RefreshCanvasSketch("Frontal");
                RefreshCanvasSketch("Sagittal");

                // 生成统计信息窗口
                // TEST 测试
                // MCNPFileEditor.CrossSectionImageShow.CellStatisticChart newCellStatisticChart = new CellStatisticChart();
                // newCellStatisticChart.SetInititalPara(selectedPhantom);
                // newCellStatisticChart.Show();
            }
            else
            {
                selectedCrossSection = null;
                selectedPhantom = null;
            }
        }

        private void BackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (newCrossSectionSelectedOrganInfoWindow == null)
            {
                newCrossSectionSelectedOrganInfoWindow = new CrossSectionSelectedOrganInfoWindow();
                newCrossSectionSelectedOrganInfoWindow.DataContext = newCrossectionSectionMouseLocationOrganInfo;
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {

        }

        private void TransverseCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // "Transverse"
            if (e.Delta > 0 && selectedCrossSection != null)
            {
                selectedCrossSection.TransverseNowSliceNum = (selectedCrossSection.TransverseNowSliceNum - 1) <= 1 ? 1 : (selectedCrossSection.TransverseNowSliceNum - 1);
                selectedCrossSection.RefreshImage("Transverse");

                RefreshSliceLabelNum();

                SavePreviousInfo("Transverse");

                RestoreInitialSketchPara("Transverse");
                selectedCrossSection.RefreshSketch("Transverse");
                DelteAllPolygon(TransverseCanvas);
                RefreshCanvasSketch("Transverse");
            }
            else if (e.Delta < 0 && selectedCrossSection != null)
            {
                selectedCrossSection.TransverseNowSliceNum = (selectedCrossSection.TransverseNowSliceNum + 1) >= selectedCrossSection.TransverseTotalSliceNum ? selectedCrossSection.TransverseTotalSliceNum : (selectedCrossSection.TransverseNowSliceNum + 1);
                selectedCrossSection.RefreshImage("Transverse");

                RefreshSliceLabelNum();

                SavePreviousInfo("Transverse");

                RestoreInitialSketchPara("Transverse");
                selectedCrossSection.RefreshSketch("Transverse");
                DelteAllPolygon(TransverseCanvas);
                RefreshCanvasSketch("Transverse");
            }
        }

        private void FrontalCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // "Frontal"
            if (e.Delta > 0 && selectedCrossSection != null)
            {
                selectedCrossSection.FrontalNowSliceNum = (selectedCrossSection.FrontalNowSliceNum - 1) <= 1 ? 1 : (selectedCrossSection.FrontalNowSliceNum - 1);
                selectedCrossSection.RefreshImage("Frontal");

                RefreshSliceLabelNum();

                SavePreviousInfo("Frontal");

                RestoreInitialSketchPara("Frontal");
                selectedCrossSection.RefreshSketch("Frontal");
                DelteAllPolygon(FrontalCanvas);
                RefreshCanvasSketch("Frontal");
            }
            else if (e.Delta < 0 && selectedCrossSection != null)
            {
                selectedCrossSection.FrontalNowSliceNum = (selectedCrossSection.FrontalNowSliceNum + 1) >= selectedCrossSection.FrontalTotalSliceNum ? selectedCrossSection.FrontalTotalSliceNum : (selectedCrossSection.FrontalNowSliceNum + 1);
                selectedCrossSection.RefreshImage("Frontal");

                RefreshSliceLabelNum();

                SavePreviousInfo("Frontal");

                RestoreInitialSketchPara("Frontal");
                selectedCrossSection.RefreshSketch("Frontal");
                DelteAllPolygon(FrontalCanvas);
                RefreshCanvasSketch("Frontal");
            }
        }

        private void SagittalCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // "Sagittal"
            if (e.Delta > 0 && selectedCrossSection != null)
            {
                selectedCrossSection.SagittalNowSliceNum = (selectedCrossSection.SagittalNowSliceNum - 1) <= 1 ? 1 : (selectedCrossSection.SagittalNowSliceNum - 1);
                selectedCrossSection.RefreshImage("Sagittal");

                RefreshSliceLabelNum();

                SavePreviousInfo("Sagittal");

                RestoreInitialSketchPara("Sagittal");
                selectedCrossSection.RefreshSketch("Sagittal");
                DelteAllPolygon(SagittalCanvas);
                RefreshCanvasSketch("Sagittal");
            }
            else if (e.Delta < 0 && selectedCrossSection != null)
            {
                selectedCrossSection.SagittalNowSliceNum = (selectedCrossSection.SagittalNowSliceNum + 1) >= selectedCrossSection.SagittalTotalSliceNum ? selectedCrossSection.SagittalTotalSliceNum : (selectedCrossSection.SagittalNowSliceNum + 1);
                selectedCrossSection.RefreshImage("Sagittal");

                RefreshSliceLabelNum();

                SavePreviousInfo("Sagittal");

                RestoreInitialSketchPara("Sagittal");
                selectedCrossSection.RefreshSketch("Sagittal");
                DelteAllPolygon(SagittalCanvas);
                RefreshCanvasSketch("Sagittal");
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            InputJumpNum newInputJumpNumWindow = new InputJumpNum();
            newInputJumpNumWindow.ShowDialog();

            if (newInputJumpNumWindow.isFullOperation)
            {
                if (newInputJumpNumWindow.NUM <= 0)
                {
                    newInputJumpNumWindow.NUM = 1;
                }
                else
                {
                    newInputJumpNumWindow.NUM = newInputJumpNumWindow.NUM >= selectedCrossSection.TransverseTotalSliceNum ? selectedCrossSection.TransverseTotalSliceNum : newInputJumpNumWindow.NUM;
                }

                selectedCrossSection.TransverseNowSliceNum = newInputJumpNumWindow.NUM;
                selectedCrossSection.RefreshImage("Transverse");

                RefreshSliceLabelNum();

                SavePreviousInfo("Transverse");

                RestoreInitialSketchPara("Transverse");
                selectedCrossSection.RefreshSketch("Transverse");
                DelteAllPolygon(TransverseCanvas);
                RefreshCanvasSketch("Transverse");
            }
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            InputJumpNum newInputJumpNumWindow = new InputJumpNum();
            newInputJumpNumWindow.ShowDialog();

            if (newInputJumpNumWindow.isFullOperation)
            {
                if (newInputJumpNumWindow.NUM <= 0)
                {
                    newInputJumpNumWindow.NUM = 1;
                }
                else
                {
                    newInputJumpNumWindow.NUM = newInputJumpNumWindow.NUM >= selectedCrossSection.FrontalTotalSliceNum ? selectedCrossSection.FrontalTotalSliceNum : newInputJumpNumWindow.NUM;
                }

                selectedCrossSection.FrontalNowSliceNum = newInputJumpNumWindow.NUM;
                selectedCrossSection.RefreshImage("Frontal");

                RefreshSliceLabelNum();

                SavePreviousInfo("Frontal");

                RestoreInitialSketchPara("Frontal");
                selectedCrossSection.RefreshSketch("Frontal");
                DelteAllPolygon(FrontalCanvas);
                RefreshCanvasSketch("Frontal");
            }
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            InputJumpNum newInputJumpNumWindow = new InputJumpNum();
            newInputJumpNumWindow.ShowDialog();

            if (newInputJumpNumWindow.isFullOperation)
            {
                if (newInputJumpNumWindow.NUM <= 0)
                {
                    newInputJumpNumWindow.NUM = 1;
                }
                else
                {
                    newInputJumpNumWindow.NUM = newInputJumpNumWindow.NUM >= selectedCrossSection.SagittalTotalSliceNum ? selectedCrossSection.SagittalTotalSliceNum : newInputJumpNumWindow.NUM;
                }

                selectedCrossSection.SagittalNowSliceNum = newInputJumpNumWindow.NUM;
                selectedCrossSection.RefreshImage("Sagittal");

                RefreshSliceLabelNum();

                SavePreviousInfo("Sagittal");

                RestoreInitialSketchPara("Sagittal");
                selectedCrossSection.RefreshSketch("Sagittal");
                DelteAllPolygon(SagittalCanvas);
                RefreshCanvasSketch("Sagittal");
            }
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            TransverseInfo.Visibility = Visibility.Hidden;
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            FrontalInfo.Visibility = Visibility.Hidden;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            SagittalInfo.Visibility = Visibility.Hidden;
        }

        private void RefreshSliceLabelNum()
        {
            BindingExpression TransverseLabelBindingExpression = BindingOperations.GetBindingExpression(nowSliceTransLabel, Label.ContentProperty);
            if (TransverseLabelBindingExpression != null)
            {
                TransverseLabelBindingExpression.UpdateTarget();
            }

            BindingExpression FrontalLabelBindingExpression = BindingOperations.GetBindingExpression(nowSliceFrontLabel, Label.ContentProperty);
            if (FrontalLabelBindingExpression != null)
            {
                FrontalLabelBindingExpression.UpdateTarget();
            }

            BindingExpression SagittalLabelBindingExpression = BindingOperations.GetBindingExpression(nowSliceSagittalLabel, Label.ContentProperty);
            if (SagittalLabelBindingExpression != null)
            {
                SagittalLabelBindingExpression.UpdateTarget();
            }
        }

        // sender 来源 Image或Canvas下面的子元素
        private void TransverseImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point mouseLocation = e.GetPosition(TransverseImage);
            int xIndex = (int)mouseLocation.X;
            int yIndex = (int)mouseLocation.Y;

            xIndex = xIndex >= selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(2) ? selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(2) - 1 : xIndex;
            yIndex = yIndex >= selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(1) ? selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(1) - 1 : yIndex;

            if (selectedCrossSection != null && IsMeasureToolSelected != true)
            {
                if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix != null)
                {
                    newCrossectionSectionMouseLocationOrganInfo.OrganIndex = selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[selectedCrossSection.TransverseNowSliceNum - 1, yIndex, xIndex];

                    if (selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex] != null)
                    {
                        newCrossectionSectionMouseLocationOrganInfo.MaterialIndex = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].MaterialIndex;
                        newCrossectionSectionMouseLocationOrganInfo.OrganName = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].OrganName;
                        newCrossectionSectionMouseLocationOrganInfo.MaterialDensity = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].DensityValue;
                        newCrossectionSectionMouseLocationOrganInfo.OrganColor = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].CellColor;
                        newCrossectionSectionMouseLocationOrganInfo.CrossSectionType = "Transverse";
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationX = mouseLocation.X;
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationY = mouseLocation.Y;

                        if (TransverseSketchPolygonForShow != null)
                        {
                            TransverseCanvas.Children.Remove(TransverseSketchPolygonForShow);
                        }
                        if (TransverseSketchInfoTmp != null)
                        {
                            if (selectedMaskType.HasValue)
                            {
                                switch (selectedMaskType.Value)
                                {
                                    case MaskType.PointType:
                                        break;
                                    case MaskType.RectangleType:
                                        TransverseSketchPolygonForShow.Points = new PointCollection(TransverseSketchInfoTmp.vertexColl);
                                        TransverseSketchPolygonForShow.Points.Add(new Point(TransverseSketchInfoTmp.vertexColl[0].X, yIndex));
                                        TransverseSketchPolygonForShow.Points.Add(new Point(xIndex, yIndex));
                                        TransverseSketchPolygonForShow.Points.Add(new Point(xIndex, TransverseSketchInfoTmp.vertexColl[0].Y));
                                        TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                                        TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                                        TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                                        TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                                        TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                                        break;
                                    case MaskType.PolygonType:
                                        TransverseSketchPolygonForShow.Points = new PointCollection(TransverseSketchInfoTmp.vertexColl);
                                        TransverseSketchPolygonForShow.Points.Add(new Point(xIndex, yIndex));
                                        TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                                        TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                                        TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                                        TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                                        TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                                        break;
                                    case MaskType.CircleType:
                                        // 仅在鼠标左键按下并且移动过程中并且
                                        // 已经创建初始的圆形多边形区域
                                        // 并且才进行环状靶区勾画
                                        // 不支持新画的区域与原来的区域不连通，这个是程序结构限制，不是采用开源库的原因
                                        if (e.LeftButton == MouseButtonState.Pressed)
                                        {
                                            // 新的圆形
                                            polygonForCircleToolShowTmp = new Polygon();
                                            SketchInfo sketchInfoTmp = MathFunction.CalCirclePoints(new Point(xIndex, yIndex), drawCircleRadius, selectedCell.CellIndex, 0);
                                            foreach (Point point in sketchInfoTmp.vertexColl)
                                            {
                                                polygonForCircleToolShowTmp.Points.Add(point);
                                            }
                                            List<Polygon> firstPolygon = new List<Polygon>();
                                            firstPolygon.Add(TransverseSketchPolygonForShow);
                                            List<Polygon> secondPolygon = new List<Polygon>();
                                            secondPolygon.Add(polygonForCircleToolShowTmp);
                                            List<Polygon> answerPolygon;
                                            answerPolygon = MathFunction.CalPolygonsBoolOp(firstPolygon, secondPolygon, ClipType.ctUnion,1000);
                                            if (answerPolygon.Count > 1) // 不支持新画的区域与原来的区域不连通，这个是程序结构限制，不是采用开源库的原因
                                            {
                                                polygonForCircleToolShowTmp = null;
                                                TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                                                TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                                                TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                                                TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                                                TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                                                return;
                                            }
                                            else
                                            {
                                                TransverseSketchPolygonForShow = answerPolygon[0];
                                                TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                                                TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                                                TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                                                TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                                                TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        newCrossectionSectionMouseLocationOrganInfo.MaterialIndex = 0;
                        newCrossectionSectionMouseLocationOrganInfo.OrganName = "";
                        newCrossectionSectionMouseLocationOrganInfo.CrossSectionType = "Transverse";
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationX = -1;
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationY = -1;
                    }

                    newCrossSectionSelectedOrganInfoWindow.RefreshInfo();
                }
            }
        }

        private void FrontalImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point mouseLocation = e.GetPosition(FrontalImage);
            int xIndex = (int)mouseLocation.X;
            int zIndex = (int)mouseLocation.Y;

            xIndex = xIndex >= selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(2) ? selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(2) - 1 : xIndex;
            zIndex = zIndex >= selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(0) ? selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(0) - 1 : zIndex;

            if (selectedCrossSection != null)
            {
                if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix != null)
                {
                    newCrossectionSectionMouseLocationOrganInfo.OrganIndex = selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[zIndex, selectedCrossSection.FrontalNowSliceNum - 1, xIndex];

                    if (selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex] != null)
                    {
                        newCrossectionSectionMouseLocationOrganInfo.MaterialIndex = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].MaterialIndex;
                        newCrossectionSectionMouseLocationOrganInfo.OrganName = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].OrganName;
                        newCrossectionSectionMouseLocationOrganInfo.MaterialDensity = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].DensityValue;
                        newCrossectionSectionMouseLocationOrganInfo.OrganColor = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].CellColor;
                        newCrossectionSectionMouseLocationOrganInfo.CrossSectionType = "Frontal";
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationX = mouseLocation.X;
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationY = mouseLocation.Y;
                    }
                    else
                    {
                        newCrossectionSectionMouseLocationOrganInfo.MaterialIndex = 0;
                        newCrossectionSectionMouseLocationOrganInfo.OrganName = "";
                        newCrossectionSectionMouseLocationOrganInfo.CrossSectionType = "Frontal";
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationX = -1;
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationY = -1;
                    }

                    newCrossSectionSelectedOrganInfoWindow.RefreshInfo();
                }
            }
        }

        private void SagittalImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point mouseLocation = e.GetPosition(SagittalImage);
            int yIndex = (int)mouseLocation.X;
            int zIndex = (int)mouseLocation.Y;

            zIndex = zIndex >= selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(0) ? selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(0) - 1 : zIndex;
            yIndex = yIndex >= selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(1) ? selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix.GetLength(1) - 1 : yIndex;

            if (selectedCrossSection != null)
            {
                if (selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix != null)
                {
                    newCrossectionSectionMouseLocationOrganInfo.OrganIndex = selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[zIndex, yIndex, selectedCrossSection.SagittalNowSliceNum - 1];

                    if (selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex] != null)
                    {
                        newCrossectionSectionMouseLocationOrganInfo.MaterialIndex = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].MaterialIndex;
                        newCrossectionSectionMouseLocationOrganInfo.OrganName = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].OrganName;
                        newCrossectionSectionMouseLocationOrganInfo.MaterialDensity = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].DensityValue;
                        newCrossectionSectionMouseLocationOrganInfo.OrganColor = selectedPhantom.CellsCollectionInAPhantom.AllCells[newCrossectionSectionMouseLocationOrganInfo.OrganIndex].CellColor;
                        newCrossectionSectionMouseLocationOrganInfo.CrossSectionType = "Sagittal";
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationX = mouseLocation.X;
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationY = mouseLocation.Y;
                    }
                    else
                    {
                        newCrossectionSectionMouseLocationOrganInfo.MaterialIndex = 0;
                        newCrossectionSectionMouseLocationOrganInfo.OrganName = "";
                        newCrossectionSectionMouseLocationOrganInfo.CrossSectionType = "Sagittal";
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationX = -1;
                        newCrossectionSectionMouseLocationOrganInfo.MouseLocationY = -1;
                    }

                    newCrossSectionSelectedOrganInfoWindow.RefreshInfo();
                }
            }
        }

        //private void SelectedPhantomCellinfoListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    object selectItem = SelectedPhantomCellinfoListView.SelectedItem;
        //    int selectedIndex = SelectedPhantomCellinfoListView.SelectedIndex;

        //    if (selectItem != null)
        //    {
        //        // MessageBox.Show("{0,5}, selectedIndex");
        //        // MessageBox.Show(selectItem.ToString());
        //        // MessageBox.Show(sender.ToString());
        //        selectedCell = (Cell)selectItem;
        //        SelectedOrganGroupBox.DataContext = selectedCell;

        //        SavePreviousInfo("Transverse");
        //        SavePreviousInfo("Frontal");
        //        SavePreviousInfo("Sagittal");

        //        TransverseSketchBrush = new SolidColorBrush(selectedCell.CellColor);
        //        FrontalSketchBrush = new SolidColorBrush(selectedCell.CellColor);
        //        SagittalSketchBrush = new SolidColorBrush(selectedCell.CellColor);
        //    }
        //}

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = comboBox.SelectedIndex;
            if (-1 != index && 4 != index)
            {
                selectedMaskType = (MaskType)index;
            }
            if (4 == index)
            {
                selectedMaskType = null;
            }
            if (3 == index) // 设置画靶区半径的功能
            {
                MCNPFileEditor.CrossSectionImageShow.RadiusSetWindow newRadiusSetWindow = new RadiusSetWindow();
                newRadiusSetWindow.ShowDialog();
                if (newRadiusSetWindow.RadiusSet != -1)
                {
                    drawCircleRadius = newRadiusSetWindow.RadiusSet;
                }
            }

            SavePreviousInfo("Transverse");
            SavePreviousInfo("Frontal");
            SavePreviousInfo("Sagittal");

            RestoreInitialSketchPara("Transverse");
            selectedCrossSection.RefreshSketch("Transverse");
            DelteAllPolygon(TransverseCanvas);
            RefreshCanvasSketch("Transverse");

            RestoreInitialSketchPara("Frontal");
            selectedCrossSection.RefreshSketch("Frontal");
            DelteAllPolygon(FrontalCanvas);
            RefreshCanvasSketch("Frontal");

            RestoreInitialSketchPara("Sagittal");
            selectedCrossSection.RefreshSketch("Sagittal");
            DelteAllPolygon(SagittalCanvas);
            RefreshCanvasSketch("Sagittal");

            selectedMaskTypePre = selectedMaskType;
        }

        // 判断是否为起始与终止点
        // 可能出现如下几种异常操作情况
        // 1 切换工作层数 (切换层数时，立刻保存上个层数里面中存储的边界信息)
        // 2 切换工作区域 (对每个工作区域设置单独的边界信息变量，每个工作区相互独立)
        // 3 切换靶区类型 (立刻保存上个存储的边界信息,SavePrevoiudInfo())
        // 4 切换替换的cell (立刻保存上个存储的边界信息)
        // 判断一个靶区已经勾画完成可能的情况
        // 1 切换层数，切换cell或者是靶区类型
        // 2 对于点，直接点击就是结束
        // 3 对于矩形，两个点就是结束
        // 4 对于多边形则监控键盘Enter按键事件
        // 工作模式为可空变量 selectedMaskType 控制
        // 当前选中的cell为 selectedCell
        // 对于点，边长为1的正4边形
        // 对于矩形,四个点的多边形
        // 圆， 多个点的多边形
        bool isTransverseBeginPoint = true;
        bool isTransverseEndPoint = false;
        int TransverseNowSliceNum = -1;


        bool isFrontalBeginPoint = true;
        bool isFrontalEndPoint = false;
        int FrontalNowSliceNum = -1;

        bool isSagittalBeginPoint = true;
        bool isSagittalEndPoint = false;
        int SagittalNowSliceNum = -1;

        // 临时存储每个靶区
        SketchInfo TransverseSketchInfoTmp = null;
        SketchInfo FrontalSketchInfoTmp = null;
        SketchInfo SagittalSketchInfoTmp = null;

        // 临时存储每个靶区，用于显示，不做多边形的方向调整，仅画边界无填充
        Polygon TransverseSketchPolygonForShow = null;
        Polygon FrontalSketchPolygonForShow = null;
        Polygon SagittalSketchPolygonForShow = null;

        // 画靶区的颜色，在切换cell时更新
        SolidColorBrush TransverseSketchBrush = new SolidColorBrush();
        SolidColorBrush FrontalSketchBrush = new SolidColorBrush();
        SolidColorBrush SagittalSketchBrush = new SolidColorBrush();

        // 画圆的半径
        private double drawCircleRadius = 1;
        // 画靶区Transverse
        private void TransverseCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TransverseCanvas.Focus();

            if (selectedCell == null)
            {
                return;
            }

            selectedMaskTypePre = selectedMaskType;

            Point mouseLocation = e.GetPosition(TransverseImage);
            double xIndex = mouseLocation.X;
            double yIndex = mouseLocation.Y;
            int zIndex = selectedCrossSection.TransverseNowSliceNum; // 层数
            TransverseNowSliceNum = zIndex;

            if (isTransverseBeginPoint)
            {
                if (selectedMaskType.HasValue)
                {

                    switch (selectedMaskType.Value)
                    {
                        case MaskType.PointType: // 单点直接添加即可
                            TransverseSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(xIndex - 0.5, yIndex - 0.5), selectedMaskType.Value);
                            TransverseSketchInfoTmp.vertexColl.Add(new Point(xIndex + 0.5, yIndex - 0.5));
                            TransverseSketchInfoTmp.vertexColl.Add(new Point(xIndex + 0.5, yIndex + 0.5));
                            TransverseSketchInfoTmp.vertexColl.Add(new Point(xIndex - 0.5, yIndex + 0.5));
                            selectedPhantom.SketchCollForAllTransverse[TransverseNowSliceNum].sketchInfoColl.Add(TransverseSketchInfoTmp);
                            selectedCrossSection.RefreshSketch("Transverse");

                            if (TransverseSketchInfoTmp != null)
                            {
                                TransverseSketchPolygonForShow = new Polygon();
                                TransverseSketchPolygonForShow.Points = new PointCollection(TransverseSketchInfoTmp.vertexColl);
                                TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                                TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                                TransverseSketchPolygonForShow.Fill = TransverseSketchBrush;
                                TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                                TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                                TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                            }

                            TransverseSketchInfoTmp = null;
                            TransverseSketchPolygonForShow = null;

                            break;
                        case MaskType.RectangleType: // 矩形则监视两个点
                            TransverseSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(xIndex, yIndex), selectedMaskType.Value);

                            TransverseSketchPolygonForShow = new Polygon();

                            isTransverseBeginPoint = false;
                            isTransverseEndPoint = true;
                            break;
                        case MaskType.PolygonType: // 多边形需要监视Enter事件，在另一个函数中间完成
                            TransverseSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(xIndex, yIndex), selectedMaskType.Value);

                            TransverseSketchPolygonForShow = new Polygon();

                            isTransverseBeginPoint = false;
                            break;
                        case MaskType.CircleType: // 圆，监视中心
                            // TransverseSketchInfoTmp = 
                            TransverseSketchInfoTmp = new SketchInfo(selectedCell.CellIndex)
                            {
                                maskType = MaskType.PolygonType, // 采用多边形的形式存储
                                vertexColl = new List<Point>()
                            };
                            TransverseSketchInfoTmp.vertexColl.AddRange(TransverseSketchPolygonForShow.Points);
                            if (selectedPhantom.SketchCollForAllTransverse[TransverseNowSliceNum].sketchInfoColl == null)
                            {
                                selectedPhantom.SketchCollForAllTransverse[TransverseNowSliceNum].sketchInfoColl = new List<SketchInfo>();
                            }
                            selectedPhantom.SketchCollForAllTransverse[TransverseNowSliceNum].sketchInfoColl.Add(TransverseSketchInfoTmp);
                            isTransverseBeginPoint = true;
                            isTransverseEndPoint = false;
                            selectedCrossSection.RefreshSketch("Transverse");
                            TransverseSketchInfoTmp = null;
                            TransverseSketchPolygonForShow = null;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                if (selectedMaskType.HasValue)
                {
                    switch (selectedMaskType.Value)
                    {
                        case MaskType.PointType:
                            throw new Exception("对于Point，不应出现isTransverseBeginPoint为false的情况!");
                        case MaskType.RectangleType:
                            if (isTransverseEndPoint)
                            {
                                TransverseSketchInfoTmp.vertexColl.Add(new Point(xIndex, yIndex));
                                // 矩形这样判断是正确的， 多边形不能这样判断
                                Vector3D PointBegin = new Vector3D(TransverseSketchInfoTmp.vertexColl[0].X, TransverseSketchInfoTmp.vertexColl[0].Y, 1);
                                Vector3D PointEnd = new Vector3D(TransverseSketchInfoTmp.vertexColl[1].X, TransverseSketchInfoTmp.vertexColl[1].Y, 1);
                                Vector3D PointMiddle = new Vector3D(TransverseSketchInfoTmp.vertexColl[1].X, TransverseSketchInfoTmp.vertexColl[0].Y, 1);
                                if (Vector3D.CrossProduct(PointMiddle - PointBegin, PointEnd - PointMiddle).Z > 0)
                                {
                                    TransverseSketchInfoTmp.vertexColl.Clear();
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointBegin.Y));
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointMiddle.X, PointMiddle.Y));
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointEnd.X, PointEnd.Y));
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointEnd.Y));
                                }
                                else
                                {
                                    TransverseSketchInfoTmp.vertexColl.Clear();
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointBegin.Y));
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointEnd.Y));
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointEnd.X, PointEnd.Y));
                                    TransverseSketchInfoTmp.vertexColl.Add(new Point(PointMiddle.X, PointMiddle.Y));
                                }

                                selectedPhantom.SketchCollForAllTransverse[TransverseNowSliceNum].sketchInfoColl.Add(TransverseSketchInfoTmp);

                                if (TransverseSketchPolygonForShow != null)
                                {
                                    TransverseCanvas.Children.Remove(TransverseSketchPolygonForShow);
                                }
                                if (TransverseSketchInfoTmp != null)
                                {
                                    TransverseSketchPolygonForShow.Points = new PointCollection(TransverseSketchInfoTmp.vertexColl);
                                    TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                                    TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                                    TransverseSketchPolygonForShow.Fill = TransverseSketchBrush;
                                    TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                                    TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                                    TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                                }

                                TransverseSketchInfoTmp = null;
                                TransverseSketchPolygonForShow = null;
                                isTransverseBeginPoint = true;
                                isTransverseEndPoint = false;
                                selectedCrossSection.RefreshSketch("Transverse");
                            }
                            else
                            {
                                throw new Exception("对于RectangleType，不应出现isTransverseBeginPoint与isTransverseEndPoint均为false的情况!");
                            }
                            break;
                        case MaskType.PolygonType:
                            TransverseSketchInfoTmp.vertexColl.Add(new Point(xIndex, yIndex));
                            LineSegment newLine = new LineSegment(new Point(xIndex, yIndex), true);

                            break;
                        case MaskType.CircleType:
                            if (isTransverseEndPoint)
                            {
                                isTransverseBeginPoint = true;
                                isTransverseEndPoint = false;
                                selectedCrossSection.RefreshSketch("Transverse");
                            }
                            else
                            {
                                throw new Exception("对于CircleType，不应出现isTransverseBeginPoint与isTransverseEndPoint均为false的情况!");
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private Polygon polygonForCircleToolShowTmp = null;
        private void TransverseCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TransverseCanvas.Focus();

            if (selectedCell == null)
            {
                return;
            }

            selectedMaskTypePre = selectedMaskType;

            Point mouseLocation = e.GetPosition(TransverseImage);
            double xIndex = mouseLocation.X;
            double yIndex = mouseLocation.Y;
            int zIndex = selectedCrossSection.TransverseNowSliceNum; // 层数
            TransverseNowSliceNum = zIndex;

            if (isTransverseBeginPoint)
            {
                if (selectedMaskType.HasValue)
                {

                    switch (selectedMaskType.Value)
                    {
                        case MaskType.CircleType:  // 仅圆形这样处理第一步先画下一个圆多边形
                            if (TransverseSketchInfoTmp != null)
                            {
                                throw new Exception("使用圆形工具勾画时出现临时靶区未能清理的情况");
                            }
                            // 画下的第一圆，临时存储下来
                            TransverseSketchInfoTmp = MathFunction.CalCirclePoints(new Point(xIndex, yIndex), drawCircleRadius,
                                selectedCell.CellIndex, 0);
                            // 在Canvas上显示
                            TransverseSketchPolygonForShow = new Polygon();
                            foreach (Point point in TransverseSketchInfoTmp.vertexColl)
                            {
                                TransverseSketchPolygonForShow.Points.Add(point);
                            }
                            TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                            TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                            TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                            TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                            TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // 监控键盘按键动作
        // 逆时针方向存储多边形
        private void TransverseCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (selectedCell == null)
            {
                return;
            }

            selectedMaskTypePre = selectedMaskType;

            // 完成多边形的勾画操作
            if (e.Key == Key.Enter && TransverseSketchInfoTmp != null)
            {
                TransverseCanvas.Focus();

                // int zIndex = selectedCrossSection.TransverseNowSliceNum; // 层数
                if (TransverseSketchInfoTmp != null && TransverseSketchInfoTmp.vertexColl.Count >= 3)
                {
                    // 多边形方向判断，参考
                    // http://wenku.baidu.com/link?url=0i75y3UHv-Ol5beS7oXwoDgSpjmG890b8rNNmIhj-6Fnprduu3epIH2ulGj7QanjcaKHmvN9odesUsWSjmMa2eG-3m3h0TQ0cHjBsn1TsOW
                    List<ForVectorIndexCompare> compareList = new List<ForVectorIndexCompare>();

                    Vector3D vector0 = new Vector3D(100000, 0, 1); int index0 = 0;
                    Vector3D vector1 = new Vector3D(-100000, 0, 1); int index1 = 0;
                    Vector3D vector2 = new Vector3D(0, 100000, 1); int index2 = 0;
                    Vector3D vector3 = new Vector3D(0, -100000, 1); int index3 = 0;
                    int indextmp = 0;

                    foreach (var item in TransverseSketchInfoTmp.vertexColl)
                    {
                        if (vector0.X >= item.X)
                        {
                            index0 = indextmp;
                            vector0.X = item.X; vector0.Y = item.Y;
                        }
                        if (vector1.X <= item.X)
                        {
                            index1 = indextmp;
                            vector1.X = item.X; vector1.Y = item.Y;
                        }
                        if (vector2.Y >= item.Y)
                        {
                            index2 = indextmp;
                            vector2.X = item.X; vector2.Y = item.Y;
                        }
                        if (vector3.Y <= item.Y)
                        {
                            index3 = indextmp;
                            vector3.X = item.X; vector3.Y = item.Y;
                        }
                        indextmp++;
                    }

                    bool isRepeated = false;
                    compareList.Add(new ForVectorIndexCompare() { index = index0, vector = vector0 });
                    if (index0 != index1 && (Math.Abs(vector0.X - vector1.X) > 1e-6 || Math.Abs(vector0.Y - vector1.Y) > 1e-6))
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index1, vector = vector1 });
                    }
                    isRepeated = false;
                    foreach (var item in compareList)
                    {
                        if (item.index == index2 || (Math.Abs(item.vector.X - vector2.X) < 1e-6 && Math.Abs(item.vector.Y - vector2.Y) < 1e-6))
                        {
                            isRepeated = true;
                        }
                    }
                    if (!isRepeated)
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index2, vector = vector2 });
                    }
                    isRepeated = false;
                    foreach (var item in compareList)
                    {
                        if (item.index == index3 || (Math.Abs(item.vector.X - vector3.X) < 1e-6 && Math.Abs(item.vector.Y - vector3.Y) < 1e-6))
                        {
                            isRepeated = true;
                        }
                    }
                    if (!isRepeated)
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index3, vector = vector3 });
                    }

                    // 从小到大排列
                    compareList.Sort();

                    if (compareList.Count <= 1 || compareList.Count > 4)
                    {
                        // throw new Exception("判断多边形正反时不应出现最值点<=1与>4这种情况");
                    }
                    else if (compareList.Count == 2)
                    {
                        indextmp = compareList[0].index;
                        if (indextmp == 0)
                        {
                            indextmp = 1;
                        }

                        if (Vector3D.CrossProduct(new Vector3D()
                        {
                            X = TransverseSketchInfoTmp.vertexColl[indextmp].X - TransverseSketchInfoTmp.vertexColl[indextmp - 1].X,
                            Y = TransverseSketchInfoTmp.vertexColl[indextmp].Y - TransverseSketchInfoTmp.vertexColl[indextmp - 1].Y,
                            Z = 1,
                        },
                            new Vector3D()
                            {
                                X = TransverseSketchInfoTmp.vertexColl[indextmp + 1].X - TransverseSketchInfoTmp.vertexColl[indextmp].X,
                                Y = TransverseSketchInfoTmp.vertexColl[indextmp + 1].Y - TransverseSketchInfoTmp.vertexColl[indextmp].Y,
                                Z = 1,
                            }).Z < 0)
                        {
                            TransverseSketchInfoTmp.vertexColl.Reverse();
                        }
                    }
                    else
                    {
                        if (Vector3D.CrossProduct(new Vector3D()
                        {
                            X = compareList[1].vector.X - compareList[0].vector.X,
                            Y = compareList[1].vector.Y - compareList[0].vector.Y,
                            Z = 1,
                        },
                            new Vector3D()
                            {
                                X = compareList[2].vector.X - compareList[1].vector.X,
                                Y = compareList[2].vector.Y - compareList[1].vector.Y,
                                Z = 1,
                            }).Z < 0)
                        {
                            TransverseSketchInfoTmp.vertexColl.Reverse();
                        }
                    }

                    if (TransverseSketchPolygonForShow != null)
                    {
                        TransverseCanvas.Children.Remove(TransverseSketchPolygonForShow);
                    }
                    if (TransverseSketchInfoTmp != null)
                    {
                        TransverseSketchPolygonForShow.Points = new PointCollection(TransverseSketchInfoTmp.vertexColl);
                        TransverseSketchPolygonForShow.RenderTransform = TransverseImage.RenderTransform;
                        TransverseSketchPolygonForShow.Stroke = TransverseSketchBrush;
                        TransverseSketchPolygonForShow.Fill = TransverseSketchBrush;
                        TransverseSketchPolygonForShow.StrokeThickness = 0.1;
                        TransverseSketchPolygonForShow.StrokeLineJoin = PenLineJoin.Bevel;
                        TransverseCanvas.Children.Add(TransverseSketchPolygonForShow);
                    }

                    selectedPhantom.SketchCollForAllTransverse[TransverseNowSliceNum].sketchInfoColl.Add(TransverseSketchInfoTmp);
                }
                else if (TransverseSketchInfoTmp.vertexColl.Count <= 2)
                {
                    TransverseCanvas.Children.Remove(TransverseSketchPolygonForShow);

                    MessageBox.Show("至少需要三个顶点才能围成一个有效的多边形!");
                }

                TransverseSketchInfoTmp = null;
                isTransverseBeginPoint = true;
                isTransverseEndPoint = false;
                TransverseSketchPolygonForShow = null;

                selectedCrossSection.RefreshSketch("Transverse");

            }
        }

        // 画靶区Frontal
        private void FrontalCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrontalCanvas.Focus();

            if (selectedCell == null)
            {
                return;
            }

            selectedMaskTypePre = selectedMaskType;

            Point mouseLocation = e.GetPosition(FrontalImage);
            double xIndex = mouseLocation.X;
            int yIndex = selectedCrossSection.FrontalNowSliceNum; // 层数
            double zIndex = mouseLocation.Y;
            FrontalNowSliceNum = yIndex;

            if (isFrontalBeginPoint)
            {
                if (selectedMaskType.HasValue)
                {
                    switch (selectedMaskType.Value)
                    {
                        case MaskType.PointType: // 单点直接添加即可
                            FrontalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(xIndex - 0.5, zIndex - 0.5), selectedMaskType.Value);
                            FrontalSketchInfoTmp.vertexColl.Add(new Point(xIndex + 0.5, zIndex - 0.5));
                            FrontalSketchInfoTmp.vertexColl.Add(new Point(xIndex + 0.5, zIndex + 0.5));
                            FrontalSketchInfoTmp.vertexColl.Add(new Point(xIndex - 0.5, zIndex + 0.5));
                            selectedPhantom.SketchCollForAllFrontal[FrontalNowSliceNum].sketchInfoColl.Add(FrontalSketchInfoTmp);
                            FrontalSketchInfoTmp = null;
                            selectedCrossSection.RefreshSketch("Frontal");
                            break;
                        case MaskType.RectangleType: // 矩形则监视两个点
                            FrontalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(xIndex, zIndex), selectedMaskType.Value);
                            isFrontalBeginPoint = false;
                            isFrontalEndPoint = true;
                            break;
                        case MaskType.PolygonType: // 多边形需要监视Enter事件，在另一个函数中间完成
                            FrontalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(xIndex, zIndex), selectedMaskType.Value);
                            isFrontalBeginPoint = false;
                            break;
                        case MaskType.CircleType: // 圆，监视中心和任意圆上的点即可
                            FrontalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(xIndex, zIndex), selectedMaskType.Value);
                            isFrontalBeginPoint = false;
                            isFrontalEndPoint = true;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                if (selectedMaskType.HasValue)
                {
                    switch (selectedMaskType.Value)
                    {
                        case MaskType.PointType:
                        //throw new Exception("对于Point，不应出现isFrontalBeginPoint为false的情况!");
                        case MaskType.RectangleType:
                            if (isFrontalEndPoint)
                            {
                                FrontalSketchInfoTmp.vertexColl.Add(new Point(xIndex, zIndex));
                                // 矩形这样判断是正确的， 多边形不能这样判断
                                Vector3D PointBegin = new Vector3D(FrontalSketchInfoTmp.vertexColl[0].X, FrontalSketchInfoTmp.vertexColl[0].Y, 1);
                                Vector3D PointEnd = new Vector3D(FrontalSketchInfoTmp.vertexColl[1].X, FrontalSketchInfoTmp.vertexColl[1].Y, 1);
                                Vector3D PointMiddle = new Vector3D(FrontalSketchInfoTmp.vertexColl[1].X, FrontalSketchInfoTmp.vertexColl[0].Y, 1);
                                if (Vector3D.CrossProduct(PointMiddle - PointBegin, PointEnd - PointMiddle).Z > 0)
                                {
                                    FrontalSketchInfoTmp.vertexColl.Clear();
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointBegin.Y));
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointMiddle.X, PointMiddle.Y));
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointEnd.X, PointEnd.Y));
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointEnd.Y));
                                }
                                else
                                {
                                    FrontalSketchInfoTmp.vertexColl.Clear();
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointBegin.Y));
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointEnd.Y));
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointEnd.X, PointEnd.Y));
                                    FrontalSketchInfoTmp.vertexColl.Add(new Point(PointMiddle.X, PointMiddle.Y));
                                }
                                selectedPhantom.SketchCollForAllFrontal[FrontalNowSliceNum].sketchInfoColl.Add(FrontalSketchInfoTmp);
                                FrontalSketchInfoTmp = null;
                                isFrontalBeginPoint = true;
                                isFrontalEndPoint = false;
                                selectedCrossSection.RefreshSketch("Frontal");
                            }
                            else
                            {
                                throw new Exception("对于RectangleType，不应出现isFrontalBeginPoint与isFrontalEndPoint均为false的情况!");
                            }
                            break;
                        case MaskType.PolygonType:
                            FrontalSketchInfoTmp.vertexColl.Add(new Point(xIndex, zIndex));
                            break;
                        case MaskType.CircleType:
                            if (isFrontalEndPoint)
                            {
                                FrontalSketchInfoTmp.vertexColl.Add(new Point(xIndex, zIndex));
                                selectedPhantom.SketchCollForAllFrontal[FrontalNowSliceNum].sketchInfoColl.Add(FrontalSketchInfoTmp);
                                FrontalSketchInfoTmp = null;
                                isFrontalBeginPoint = true;
                                isFrontalEndPoint = false;
                                selectedCrossSection.RefreshSketch("Frontal");
                            }
                            else
                            {
                                throw new Exception("对于CircleType，不应出现isFrontalBeginPoint与isFrontalEndPoint均为false的情况!");
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // 监控键盘按键动作
        private void FrontalCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (selectedCell == null)
            {
                return;
            }

            selectedMaskTypePre = selectedMaskType;

            // 完成多边形的勾画操作
            if (e.Key == Key.Enter && FrontalSketchInfoTmp != null)
            {
                FrontalCanvas.Focus();

                // int zIndex = selectedCrossSection.FrontalNowSliceNum; // 层数
                if (FrontalSketchInfoTmp != null && FrontalSketchInfoTmp.vertexColl.Count >= 3)
                {
                    // 多边形方向判断，参考
                    // http://wenku.baidu.com/link?url=0i75y3UHv-Ol5beS7oXwoDgSpjmG890b8rNNmIhj-6Fnprduu3epIH2ulGj7QanjcaKHmvN9odesUsWSjmMa2eG-3m3h0TQ0cHjBsn1TsOW
                    List<ForVectorIndexCompare> compareList = new List<ForVectorIndexCompare>();

                    Vector3D vector0 = new Vector3D(100000, 0, 1); int index0 = 0;
                    Vector3D vector1 = new Vector3D(-100000, 0, 1); int index1 = 0;
                    Vector3D vector2 = new Vector3D(0, 100000, 1); int index2 = 0;
                    Vector3D vector3 = new Vector3D(0, -100000, 1); int index3 = 0;
                    int indextmp = 0;

                    foreach (var item in FrontalSketchInfoTmp.vertexColl)
                    {
                        if (vector0.X >= item.X)
                        {
                            index0 = indextmp;
                            vector0.X = item.X; vector0.Y = item.Y;
                        }
                        if (vector1.X <= item.X)
                        {
                            index1 = indextmp;
                            vector1.X = item.X; vector1.Y = item.Y;
                        }
                        if (vector2.Y >= item.Y)
                        {
                            index2 = indextmp;
                            vector2.X = item.X; vector2.Y = item.Y;
                        }
                        if (vector3.Y <= item.Y)
                        {
                            index3 = indextmp;
                            vector3.X = item.X; vector3.Y = item.Y;
                        }
                        indextmp++;
                    }

                    bool isRepeated = false;
                    compareList.Add(new ForVectorIndexCompare() { index = index0, vector = vector0 });
                    if (index0 != index1 && (Math.Abs(vector0.X - vector1.X) > 1e-6 || Math.Abs(vector0.Y - vector1.Y) > 1e-6))
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index1, vector = vector1 });
                    }
                    isRepeated = false;
                    foreach (var item in compareList)
                    {
                        if (item.index == index2 || (Math.Abs(item.vector.X - vector2.X) < 1e-6 && Math.Abs(item.vector.Y - vector2.Y) < 1e-6))
                        {
                            isRepeated = true;
                        }
                    }
                    if (!isRepeated)
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index2, vector = vector2 });
                    }
                    isRepeated = false;
                    foreach (var item in compareList)
                    {
                        if (item.index == index3 || (Math.Abs(item.vector.X - vector3.X) < 1e-6 && Math.Abs(item.vector.Y - vector3.Y) < 1e-6))
                        {
                            isRepeated = true;
                        }
                    }
                    if (!isRepeated)
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index3, vector = vector3 });
                    }

                    // 从小到大排列
                    compareList.Sort();

                    if (compareList.Count <= 1 || compareList.Count > 4)
                    {
                        // throw new Exception("判断多边形正反时不应出现最值点<=1与>4这种情况");
                    }
                    else if (compareList.Count == 2)
                    {
                        indextmp = compareList[0].index;
                        if (indextmp == 0)
                        {
                            indextmp = 1;
                        }

                        if (Vector3D.CrossProduct(new Vector3D()
                        {
                            X = FrontalSketchInfoTmp.vertexColl[indextmp].X - FrontalSketchInfoTmp.vertexColl[indextmp - 1].X,
                            Y = FrontalSketchInfoTmp.vertexColl[indextmp].Y - FrontalSketchInfoTmp.vertexColl[indextmp - 1].Y,
                            Z = 1,
                        },
                            new Vector3D()
                            {
                                X = FrontalSketchInfoTmp.vertexColl[indextmp + 1].X - FrontalSketchInfoTmp.vertexColl[indextmp].X,
                                Y = FrontalSketchInfoTmp.vertexColl[indextmp + 1].Y - FrontalSketchInfoTmp.vertexColl[indextmp].Y,
                                Z = 1,
                            }).Z < 0)
                        {
                            FrontalSketchInfoTmp.vertexColl.Reverse();
                        }
                    }
                    else
                    {
                        if (Vector3D.CrossProduct(new Vector3D()
                        {
                            X = compareList[1].vector.X - compareList[0].vector.X,
                            Y = compareList[1].vector.Y - compareList[0].vector.Y,
                            Z = 1,
                        },
                            new Vector3D()
                            {
                                X = compareList[2].vector.X - compareList[1].vector.X,
                                Y = compareList[2].vector.Y - compareList[1].vector.Y,
                                Z = 1,
                            }).Z < 0)
                        {
                            FrontalSketchInfoTmp.vertexColl.Reverse();
                        }
                    }

                    selectedPhantom.SketchCollForAllFrontal[FrontalNowSliceNum].sketchInfoColl.Add(FrontalSketchInfoTmp);
                }
                else if (FrontalSketchInfoTmp.vertexColl.Count <= 2)
                {
                    MessageBox.Show("至少需要三个顶点才能围城一个有效的多边形!");
                }

                FrontalSketchInfoTmp = null;
                isFrontalBeginPoint = true;
                isFrontalEndPoint = false;

                selectedCrossSection.RefreshSketch("Frontal");
            }
        }

        // 画靶区Sagittal
        private void SagittalCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedCell == null)
            {
                return;
            }

            selectedMaskTypePre = selectedMaskType;

            SagittalCanvas.Focus();

            Point mouseLocation = e.GetPosition(SagittalImage);
            int xIndex = selectedCrossSection.SagittalNowSliceNum; // 层数
            double yIndex = mouseLocation.X;
            double zIndex = mouseLocation.Y;
            SagittalNowSliceNum = xIndex;

            if (isSagittalBeginPoint)
            {
                if (selectedMaskType.HasValue)
                {
                    switch (selectedMaskType.Value)
                    {
                        case MaskType.PointType: // 单点直接添加即可
                            SagittalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(yIndex - 0.5, zIndex - 0.5), selectedMaskType.Value);
                            SagittalSketchInfoTmp.vertexColl.Add(new Point(yIndex + 0.5, zIndex - 0.5));
                            SagittalSketchInfoTmp.vertexColl.Add(new Point(yIndex + 0.5, zIndex + 0.5));
                            SagittalSketchInfoTmp.vertexColl.Add(new Point(yIndex - 0.5, zIndex + 0.5));
                            selectedPhantom.SketchCollForAllSagittal[SagittalNowSliceNum].sketchInfoColl.Add(SagittalSketchInfoTmp);
                            SagittalSketchInfoTmp = null;
                            selectedCrossSection.RefreshSketch("Sagittal");
                            break;
                        case MaskType.RectangleType: // 矩形则监视两个点
                            SagittalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(yIndex, zIndex), selectedMaskType.Value);
                            isSagittalBeginPoint = false;
                            isSagittalEndPoint = true;
                            break;
                        case MaskType.PolygonType: // 多边形需要监视Enter事件，在另一个函数中间完成
                            SagittalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(yIndex, zIndex), selectedMaskType.Value);
                            isSagittalBeginPoint = false;
                            break;
                        case MaskType.CircleType: // 圆，监视中心和任意圆上的点即可
                            SagittalSketchInfoTmp = new SketchInfo(selectedCell.CellIndex, new Point(yIndex, zIndex), selectedMaskType.Value);
                            isSagittalBeginPoint = false;
                            isSagittalEndPoint = true;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                if (selectedMaskType.HasValue)
                {
                    switch (selectedMaskType.Value)
                    {
                        case MaskType.PointType:
                            throw new Exception("对于Point，不应出现isSagittalBeginPoint为false的情况!");
                        case MaskType.RectangleType:
                            if (isSagittalEndPoint)
                            {
                                SagittalSketchInfoTmp.vertexColl.Add(new Point(yIndex, zIndex));
                                // 矩形这样判断是正确的， 多边形不能这样判断
                                Vector3D PointBegin = new Vector3D(SagittalSketchInfoTmp.vertexColl[0].X, SagittalSketchInfoTmp.vertexColl[0].Y, 1);
                                Vector3D PointEnd = new Vector3D(SagittalSketchInfoTmp.vertexColl[1].X, SagittalSketchInfoTmp.vertexColl[1].Y, 1);
                                Vector3D PointMiddle = new Vector3D(SagittalSketchInfoTmp.vertexColl[1].X, SagittalSketchInfoTmp.vertexColl[0].Y, 1);
                                if (Vector3D.CrossProduct(PointMiddle - PointBegin, PointEnd - PointMiddle).Z > 0)
                                {
                                    SagittalSketchInfoTmp.vertexColl.Clear();
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointBegin.Y));
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointMiddle.X, PointMiddle.Y));
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointEnd.X, PointEnd.Y));
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointEnd.Y));
                                }
                                else
                                {
                                    SagittalSketchInfoTmp.vertexColl.Clear();
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointBegin.Y));
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointBegin.X, PointEnd.Y));
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointEnd.X, PointEnd.Y));
                                    SagittalSketchInfoTmp.vertexColl.Add(new Point(PointMiddle.X, PointMiddle.Y));
                                }
                                selectedPhantom.SketchCollForAllSagittal[SagittalNowSliceNum].sketchInfoColl.Add(SagittalSketchInfoTmp);
                                SagittalSketchInfoTmp = null;
                                isSagittalBeginPoint = true;
                                isSagittalEndPoint = false;
                                selectedCrossSection.RefreshSketch("Sagittal");
                            }
                            else
                            {
                                throw new Exception("对于RectangleType，不应出现isSagittalBeginPoint与isSagittalEndPoint均为false的情况!");
                            }
                            break;
                        case MaskType.PolygonType:
                            SagittalSketchInfoTmp.vertexColl.Add(new Point(yIndex, zIndex));
                            break;
                        case MaskType.CircleType:
                            if (isSagittalEndPoint)
                            {
                                SagittalSketchInfoTmp.vertexColl.Add(new Point(yIndex, zIndex));
                                selectedPhantom.SketchCollForAllSagittal[SagittalNowSliceNum].sketchInfoColl.Add(SagittalSketchInfoTmp);
                                SagittalSketchInfoTmp = null;
                                isSagittalBeginPoint = true;
                                isSagittalEndPoint = false;
                                selectedCrossSection.RefreshSketch("Sagittal");
                            }
                            else
                            {
                                throw new Exception("对于CircleType，不应出现isSagittalBeginPoint与isSagittalEndPoint均为false的情况!");
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // 监控键盘按键动作
        private void SagittalCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (selectedCell == null)
            {
                return;
            }

            selectedMaskTypePre = selectedMaskType;

            // 完成多边形的勾画操作
            if (e.Key == Key.Enter && SagittalSketchInfoTmp != null)
            {
                SagittalCanvas.Focus();

                // int zIndex = selectedCrossSection.SagittalNowSliceNum; // 层数
                if (SagittalSketchInfoTmp != null && SagittalSketchInfoTmp.vertexColl.Count >= 3)
                {
                    // 多边形方向判断，参考
                    // http://wenku.baidu.com/link?url=0i75y3UHv-Ol5beS7oXwoDgSpjmG890b8rNNmIhj-6Fnprduu3epIH2ulGj7QanjcaKHmvN9odesUsWSjmMa2eG-3m3h0TQ0cHjBsn1TsOW
                    List<ForVectorIndexCompare> compareList = new List<ForVectorIndexCompare>();

                    Vector3D vector0 = new Vector3D(100000, 0, 1); int index0 = 0;
                    Vector3D vector1 = new Vector3D(-100000, 0, 1); int index1 = 0;
                    Vector3D vector2 = new Vector3D(0, 100000, 1); int index2 = 0;
                    Vector3D vector3 = new Vector3D(0, -100000, 1); int index3 = 0;
                    int indextmp = 0;

                    foreach (var item in SagittalSketchInfoTmp.vertexColl)
                    {
                        if (vector0.X >= item.X)
                        {
                            index0 = indextmp;
                            vector0.X = item.X; vector0.Y = item.Y;
                        }
                        if (vector1.X <= item.X)
                        {
                            index1 = indextmp;
                            vector1.X = item.X; vector1.Y = item.Y;
                        }
                        if (vector2.Y >= item.Y)
                        {
                            index2 = indextmp;
                            vector2.X = item.X; vector2.Y = item.Y;
                        }
                        if (vector3.Y <= item.Y)
                        {
                            index3 = indextmp;
                            vector3.X = item.X; vector3.Y = item.Y;
                        }
                        indextmp++;
                    }

                    bool isRepeated = false;
                    compareList.Add(new ForVectorIndexCompare() { index = index0, vector = vector0 });
                    if (index0 != index1 && (Math.Abs(vector0.X - vector1.X) > 1e-6 || Math.Abs(vector0.Y - vector1.Y) > 1e-6))
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index1, vector = vector1 });
                    }
                    isRepeated = false;
                    foreach (var item in compareList)
                    {
                        if (item.index == index2 || (Math.Abs(item.vector.X - vector2.X) < 1e-6 && Math.Abs(item.vector.Y - vector2.Y) < 1e-6))
                        {
                            isRepeated = true;
                        }
                    }
                    if (!isRepeated)
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index2, vector = vector2 });
                    }
                    isRepeated = false;
                    foreach (var item in compareList)
                    {
                        if (item.index == index3 || (Math.Abs(item.vector.X - vector3.X) < 1e-6 && Math.Abs(item.vector.Y - vector3.Y) < 1e-6))
                        {
                            isRepeated = true;
                        }
                    }
                    if (!isRepeated)
                    {
                        compareList.Add(new ForVectorIndexCompare() { index = index3, vector = vector3 });
                    }

                    // 从小到大排列
                    compareList.Sort();

                    if (compareList.Count <= 1 || compareList.Count > 4)
                    {
                        // throw new Exception("判断多边形正反时不应出现最值点<=1与>4这种情况");
                    }
                    else if (compareList.Count == 2)
                    {
                        indextmp = compareList[0].index;
                        if (indextmp == 0)
                        {
                            indextmp = 1;
                        }

                        if (Vector3D.CrossProduct(new Vector3D()
                        {
                            X = SagittalSketchInfoTmp.vertexColl[indextmp].X - SagittalSketchInfoTmp.vertexColl[indextmp - 1].X,
                            Y = SagittalSketchInfoTmp.vertexColl[indextmp].Y - SagittalSketchInfoTmp.vertexColl[indextmp - 1].Y,
                            Z = 1,
                        },
                            new Vector3D()
                            {
                                X = SagittalSketchInfoTmp.vertexColl[indextmp + 1].X - SagittalSketchInfoTmp.vertexColl[indextmp].X,
                                Y = SagittalSketchInfoTmp.vertexColl[indextmp + 1].Y - SagittalSketchInfoTmp.vertexColl[indextmp].Y,
                                Z = 1,
                            }).Z < 0)
                        {
                            SagittalSketchInfoTmp.vertexColl.Reverse();
                        }
                    }
                    else
                    {
                        if (Vector3D.CrossProduct(new Vector3D()
                        {
                            X = compareList[1].vector.X - compareList[0].vector.X,
                            Y = compareList[1].vector.Y - compareList[0].vector.Y,
                            Z = 1,
                        },
                            new Vector3D()
                            {
                                X = compareList[2].vector.X - compareList[1].vector.X,
                                Y = compareList[2].vector.Y - compareList[1].vector.Y,
                                Z = 1,
                            }).Z < 0)
                        {
                            SagittalSketchInfoTmp.vertexColl.Reverse();
                        }
                    }

                    selectedPhantom.SketchCollForAllSagittal[SagittalNowSliceNum].sketchInfoColl.Add(SagittalSketchInfoTmp);
                }
                else if (SagittalSketchInfoTmp.vertexColl.Count <= 2)
                {
                    MessageBox.Show("至少需要三个顶点才能围城一个有效的多边形!");
                }

                SagittalSketchInfoTmp = null;
                isSagittalBeginPoint = true;
                isSagittalEndPoint = false;
                selectedCrossSection.RefreshSketch("Sagittal");
            }
        }

        // 判断靶区起点终点的那些参数初始化
        private void RestoreInitialSketchPara(string workMode)
        {
            selectedMaskTypePre = selectedMaskType;

            if (workMode == "Transverse")
            {
                isTransverseBeginPoint = true;
                isTransverseEndPoint = false;
                TransverseNowSliceNum = -1;

                TransverseSketchInfoTmp = null;
                TransverseSketchPolygonForShow = null;

                IsMeasureToolSelected = false;
                DistanceMeasureLine = null;
                IsFirstPoint = true;
            }
            else if (workMode == "Frontal")
            {
                isFrontalBeginPoint = true;
                isFrontalEndPoint = false;
                FrontalNowSliceNum = -1;

                FrontalSketchInfoTmp = null;
                FrontalSketchPolygonForShow = null;

                IsMeasureToolSelected = false;
                DistanceMeasureLine = null;
                IsFirstPoint = true;
            }
            else if (workMode == "Sagittal")
            {
                isSagittalBeginPoint = true;
                isSagittalEndPoint = false;
                SagittalNowSliceNum = -1;

                SagittalSketchInfoTmp = null;
                SagittalSketchPolygonForShow = null;

                IsMeasureToolSelected = false;
                DistanceMeasureLine = null;
                IsFirstPoint = true;
            }
        }

        // 切换层数，切换cell或者是切换靶区类型必须调用该函数
        // 只保存有意义的多边形区域, 其他点、矩形、圆直接抛弃
        // 同样按照逆时针方向存储 http://wenku.baidu.com/link?url=0i75y3UHv-Ol5beS7oXwoDgSpjmG890b8rNNmIhj-6Fnprduu3epIH2ulGj7QanjcaKHmvN9odesUsWSjmMa2eG-3m3h0TQ0cHjBsn1TsOW
        private void SavePreviousInfo(string workMode)
        {
            if (workMode == "Transverse")
            {
                if (TransverseSketchInfoTmp == null)
                {
                    return;
                }
                if (selectedMaskTypePre.HasValue)
                {
                    switch (selectedMaskTypePre.Value)
                    {
                        case MaskType.PointType:
                            break;
                        case MaskType.RectangleType:
                            break;
                        case MaskType.PolygonType:
                            ///
                            if (TransverseNowSliceNum != -1 && TransverseSketchInfoTmp != null && TransverseSketchInfoTmp.vertexColl.Count >= 3)
                            {
                                // 多边形方向判断，参考
                                // http://wenku.baidu.com/link?url=0i75y3UHv-Ol5beS7oXwoDgSpjmG890b8rNNmIhj-6Fnprduu3epIH2ulGj7QanjcaKHmvN9odesUsWSjmMa2eG-3m3h0TQ0cHjBsn1TsOW
                                List<ForVectorIndexCompare> compareList = new List<ForVectorIndexCompare>();

                                Vector3D vector0 = new Vector3D(100000, 0, 1); int index0 = 0;
                                Vector3D vector1 = new Vector3D(-100000, 0, 1); int index1 = 0;
                                Vector3D vector2 = new Vector3D(0, 100000, 1); int index2 = 0;
                                Vector3D vector3 = new Vector3D(0, -100000, 1); int index3 = 0;
                                int indextmp = 0;

                                foreach (var item in TransverseSketchInfoTmp.vertexColl)
                                {
                                    if (vector0.X >= item.X)
                                    {
                                        index0 = indextmp;
                                        vector0.X = item.X; vector0.Y = item.Y;
                                    }
                                    if (vector1.X <= item.X)
                                    {
                                        index1 = indextmp;
                                        vector1.X = item.X; vector1.Y = item.Y;
                                    }
                                    if (vector2.Y >= item.Y)
                                    {
                                        index2 = indextmp;
                                        vector2.X = item.X; vector2.Y = item.Y;
                                    }
                                    if (vector3.Y <= item.Y)
                                    {
                                        index3 = indextmp;
                                        vector3.X = item.X; vector3.Y = item.Y;
                                    }
                                    indextmp++;
                                }

                                bool isRepeated = false;
                                compareList.Add(new ForVectorIndexCompare() { index = index0, vector = vector0 });
                                if (index0 != index1 && (Math.Abs(vector0.X - vector1.X) > 1e-6 || Math.Abs(vector0.Y - vector1.Y) > 1e-6))
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index1, vector = vector1 });
                                }
                                isRepeated = false;
                                foreach (var item in compareList)
                                {
                                    if (item.index == index2 || (Math.Abs(item.vector.X - vector2.X) < 1e-6 && Math.Abs(item.vector.Y - vector2.Y) < 1e-6))
                                    {
                                        isRepeated = true;
                                    }
                                }
                                if (!isRepeated)
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index2, vector = vector2 });
                                }
                                isRepeated = false;
                                foreach (var item in compareList)
                                {
                                    if (item.index == index3 || (Math.Abs(item.vector.X - vector3.X) < 1e-6 && Math.Abs(item.vector.Y - vector3.Y) < 1e-6))
                                    {
                                        isRepeated = true;
                                    }
                                }
                                if (!isRepeated)
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index3, vector = vector3 });
                                }

                                // 从小到大排列
                                compareList.Sort();

                                if (compareList.Count <= 1 || compareList.Count > 4)
                                {
                                    // throw new Exception("判断多边形正反时不应出现最值点<=1与>4这种情况");

                                }
                                else if (compareList.Count == 2)
                                {
                                    indextmp = compareList[0].index;
                                    if (indextmp == 0)
                                    {
                                        indextmp = 1;
                                    }

                                    if (Vector3D.CrossProduct(new Vector3D()
                                    {
                                        X = TransverseSketchInfoTmp.vertexColl[indextmp].X - TransverseSketchInfoTmp.vertexColl[indextmp - 1].X,
                                        Y = TransverseSketchInfoTmp.vertexColl[indextmp].Y - TransverseSketchInfoTmp.vertexColl[indextmp - 1].Y,
                                        Z = 1,
                                    },
                                        new Vector3D()
                                        {
                                            X = TransverseSketchInfoTmp.vertexColl[indextmp + 1].X - TransverseSketchInfoTmp.vertexColl[indextmp].X,
                                            Y = TransverseSketchInfoTmp.vertexColl[indextmp + 1].Y - TransverseSketchInfoTmp.vertexColl[indextmp].Y,
                                            Z = 1,
                                        }).Z < 0)
                                    {
                                        TransverseSketchInfoTmp.vertexColl.Reverse();
                                    }
                                }
                                else
                                {
                                    if (Vector3D.CrossProduct(new Vector3D()
                                    {
                                        X = compareList[1].vector.X - compareList[0].vector.X,
                                        Y = compareList[1].vector.Y - compareList[0].vector.Y,
                                        Z = 1,
                                    },
                                        new Vector3D()
                                        {
                                            X = compareList[2].vector.X - compareList[1].vector.X,
                                            Y = compareList[2].vector.Y - compareList[1].vector.Y,
                                            Z = 1,
                                        }).Z < 0)
                                    {
                                        TransverseSketchInfoTmp.vertexColl.Reverse();
                                    }
                                }
                                selectedPhantom.SketchCollForAllTransverse[TransverseNowSliceNum].sketchInfoColl.Add(TransverseSketchInfoTmp);

                            }
                            else
                            {
                                MessageBox.Show("需要至少三个顶点才能构成一个多边形");
                            }

                            TransverseSketchInfoTmp = null;
                            isTransverseBeginPoint = true;
                            isTransverseEndPoint = false;
                            selectedCrossSection.RefreshSketch("Transverse");
                            break;
                        case MaskType.CircleType:
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (workMode == "Frontal")
            {
                if (FrontalSketchInfoTmp == null)
                {
                    return;
                }
                if (selectedMaskTypePre.HasValue)
                {
                    switch (selectedMaskTypePre.Value)
                    {
                        case MaskType.PointType:
                            break;
                        case MaskType.RectangleType:
                            break;
                        case MaskType.PolygonType:
                            ///
                            if (FrontalNowSliceNum != -1 && FrontalSketchInfoTmp != null && FrontalSketchInfoTmp.vertexColl.Count >= 3)
                            {
                                // 多边形方向判断，参考
                                // http://wenku.baidu.com/link?url=0i75y3UHv-Ol5beS7oXwoDgSpjmG890b8rNNmIhj-6Fnprduu3epIH2ulGj7QanjcaKHmvN9odesUsWSjmMa2eG-3m3h0TQ0cHjBsn1TsOW
                                List<ForVectorIndexCompare> compareList = new List<ForVectorIndexCompare>();

                                Vector3D vector0 = new Vector3D(100000, 0, 1); int index0 = 0;
                                Vector3D vector1 = new Vector3D(-100000, 0, 1); int index1 = 0;
                                Vector3D vector2 = new Vector3D(0, 100000, 1); int index2 = 0;
                                Vector3D vector3 = new Vector3D(0, -100000, 1); int index3 = 0;
                                int indextmp = 0;

                                foreach (var item in FrontalSketchInfoTmp.vertexColl)
                                {
                                    if (vector0.X >= item.X)
                                    {
                                        index0 = indextmp;
                                        vector0.X = item.X; vector0.Y = item.Y;
                                    }
                                    if (vector1.X <= item.X)
                                    {
                                        index1 = indextmp;
                                        vector1.X = item.X; vector1.Y = item.Y;
                                    }
                                    if (vector2.Y >= item.Y)
                                    {
                                        index2 = indextmp;
                                        vector2.X = item.X; vector2.Y = item.Y;
                                    }
                                    if (vector3.Y <= item.Y)
                                    {
                                        index3 = indextmp;
                                        vector3.X = item.X; vector3.Y = item.Y;
                                    }
                                    indextmp++;
                                }

                                bool isRepeated = false;
                                compareList.Add(new ForVectorIndexCompare() { index = index0, vector = vector0 });
                                if (index0 != index1 && (Math.Abs(vector0.X - vector1.X) > 1e-6 || Math.Abs(vector0.Y - vector1.Y) > 1e-6))
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index1, vector = vector1 });
                                }
                                isRepeated = false;
                                foreach (var item in compareList)
                                {
                                    if (item.index == index2 || (Math.Abs(item.vector.X - vector2.X) < 1e-6 && Math.Abs(item.vector.Y - vector2.Y) < 1e-6))
                                    {
                                        isRepeated = true;
                                    }
                                }
                                if (!isRepeated)
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index2, vector = vector2 });
                                }
                                isRepeated = false;
                                foreach (var item in compareList)
                                {
                                    if (item.index == index3 || (Math.Abs(item.vector.X - vector3.X) < 1e-6 && Math.Abs(item.vector.Y - vector3.Y) < 1e-6))
                                    {
                                        isRepeated = true;
                                    }
                                }
                                if (!isRepeated)
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index3, vector = vector3 });
                                }

                                // 从小到大排列
                                compareList.Sort();

                                if (compareList.Count <= 1 || compareList.Count > 4)
                                {
                                    // throw new Exception("判断多边形正反时不应出现最值点<=1与>4这种情况");
                                }
                                else if (compareList.Count == 2)
                                {
                                    indextmp = compareList[0].index;
                                    if (indextmp == 0)
                                    {
                                        indextmp = 1;
                                    }

                                    if (Vector3D.CrossProduct(new Vector3D()
                                    {
                                        X = FrontalSketchInfoTmp.vertexColl[indextmp].X - FrontalSketchInfoTmp.vertexColl[indextmp - 1].X,
                                        Y = FrontalSketchInfoTmp.vertexColl[indextmp].Y - FrontalSketchInfoTmp.vertexColl[indextmp - 1].Y,
                                        Z = 1,
                                    },
                                        new Vector3D()
                                        {
                                            X = FrontalSketchInfoTmp.vertexColl[indextmp + 1].X - FrontalSketchInfoTmp.vertexColl[indextmp].X,
                                            Y = FrontalSketchInfoTmp.vertexColl[indextmp + 1].Y - FrontalSketchInfoTmp.vertexColl[indextmp].Y,
                                            Z = 1,
                                        }).Z < 0)
                                    {
                                        FrontalSketchInfoTmp.vertexColl.Reverse();
                                    }
                                }
                                else
                                {
                                    if (Vector3D.CrossProduct(new Vector3D()
                                    {
                                        X = compareList[1].vector.X - compareList[0].vector.X,
                                        Y = compareList[1].vector.Y - compareList[0].vector.Y,
                                        Z = 1,
                                    },
                                        new Vector3D()
                                        {
                                            X = compareList[2].vector.X - compareList[1].vector.X,
                                            Y = compareList[2].vector.Y - compareList[1].vector.Y,
                                            Z = 1,
                                        }).Z < 0)
                                    {
                                        FrontalSketchInfoTmp.vertexColl.Reverse();
                                    }
                                }

                                selectedPhantom.SketchCollForAllFrontal[FrontalNowSliceNum].sketchInfoColl.Add(FrontalSketchInfoTmp);
                            }
                            else
                            {
                                MessageBox.Show("需要至少三个顶点才能构成一个多边形");
                            }

                            FrontalSketchInfoTmp = null;
                            isFrontalBeginPoint = true;
                            isFrontalEndPoint = false;
                            selectedCrossSection.RefreshSketch("Frontal");
                            break;
                        case MaskType.CircleType:
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (workMode == "Sagittal")
            {
                if (SagittalSketchInfoTmp == null)
                {
                    return;
                }
                if (selectedMaskTypePre.HasValue)
                {
                    switch (selectedMaskTypePre.Value)
                    {
                        case MaskType.PointType:
                            break;
                        case MaskType.RectangleType:
                            break;
                        case MaskType.PolygonType:
                            ////
                            if (SagittalNowSliceNum != -1 && SagittalSketchInfoTmp != null && SagittalSketchInfoTmp.vertexColl.Count >= 3)
                            {
                                // 多边形方向判断，参考
                                // http://wenku.baidu.com/link?url=0i75y3UHv-Ol5beS7oXwoDgSpjmG890b8rNNmIhj-6Fnprduu3epIH2ulGj7QanjcaKHmvN9odesUsWSjmMa2eG-3m3h0TQ0cHjBsn1TsOW
                                List<ForVectorIndexCompare> compareList = new List<ForVectorIndexCompare>();

                                Vector3D vector0 = new Vector3D(100000, 0, 1); int index0 = 0;
                                Vector3D vector1 = new Vector3D(-100000, 0, 1); int index1 = 0;
                                Vector3D vector2 = new Vector3D(0, 100000, 1); int index2 = 0;
                                Vector3D vector3 = new Vector3D(0, -100000, 1); int index3 = 0;
                                int indextmp = 0;

                                foreach (var item in SagittalSketchInfoTmp.vertexColl)
                                {
                                    if (vector0.X >= item.X)
                                    {
                                        index0 = indextmp;
                                        vector0.X = item.X; vector0.Y = item.Y;
                                    }
                                    if (vector1.X <= item.X)
                                    {
                                        index1 = indextmp;
                                        vector1.X = item.X; vector1.Y = item.Y;
                                    }
                                    if (vector2.Y >= item.Y)
                                    {
                                        index2 = indextmp;
                                        vector2.X = item.X; vector2.Y = item.Y;
                                    }
                                    if (vector3.Y <= item.Y)
                                    {
                                        index3 = indextmp;
                                        vector3.X = item.X; vector3.Y = item.Y;
                                    }
                                    indextmp++;
                                }

                                bool isRepeated = false;
                                compareList.Add(new ForVectorIndexCompare() { index = index0, vector = vector0 });
                                if (index0 != index1 && (Math.Abs(vector0.X - vector1.X) > 1e-6 || Math.Abs(vector0.Y - vector1.Y) > 1e-6))
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index1, vector = vector1 });
                                }
                                isRepeated = false;
                                foreach (var item in compareList)
                                {
                                    if (item.index == index2 || (Math.Abs(item.vector.X - vector2.X) < 1e-6 && Math.Abs(item.vector.Y - vector2.Y) < 1e-6))
                                    {
                                        isRepeated = true;
                                    }
                                }
                                if (!isRepeated)
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index2, vector = vector2 });
                                }
                                isRepeated = false;
                                foreach (var item in compareList)
                                {
                                    if (item.index == index3 || (Math.Abs(item.vector.X - vector3.X) < 1e-6 && Math.Abs(item.vector.Y - vector3.Y) < 1e-6))
                                    {
                                        isRepeated = true;
                                    }
                                }
                                if (!isRepeated)
                                {
                                    compareList.Add(new ForVectorIndexCompare() { index = index3, vector = vector3 });
                                }

                                // 从小到大排列
                                compareList.Sort();

                                if (compareList.Count <= 1 || compareList.Count > 4)
                                {
                                    // throw new Exception("判断多边形正反时不应出现最值点<=1与>4这种情况");
                                }
                                else if (compareList.Count == 2)
                                {
                                    indextmp = compareList[0].index;
                                    if (indextmp == 0)
                                    {
                                        indextmp = 1;
                                    }

                                    if (Vector3D.CrossProduct(new Vector3D()
                                    {
                                        X = SagittalSketchInfoTmp.vertexColl[indextmp].X - SagittalSketchInfoTmp.vertexColl[indextmp - 1].X,
                                        Y = SagittalSketchInfoTmp.vertexColl[indextmp].Y - SagittalSketchInfoTmp.vertexColl[indextmp - 1].Y,
                                        Z = 1,
                                    },
                                        new Vector3D()
                                        {
                                            X = SagittalSketchInfoTmp.vertexColl[indextmp + 1].X - SagittalSketchInfoTmp.vertexColl[indextmp].X,
                                            Y = SagittalSketchInfoTmp.vertexColl[indextmp + 1].Y - SagittalSketchInfoTmp.vertexColl[indextmp].Y,
                                            Z = 1,
                                        }).Z < 0)
                                    {
                                        SagittalSketchInfoTmp.vertexColl.Reverse();
                                    }
                                }
                                else
                                {
                                    if (Vector3D.CrossProduct(new Vector3D()
                                    {
                                        X = compareList[1].vector.X - compareList[0].vector.X,
                                        Y = compareList[1].vector.Y - compareList[0].vector.Y,
                                        Z = 1,
                                    },
                                        new Vector3D()
                                        {
                                            X = compareList[2].vector.X - compareList[1].vector.X,
                                            Y = compareList[2].vector.Y - compareList[1].vector.Y,
                                            Z = 1,
                                        }).Z < 0)
                                    {
                                        SagittalSketchInfoTmp.vertexColl.Reverse();
                                    }
                                }

                                selectedPhantom.SketchCollForAllSagittal[SagittalNowSliceNum].sketchInfoColl.Add(SagittalSketchInfoTmp);
                            }
                            else
                            {
                                MessageBox.Show("需要至少三个顶点才能构成一个多边形");
                            }

                            SagittalSketchInfoTmp = null;
                            isSagittalBeginPoint = true;
                            isSagittalEndPoint = false;
                            selectedCrossSection.RefreshSketch("Sagittal");
                            break;
                        case MaskType.CircleType:
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                throw new Exception("保存上一个工作区域的靶区信息出现问题!");
            }
        }

        // 删除该Canvas 下面的所有多边形
        void DelteAllPolygon(Canvas workCanvas)
        {
            if (workCanvas != null)
            {
                for (int i = 0; i < workCanvas.Children.Count; i++)
                {
                    if (workCanvas.Children[i] is Polygon)
                    {
                        workCanvas.Children.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        void RefreshCanvasSketch(string workMode)
        {
            int sliceNum = -1;
            if (workMode == "Transverse")
            {
                sliceNum = selectedCrossSection.TransverseNowSliceNum;
                foreach (var item in selectedPhantom.SketchCollForAllTransverse[sliceNum].sketchInfoColl)
                {
                    switch (item.maskType)
                    {
                        case MaskType.PointType:
                            Polygon newPolygonPoint = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonPoint.Points.Add(item1);
                            }
                            newPolygonPoint.RenderTransform = TransverseImage.RenderTransform;
                            newPolygonPoint.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPoint.StrokeThickness = 0.1;
                            newPolygonPoint.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPoint.SnapsToDevicePixels = true;
                            TransverseCanvas.Children.Add(newPolygonPoint);
                            break;
                        case MaskType.RectangleType:
                            Polygon newPolygonRectangle = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonRectangle.Points.Add(item1);
                            }
                            newPolygonRectangle.RenderTransform = TransverseImage.RenderTransform;
                            newPolygonRectangle.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonRectangle.StrokeThickness = 0.1;
                            newPolygonRectangle.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonRectangle.SnapsToDevicePixels = true;
                            TransverseCanvas.Children.Add(newPolygonRectangle);
                            break;
                        case MaskType.PolygonType:
                            Polygon newPolygonPolygon = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonPolygon.Points.Add(item1);
                            }
                            newPolygonPolygon.RenderTransform = TransverseImage.RenderTransform;
                            newPolygonPolygon.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPolygon.StrokeThickness = 0.1;
                            newPolygonPolygon.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPolygon.SnapsToDevicePixels = true;
                            TransverseCanvas.Children.Add(newPolygonPolygon);
                            break;
                        case MaskType.CircleType:
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (workMode == "Frontal")
            {
                sliceNum = selectedCrossSection.FrontalNowSliceNum;
                foreach (var item in selectedPhantom.SketchCollForAllFrontal[sliceNum].sketchInfoColl)
                {
                    switch (item.maskType)
                    {
                        case MaskType.PointType:
                            Polygon newPolygonPoint = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonPoint.Points.Add(item1);
                            }
                            newPolygonPoint.RenderTransform = FrontalImage.RenderTransform;
                            newPolygonPoint.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPoint.StrokeThickness = 0.1;
                            newPolygonPoint.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPoint.SnapsToDevicePixels = true;
                            FrontalCanvas.Children.Add(newPolygonPoint);
                            break;
                        case MaskType.RectangleType:
                            Polygon newPolygonRectangle = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonRectangle.Points.Add(item1);
                            }
                            newPolygonRectangle.RenderTransform = FrontalImage.RenderTransform;
                            newPolygonRectangle.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonRectangle.StrokeThickness = 0.1;
                            newPolygonRectangle.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonRectangle.SnapsToDevicePixels = true;
                            FrontalCanvas.Children.Add(newPolygonRectangle);
                            break;
                        case MaskType.PolygonType:
                            Polygon newPolygonPolygon = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonPolygon.Points.Add(item1);
                            }
                            newPolygonPolygon.RenderTransform = FrontalImage.RenderTransform;
                            newPolygonPolygon.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPolygon.StrokeThickness = 0.1;
                            newPolygonPolygon.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPolygon.SnapsToDevicePixels = true;
                            FrontalCanvas.Children.Add(newPolygonPolygon);
                            break;
                        case MaskType.CircleType:
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (workMode == "Sagittal")
            {
                sliceNum = selectedCrossSection.SagittalNowSliceNum;
                foreach (var item in selectedPhantom.SketchCollForAllSagittal[sliceNum].sketchInfoColl)
                {
                    switch (item.maskType)
                    {
                        case MaskType.PointType:
                            Polygon newPolygonPoint = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonPoint.Points.Add(item1);
                            }
                            newPolygonPoint.RenderTransform = SagittalImage.RenderTransform;
                            newPolygonPoint.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPoint.StrokeThickness = 0.1;
                            newPolygonPoint.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPoint.SnapsToDevicePixels = true;
                            SagittalCanvas.Children.Add(newPolygonPoint);
                            break;
                        case MaskType.RectangleType:
                            Polygon newPolygonRectangle = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonRectangle.Points.Add(item1);
                            }
                            newPolygonRectangle.RenderTransform = SagittalImage.RenderTransform;
                            newPolygonRectangle.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonRectangle.StrokeThickness = 0.1;
                            newPolygonRectangle.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonRectangle.SnapsToDevicePixels = true;
                            SagittalCanvas.Children.Add(newPolygonRectangle);
                            break;
                        case MaskType.PolygonType:
                            Polygon newPolygonPolygon = new Polygon();
                            foreach (var item1 in item.vertexColl)
                            {
                                newPolygonPolygon.Points.Add(item1);
                            }
                            newPolygonPolygon.RenderTransform = SagittalImage.RenderTransform;
                            newPolygonPolygon.Stroke = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPolygon.StrokeThickness = 0.1;
                            newPolygonPolygon.Fill = new SolidColorBrush(selectedPhantom.CellsCollectionInAPhantom.AllCells[item.cellIndex].CellColor);
                            newPolygonPolygon.SnapsToDevicePixels = true;
                            SagittalCanvas.Children.Add(newPolygonPolygon);
                            break;
                        case MaskType.CircleType:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        bool IsMeasureToolSelected = false;
        Line DistanceMeasureLine = null;
        bool IsFirstPoint = true;

        // 这项工作暂不做，牵扯有些多，需要加一个标志当前工作内容的变量或者是枚举
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IsMeasureToolSelected = true;
            IsFirstPoint = true;
            if (DistanceMeasureLine != null && TransverseCanvas != null)
            {
                TransverseCanvas.Children.Remove(DistanceMeasureLine);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MCNPFileEditor.CrossSectionImageShow.FillTest newFillTest = new FillTest();
            newFillTest.Owner = this;
            newFillTest.SketchCollForTheTransverse = selectedCrossSection.SketchInATranseverseSlice;
            newFillTest.selectedCrossSection = selectedCrossSection;
            newFillTest.Show();
        }

        // 根据所画靶区替换体素
        BackgroundWorker RaplaceBG;
        public List<int> specifiedOrganList = new List<int>(); // 储存需要变换的体素
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (selectedPhantom != null && selectedPhantom.SketchCollForAllTransverse != null)
            {
                int IsNotNullNum = 0;
                for (int i = 0; i < selectedPhantom.SketchCollForAllTransverse.Length; i++)
                {
                    if (selectedPhantom.SketchCollForAllTransverse[i] != null
                        && selectedPhantom.SketchCollForAllTransverse[i].sketchInfoColl != null
                        && selectedPhantom.SketchCollForAllTransverse[i].sketchInfoColl.Count >= 1)
                    {
                        IsNotNullNum++;
                    }
                }
                ReplaceProgressBar.Maximum = IsNotNullNum;

                RaplaceBG = (BackgroundWorker)this.FindResource("backgroundWorkerForReplace");

                // 得到框选在多边形里面的元素
                if (specifiedOrganList == null)
                {
                    specifiedOrganList = new List<int>();
                }
                Phantom thePhantom = selectedPhantom;
                for (int i = 0; i < thePhantom.SketchCollForAllTransverse.Length; i++)
                {
                    if (thePhantom.SketchCollForAllTransverse[i] != null
                        && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl != null
                        && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl.Count >= 1)
                    {
                        IsNotNullNum++;
                    }
                }

                // 按层数遍历
                for (int i = 0; i < thePhantom.SketchCollForAllTransverse.Length; i++)
                {
                    // 临时储存替换的体素位置
                    short[,] RaplaceMask = new short[thePhantom.RepeatStructureInAPhantom.DimY, thePhantom.RepeatStructureInAPhantom.DimX];
                    for (int j = 0; j < thePhantom.RepeatStructureInAPhantom.DimY; j++)
                    {
                        for (int k = 0; k < thePhantom.RepeatStructureInAPhantom.DimX; k++)
                        {
                            RaplaceMask[j, k] = -1;
                        }
                    }

                    if (thePhantom.SketchCollForAllTransverse[i] != null
                        && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl != null
                        && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl.Count >= 1)
                    {
                        foreach (var item in thePhantom.SketchCollForAllTransverse[i].sketchInfoColl)
                        {
                            bool[,] SketchTmp = MathFunction.SketchMask(item.vertexColl, thePhantom.RepeatStructureInAPhantom.DimX, thePhantom.RepeatStructureInAPhantom.DimY);
                            for (int mm = 0; mm < thePhantom.RepeatStructureInAPhantom.DimY; mm++)
                            {
                                for (int nn = 0; nn < thePhantom.RepeatStructureInAPhantom.DimX; nn++)
                                {
                                    if (SketchTmp[mm, nn])
                                    {
                                        RaplaceMask[mm, nn] = (short)item.cellIndex;
                                    }
                                }
                            }
                        }
                        for (int mm = 0; mm < thePhantom.RepeatStructureInAPhantom.DimY; mm++)
                        {
                            for (int nn = 0; nn < thePhantom.RepeatStructureInAPhantom.DimX; nn++)
                            {
                                if (RaplaceMask[mm, nn] != -1)
                                {
                                    // Mask索引与thePhantom.RepeatStructureInAPhantom.RepeatMatrix不同
                                    // thePhantom.RepeatStructureInAPhantom.RepeatMatrix[i - 1, mm, nn] = RaplaceMask[mm, nn];
                                    if (!specifiedOrganList.Where(x => x == thePhantom.RepeatStructureInAPhantom.RepeatMatrix[i - 1, mm, nn]).ToArray().Any())
                                    {
                                        specifiedOrganList.Add(thePhantom.RepeatStructureInAPhantom.RepeatMatrix[i - 1, mm, nn]);
                                    }
                                }
                            }
                        }
                    }
                }

                InputReplaceOrgans InputReplaceOrganswindow = new InputReplaceOrgans()
                {
                    specifiedOrganList = specifiedOrganList,
                };

                InputReplaceOrganswindow.ShowDialog();
                if (InputReplaceOrganswindow.isFullyOperate)
                {
                    specifiedOrganList = InputReplaceOrganswindow.specifiedOrganList;

                    RaplaceBG.RunWorkerAsync(selectedPhantom);
                }
            }
        }

        private void BackgroundWorker_DoWork_1(object sender, DoWorkEventArgs e)
        {
            if (specifiedOrganList == null)
            {
                specifiedOrganList = new List<int>();
            }
            Phantom thePhantom = (Phantom)e.Argument;
            double progressIndex = 0;
            int IsNotNullNum = 0;
            for (int i = 0; i < thePhantom.SketchCollForAllTransverse.Length; i++)
            {
                if (thePhantom.SketchCollForAllTransverse[i] != null
                    && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl != null
                    && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl.Count >= 1)
                {
                    IsNotNullNum++;
                }
            }

            // 按层数遍历
            for (int i = 0; i < thePhantom.SketchCollForAllTransverse.Length; i++)
            {
                // 临时储存替换的体素位置
                short[,] RaplaceMask = new short[thePhantom.RepeatStructureInAPhantom.DimY, thePhantom.RepeatStructureInAPhantom.DimX];
                for (int j = 0; j < thePhantom.RepeatStructureInAPhantom.DimY; j++)
                {
                    for (int k = 0; k < thePhantom.RepeatStructureInAPhantom.DimX; k++)
                    {
                        RaplaceMask[j, k] = -1;
                    }
                }

                if (thePhantom.SketchCollForAllTransverse[i] != null
                    && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl != null
                    && thePhantom.SketchCollForAllTransverse[i].sketchInfoColl.Count >= 1)
                {
                    foreach (var item in thePhantom.SketchCollForAllTransverse[i].sketchInfoColl)
                    {
                        bool[,] SketchTmp = MathFunction.SketchMask(item.vertexColl, thePhantom.RepeatStructureInAPhantom.DimX, thePhantom.RepeatStructureInAPhantom.DimY);
                        for (int mm = 0; mm < thePhantom.RepeatStructureInAPhantom.DimY; mm++)
                        {
                            for (int nn = 0; nn < thePhantom.RepeatStructureInAPhantom.DimX; nn++)
                            {
                                if (SketchTmp[mm, nn])
                                {
                                    RaplaceMask[mm, nn] = (short)item.cellIndex;
                                }
                            }
                        }
                    }
                    for (int mm = 0; mm < thePhantom.RepeatStructureInAPhantom.DimY; mm++)
                    {
                        for (int nn = 0; nn < thePhantom.RepeatStructureInAPhantom.DimX; nn++)
                        {
                            if (RaplaceMask[mm, nn] != -1)
                            {
                                // Mask索引与thePhantom.RepeatStructureInAPhantom.RepeatMatrix不同
                                if (specifiedOrganList.Where(x => x == thePhantom.RepeatStructureInAPhantom.RepeatMatrix[i - 1, mm, nn]).ToArray().Any())
                                    thePhantom.RepeatStructureInAPhantom.RepeatMatrix[i - 1, mm, nn] = RaplaceMask[mm, nn];
                                // if(! specifiedOrganList.Where(x=>x== thePhantom.RepeatStructureInAPhantom.RepeatMatrix[i - 1, mm, nn]).ToArray().Any())
                                // {
                                //     specifiedOrganList.Add(thePhantom.RepeatStructureInAPhantom.RepeatMatrix[i - 1, mm, nn]);
                                // }

                            }
                        }
                    }

                    progressIndex++;
                    RaplaceBG.ReportProgress((int)((progressIndex / IsNotNullNum) * 100));
                }
            }
        }

        private void BackgroundWorker_RunWorkerCompleted_1(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("FinishREplace");
            selectedPhantom.ClearSketchCollForAll();
            ReplaceProgressBar.Value = 0;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ReplaceProgressBar.Value = e.ProgressPercentage;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            //System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            //sfd.RestoreDirectory = true;
            //sfd.OverwritePrompt = true;
            //sfd.CheckPathExists = true;
            //if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    string outfilename = sfd.FileName;
            //    selectedPhantom.OutPutPhantom(outfilename);
            //    MessageBox.Show("完成输出");
            //}

            OutputWindow outputWindow = new OutputWindow(phantomsCollection ,selectedPhantom);
            outputWindow.ShowDialog();
        }

        List<int> OrgansTobeMove = new List<int>() { 4, 6, 74, 96, 98, 111, 112, 116, 117, 118, 122, 123 };
        string organsMoveDirection = "X";
        int organsMoceDis = 0;
        int additionOrgan = 119; // 补充空位的器官
        bool shouldForceReplace = true;
        // 器官平移选择
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            InputTobeMoveOrgans newInputTobeMoveOrgans = new InputTobeMoveOrgans();
            newInputTobeMoveOrgans.OrgansTobeMove = OrgansTobeMove;
            newInputTobeMoveOrgans.ShowDialog();
            if (newInputTobeMoveOrgans.isFullyOperation)
            {
                organsMoveDirection = newInputTobeMoveOrgans.organsMoveDirection;
                organsMoceDis = newInputTobeMoveOrgans.organsMoceDis;
                additionOrgan = newInputTobeMoveOrgans.additionOrgan;
                shouldForceReplace = newInputTobeMoveOrgans.shouldForceReplace;

                selectedPhantom.MoveOrgan(OrgansTobeMove, organsMoveDirection, organsMoceDis, additionOrgan, shouldForceReplace);

                MessageBox.Show("完成替换");
            }
        }
        // 统计体模的相关信息
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            MCNPFileEditor.CrossSectionImageShow.subWindowSliceInfo newsubWindowSliceInfo = new MCNPFileEditor.CrossSectionImageShow.subWindowSliceInfo(this.selectedPhantom);
            // newsubWindowSliceInfo.selectedPhantom = this.selectedPhantom;
            // newsubWindowSliceInfo.refreshInfo();
            newsubWindowSliceInfo.ShowDialog();
        }

        //更改每个体素的颜色表示
        private void SelectedPhantomCellinfoListView_MouseDoubleClick(object sender, RoutedEventArgs e)
        {

            Cell CellShouldBeChanged = null; // 需要修改显示颜色的cell

            if (SelectedPhantomCellinfoListView.ItemsSource != null && SelectedPhantomCellinfoListView.SelectedItem != null)
            {
                Cell selectedCell = SelectedPhantomCellinfoListView.SelectedItem as Cell;
                int cellIndex = selectedCell.CellIndex;

                foreach (var item in selectedPhantom.CellsCollectionInAPhantom.AllCells)
                {
                    if (item.CellIndex == cellIndex)
                    {
                        CellShouldBeChanged = item;
                        break;
                    }
                }
            }

            if (CellShouldBeChanged == null)
            {
                MessageBox.Show("查找Cell出错");
            }
            else
            {
                CellColorChange newCellColorChange = new CellColorChange(CellShouldBeChanged);
                newCellColorChange.ShowDialog();
                if (newCellColorChange.isAccepted)
                {
                    CellShouldBeChanged.CellColor = newCellColorChange.CellColor;
                    SelectedPhantomCellinfoListView.ItemsSource = selectedPhantom.CellsCollectionInAPhantom.AllCells.Where(x => (x != null && x.IsCellEffective == true));
                }
            }
        }

        private void SelectedPhantomCellinfoListView_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            object selectItem = SelectedPhantomCellinfoListView.SelectedItem;
            int selectedIndex = SelectedPhantomCellinfoListView.SelectedIndex;

            if (selectItem != null)
            {
                // MessageBox.Show("{0,5}, selectedIndex");
                // MessageBox.Show(selectItem.ToString());
                // MessageBox.Show(sender.ToString());
                selectedCell = (Cell)selectItem;
                SelectedOrganGroupBox.DataContext = selectedCell;

                SavePreviousInfo("Transverse");
                SavePreviousInfo("Frontal");
                SavePreviousInfo("Sagittal");

                TransverseSketchBrush = new SolidColorBrush(selectedCell.CellColor);
                FrontalSketchBrush = new SolidColorBrush(selectedCell.CellColor);
                SagittalSketchBrush = new SolidColorBrush(selectedCell.CellColor);
            }

            Cell CellShouldBeChanged = null; // 需要修改显示颜色的cell

            if (SelectedPhantomCellinfoListView.ItemsSource != null && SelectedPhantomCellinfoListView.SelectedItem != null)
            {
                Cell selectedCell = SelectedPhantomCellinfoListView.SelectedItem as Cell;
                int cellIndex = selectedCell.CellIndex;

                foreach (var item in selectedPhantom.CellsCollectionInAPhantom.AllCells)
                {
                    if (item != null && item.CellIndex == cellIndex)
                    {
                        CellShouldBeChanged = item;
                        break;
                    }
                }
            }

            if (CellShouldBeChanged == null)
            {
                MessageBox.Show("查找Cell出错");
            }
            else
            {
                CellColorChange newCellColorChange = new CellColorChange(CellShouldBeChanged);
                newCellColorChange.ShowDialog();
                if (newCellColorChange.isAccepted)
                {
                    CellShouldBeChanged.CellColor = newCellColorChange.CellColor;
                    SelectedPhantomCellinfoListView.ItemsSource = selectedPhantom.CellsCollectionInAPhantom.AllCells.Where(x => (x != null && x.IsCellEffective == true));

                    {
                        if (CellShouldBeChanged != null)
                        {
                            // MessageBox.Show("{0,5}, selectedIndex");
                            // MessageBox.Show(selectItem.ToString());
                            // MessageBox.Show(sender.ToString());
                            SelectedOrganGroupBox.DataContext = CellShouldBeChanged;

                            SavePreviousInfo("Transverse");
                            SavePreviousInfo("Frontal");
                            SavePreviousInfo("Sagittal");

                            TransverseSketchBrush = new SolidColorBrush(CellShouldBeChanged.CellColor);
                            FrontalSketchBrush = new SolidColorBrush(CellShouldBeChanged.CellColor);
                            SagittalSketchBrush = new SolidColorBrush(CellShouldBeChanged.CellColor);
                        }
                    }
                }
            }
        }

        // 非正式
        // 用于复制当前层的靶区到其他所有层
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            int sliceNum = selectedPhantom.RepeatStructureInAPhantom.UpperBoundZ - selectedPhantom.RepeatStructureInAPhantom.LowerBoundZ + 1;
            int transverseNowSliceNum = selectedCrossSection.TransverseNowSliceNum;

            SketchCollInASlice transverseNowSliceSketch = selectedPhantom.SketchCollForAllTransverse[transverseNowSliceNum];

            InputBound newInputBound = new InputBound();
            newInputBound.ShowDialog();
            if (newInputBound.IsOperationEffective)
            {
                int beginningNum = 0;
                int endNum = 0;
                beginningNum = (int) Math.Round(newInputBound.LowerBoundValue);
                endNum = (int) Math.Round(newInputBound.UpperBoundValue);
                if (endNum <= beginningNum)
                {
                    endNum = beginningNum;
                }
                beginningNum = beginningNum < 1 ? 1 : beginningNum;
                endNum = endNum > sliceNum ? sliceNum : endNum;


                for (int i = beginningNum; i <= endNum; i++)
                {
                    if (i != transverseNowSliceNum)
                    {
                        foreach (var item in transverseNowSliceSketch.sketchInfoColl)
                        {
                            if(!selectedPhantom.SketchCollForAllTransverse[i].sketchInfoColl.Exists(x=>x == item)) // 不存在才添加
                                selectedPhantom.SketchCollForAllTransverse[i].sketchInfoColl.Add(item);
                        }
                    }
                }

                MessageBox.Show("Finish");
            }
            else
            {
                MessageBox.Show("Do nothing!");
            }
        }

        // 某一界面的图大屏显示
        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            CrossSectionImageBigShow newCrossSectionImageBigShow = new CrossSectionImageBigShow(selectedPhantom, selectedCrossSection);
            newCrossSectionImageBigShow.Show();
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {

        }
        
        // 靶区勾画使用圆形工具时用于设置勾画圆的半径
        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            MCNPFileEditor.CrossSectionImageShow.RadiusSetWindow newRadiusSetWindow = new RadiusSetWindow();
            newRadiusSetWindow.ShowDialog();
            if (newRadiusSetWindow.RadiusSet != -1)
            {
                drawCircleRadius = newRadiusSetWindow.RadiusSet;
            }
            
            // throw new NotImplementedException();
        }


        private void outputPhantomToBin_buttomClick(object sender, RoutedEventArgs e)
        {
            selectedPhantom.OutputPhantomToBinaryFile();

            MessageBox.Show("Finished!");
        }

        // 更改器官编号，控制窗口弹出
        private void organIndexReplace_buttomClick(object sender, RoutedEventArgs e)
        {
            OrganIdChange organIdChangeWindow = new OrganIdChange(phantomsCollection);
            organIdChangeWindow.ShowDialog();
        }
    }

    public class CrossSection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // 当前工作的体模
        Phantom nowPhantom;

        // Transeverse 图像
        WriteableBitmap TransverseImg_;
        public WriteableBitmap TransverseImg
        {
            set
            {
                TransverseImg_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TransverseImg_"));
                }
            }
            get
            {
                return TransverseImg_;
            }
        }

        // Transeverse 靶区
        public SketchCollInASlice SketchInATranseverseSlice;

        // Frontal 图像
        WriteableBitmap FrontalImg_;
        public WriteableBitmap FrontalImg
        {
            set
            {
                FrontalImg_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FrontalImg_"));
                }
            }
            get
            {
                return FrontalImg_;
            }
        }

        // Frontal 靶区
        public SketchCollInASlice SketchInAFrontalSlice;

        // Sagittal 图像
        WriteableBitmap SagittalImg_;
        public WriteableBitmap SagittalImg
        {
            set
            {
                SagittalImg_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SagittalImg_"));
                }
            }
            get
            {
                return SagittalImg_;
            }
        }

        // Sagittal 靶区
        public SketchCollInASlice SketchInASagittalSlice;

        // 器官三维矩阵
        short[,,] PhantomMatrix3D_;
        public short[,,] PhantomMatrix3D
        {
            set
            {
                PhantomMatrix3D_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PhantomMatrix3D_"));
                }
            }
            get
            {
                return PhantomMatrix3D_;
            }
        }

        // Transeverse 宽
        int TransverseWidth_;
        public int TransverseWidth
        {
            set
            {
                TransverseWidth_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TransverseWidth_"));
                }
            }
            get
            {
                return TransverseWidth_;
            }
        }

        // Transeverse 高
        int TransverseHeight_;
        public int TransverseHeight
        {
            set
            {
                TransverseHeight_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TransverseHeight_"));
                }
            }
            get
            {
                return TransverseHeight_;
            }
        }

        // Transeverse 总层数
        int TransverseTotalSliceNum_;
        public int TransverseTotalSliceNum
        {
            set
            {
                TransverseTotalSliceNum_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TransverseTotalSliceNum_"));
                }
            }
            get
            {
                return TransverseTotalSliceNum_;
            }
        }

        // Transeverse 当前层数,从1开始
        int TransverseNowSliceNum_;
        public int TransverseNowSliceNum
        {
            set
            {
                TransverseNowSliceNum_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TransverseNowSliceNum_"));
                }
            }
            get
            {
                return TransverseNowSliceNum_;
            }
        }

        // Frontal 宽
        int FrontalWidth_;
        public int FrontalWidth
        {
            set
            {
                FrontalWidth_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FrontalWidth_"));
                }
            }
            get
            {
                return FrontalWidth_;
            }
        }

        // Frontal 高
        int FrontalHeight_;
        public int FrontalHeight
        {
            set
            {
                FrontalHeight_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FrontalHeight_"));
                }
            }
            get
            {
                return FrontalHeight_;
            }
        }

        // Frontal 总层数
        int FrontalTotalSliceNum_;
        public int FrontalTotalSliceNum
        {
            set
            {
                FrontalTotalSliceNum_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FrontalTotalSliceNum_"));
                }
            }
            get
            {
                return FrontalTotalSliceNum_;
            }
        }

        // Frontal 当前层数,从1开始
        int FrontalNowSliceNum_;
        public int FrontalNowSliceNum
        {
            set
            {
                FrontalNowSliceNum_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FrontalNowSliceNum_"));
                }
            }
            get
            {
                return FrontalNowSliceNum_;
            }
        }

        // Sagittal 宽
        int SagittalWidth_;
        public int SagittalWidth
        {
            set
            {
                SagittalWidth_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SagittalWidth_"));
                }
            }
            get
            {
                return SagittalWidth_;
            }
        }

        // Sagittal 高
        int SagittalHeight_;
        public int SagittalHeight
        {
            set
            {
                SagittalHeight_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SagittalHeight_"));
                }
            }
            get
            {
                return SagittalHeight_;
            }
        }

        // Sagittal 总层数
        int SagittalTotalSliceNum_;
        public int SagittalTotalSliceNum
        {
            set
            {
                SagittalTotalSliceNum_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SagittalTotalSliceNum_"));
                }
            }
            get
            {
                return SagittalTotalSliceNum_;
            }
        }

        // Sagittal 当前层数,从1开始
        int SagittalNowSliceNum_;
        public int SagittalNowSliceNum
        {
            set
            {
                SagittalNowSliceNum_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SagittalNowSliceNum_"));
                }
            }
            get
            {
                return SagittalNowSliceNum_;
            }
        }

        // Phantom分辨率
        float ResolutionX_;
        public float ResolutionX
        {
            set
            {
                ResolutionX_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ResolutionX_"));
                }
            }
            get
            {
                return ResolutionX_;
            }
        }
        float ResolutionY_;
        public float ResolutionY
        {
            set
            {
                ResolutionY_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ResolutionY_"));
                }
            }
            get
            {
                return ResolutionY_;
            }
        }
        float ResolutionZ_;
        public float ResolutionZ
        {
            set
            {
                ResolutionZ_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ResolutionZ_"));
                }
            }
            get
            {
                return ResolutionZ_;
            }
        }

        /// <summary>
        /// 初始默认构造函数
        /// </summary>
        public CrossSection()
        {
            TransverseImg = null;
            FrontalImg = null;
            SagittalImg = null;
            SketchInATranseverseSlice = null;
            SketchInAFrontalSlice = null;
            SketchInASagittalSlice = null;
            PhantomMatrix3D = null;
            TransverseWidth = 0;
            TransverseHeight = 0;
            TransverseTotalSliceNum = 0;
            TransverseNowSliceNum = 0;
            FrontalWidth = 0;
            FrontalHeight = 0;
            FrontalTotalSliceNum = 0;
            FrontalNowSliceNum = 0;
            SagittalWidth = 0;
            SagittalHeight = 0;
            SagittalTotalSliceNum = 0;
            SagittalNowSliceNum = 0;
            ResolutionX = 0;
            ResolutionY = 0;
            ResolutionZ = 0;
        }

        public CrossSection(Phantom nowPhantom)
        {
            if (nowPhantom != null)
            {
                this.nowPhantom = nowPhantom;

                ResolutionX = this.nowPhantom.RepeatStructureInAPhantom.ResolutionX;
                ResolutionY = this.nowPhantom.RepeatStructureInAPhantom.ResolutionY;
                ResolutionZ = this.nowPhantom.RepeatStructureInAPhantom.ResolutionZ;

                TransverseWidth = nowPhantom.RepeatStructureInAPhantom.DimX;
                TransverseHeight = nowPhantom.RepeatStructureInAPhantom.DimY;
                TransverseTotalSliceNum = nowPhantom.RepeatStructureInAPhantom.DimZ;
                TransverseNowSliceNum = nowPhantom.RepeatStructureInAPhantom.DimZ / 2;

                FrontalWidth = nowPhantom.RepeatStructureInAPhantom.DimX;
                FrontalHeight = nowPhantom.RepeatStructureInAPhantom.DimZ;
                FrontalTotalSliceNum = nowPhantom.RepeatStructureInAPhantom.DimY;
                FrontalNowSliceNum = nowPhantom.RepeatStructureInAPhantom.DimY / 2;

                SagittalWidth = nowPhantom.RepeatStructureInAPhantom.DimY;
                SagittalHeight = nowPhantom.RepeatStructureInAPhantom.DimZ;
                SagittalTotalSliceNum = nowPhantom.RepeatStructureInAPhantom.DimX;
                SagittalNowSliceNum = nowPhantom.RepeatStructureInAPhantom.DimX / 2;

                PhantomMatrix3D = nowPhantom.RepeatStructureInAPhantom.RepeatMatrix;

                RefreshImage("Transverse");
                RefreshImage("Frontal");
                RefreshImage("Sagittal");
            }
        }

        /// <summary>
        /// 更新截面图片
        /// </summary>
        /// <param name="workMode">可取值 "Transverse" "Frontal" "Sagittal"</param>
        public void RefreshImage(string workMode)
        {
            double dpiX; double dpiY;
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }

            if (workMode == "Transverse")
            {
                if (TransverseTotalSliceNum >= 1 && TransverseNowSliceNum >= 1 && TransverseNowSliceNum <= TransverseTotalSliceNum && PhantomMatrix3D != null)
                {
                    if (TransverseImg == null)
                    {
                        //WriteableBitmap参考资料 http://www.360doc.com/content/13/0424/23/11482448_280721226.shtml
                        TransverseImg = new WriteableBitmap(TransverseWidth, TransverseHeight, dpiX, dpiY, PixelFormats.Pbgra32, null);
                    }

                    // 提出截面
                    short[,] CrossMatrix = new short[TransverseHeight, TransverseWidth];
                    for (int i = 0; i < TransverseHeight; i++)
                    {
                        for (int j = 0; j < TransverseWidth; j++)
                        {
                            CrossMatrix[i, j] = PhantomMatrix3D[TransverseNowSliceNum - 1, i, j];
                        }
                    }

                    RefreshCrossSectionImage(TransverseImg, TransverseHeight, TransverseWidth, TransverseNowSliceNum, CrossMatrix);
                }
            }
            else if (workMode == "Frontal")
            {
                if (FrontalTotalSliceNum >= 1 && FrontalNowSliceNum >= 1 && FrontalNowSliceNum <= FrontalTotalSliceNum && PhantomMatrix3D != null)
                {
                    if (FrontalImg == null)
                    {
                        //WriteableBitmap参考资料 http://www.360doc.com/content/13/0424/23/11482448_280721226.shtml
                        FrontalImg = new WriteableBitmap(FrontalWidth, FrontalHeight, dpiX, dpiY, PixelFormats.Pbgra32, null);
                    }

                    // 提出截面
                    short[,] CrossMatrix = new short[FrontalHeight, FrontalWidth];
                    for (int i = 0; i < FrontalHeight; i++)
                    {
                        for (int j = 0; j < FrontalWidth; j++)
                        {
                            CrossMatrix[i, j] = PhantomMatrix3D[i, FrontalNowSliceNum - 1, j];  // [FrontalNowSliceNum - 1, i, j];
                        }
                    }

                    RefreshCrossSectionImage(FrontalImg, FrontalHeight, FrontalWidth, FrontalNowSliceNum, CrossMatrix);
                }
            }
            else if (workMode == "Sagittal")
            {
                if (SagittalTotalSliceNum >= 1 && SagittalNowSliceNum >= 1 && SagittalNowSliceNum <= SagittalTotalSliceNum && PhantomMatrix3D != null)
                {
                    if (SagittalImg == null)
                    {
                        //WriteableBitmap参考资料 http://www.360doc.com/content/13/0424/23/11482448_280721226.shtml
                        SagittalImg = new WriteableBitmap(SagittalWidth, SagittalHeight, dpiX, dpiY, PixelFormats.Pbgra32, null);
                    }

                    // 提出截面
                    short[,] CrossMatrix = new short[SagittalHeight, SagittalWidth];
                    for (int i = 0; i < SagittalHeight; i++)
                    {
                        for (int j = 0; j < SagittalWidth; j++)
                        {
                            CrossMatrix[i, j] = PhantomMatrix3D[i, j, SagittalNowSliceNum - 1];  //[SagittalNowSliceNum - 1, i, j];
                        }
                    }

                    RefreshCrossSectionImage(SagittalImg, SagittalHeight, SagittalWidth, SagittalNowSliceNum, CrossMatrix);
                }
            }
        }

        void RefreshCrossSectionImage(WriteableBitmap CrossSectionImage, int height, int width, int sliceNum, short[,] CrossMatrix)
        {
            Cell[] cellCollectionInAPhantom = nowPhantom.CellsCollectionInAPhantom.AllCells;
            if (cellCollectionInAPhantom != null && CrossSectionImage != null)
            {
                byte[] arraylistforshow = new byte[height * width * 4];
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        arraylistforshow[i * CrossSectionImage.BackBufferStride + j * 4 + 0] =
                                cellCollectionInAPhantom[CrossMatrix[i, j]].CellColor.B;
                        arraylistforshow[i * CrossSectionImage.BackBufferStride + j * 4 + 1] =
                                cellCollectionInAPhantom[CrossMatrix[i, j]].CellColor.G;
                        arraylistforshow[i * CrossSectionImage.BackBufferStride + j * 4 + 2] =
                                cellCollectionInAPhantom[CrossMatrix[i, j]].CellColor.R;
                        arraylistforshow[i * CrossSectionImage.BackBufferStride + j * 4 + 3] =
                                cellCollectionInAPhantom[CrossMatrix[i, j]].CellColor.A;
                    }
                }

                Int32Rect myrect = new Int32Rect(0, 0, width, height);

                CrossSectionImage.WritePixels(myrect, arraylistforshow, CrossSectionImage.BackBufferStride, 0);
            }
        }

        /// <summary>
        /// 更新截面靶区信息
        /// </summary>
        /// <param name="workMode">可取值 "Transverse" "Frontal" "Sagittal"</param>
        public void RefreshSketch(string workMode)
        {
            int SliceNowNumber = -1;
            if (workMode == "Transverse")
            {
                SliceNowNumber = TransverseNowSliceNum;
                if (nowPhantom != null)
                {
                    SketchInATranseverseSlice = new SketchCollInASlice();
                    SketchInATranseverseSlice.sketchInfoColl = nowPhantom.SketchCollForAllTransverse[SliceNowNumber].sketchInfoColl;
                }
            }
            else if (workMode == "Frontal")
            {
                SliceNowNumber = FrontalNowSliceNum;
                if (nowPhantom != null)
                {
                    SketchInAFrontalSlice = new SketchCollInASlice();
                    SketchInAFrontalSlice.sketchInfoColl = nowPhantom.SketchCollForAllFrontal[SliceNowNumber].sketchInfoColl;
                }
            }
            else if (workMode == "Sagittal")
            {
                SliceNowNumber = SagittalNowSliceNum;
                if (nowPhantom != null)
                {
                    SketchInASagittalSlice = new SketchCollInASlice();
                    SketchInASagittalSlice.sketchInfoColl = nowPhantom.SketchCollForAllSagittal[SliceNowNumber].sketchInfoColl;
                }
            }
        }

    }

    public class ColorToSolidBrushConvert : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return new SolidColorBrush((Color)value);
            }
            else
            {
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CrossectionSectionMouseLocationOrganInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        double MouseLocationX_;
        public double MouseLocationX
        {
            set
            {
                MouseLocationX_ = value;
                if (null != PropertyChanged)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MouseLocationX_"));
                }
            }
            get
            {
                return MouseLocationX_;
            }
        }

        double MouseLocationY_;
        public double MouseLocationY
        {
            set
            {
                MouseLocationY_ = value;
                if (null != PropertyChanged)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MouseLocationY_"));
                }
            }
            get
            {
                return MouseLocationY_;
            }
        }

        string CrossSectionType_;
        public string CrossSectionType
        {
            set
            {
                CrossSectionType_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CrossSectionType_"));
                }
            }
            get
            {
                return CrossSectionType_;
            }
        }

        int OrganIndex_;
        public int OrganIndex
        {
            set
            {
                OrganIndex_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("OrganIndex_"));
                }
            }
            get
            {
                return OrganIndex_;
            }
        }

        int MaterialIndex_;
        public int MaterialIndex
        {
            set
            {
                MaterialIndex_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MaterialIndex_"));
                }
            }
            get
            {
                return MaterialIndex_;
            }
        }

        string OrganName_;
        public string OrganName
        {
            set
            {
                OrganName_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("OrganName_"));
                }
            }
            get
            {
                return OrganName_;
            }
        }

        Color OrganColor_;
        public Color OrganColor
        {
            set
            {
                OrganColor_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("OrganColor_"));
                }
            }
            get
            {
                return OrganColor_;
            }
        }

        double MaterialDensity_;
        public double MaterialDensity
        {
            set
            {
                MaterialDensity_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MaterialDensity_"));
                }
            }
            get
            {
                return MaterialDensity_;
            }
        }
    }

    class ForVectorIndexCompare : IComparable
    {
        public int index;
        public Vector3D vector;

        public int CompareTo(object obj)
        {
            return index.CompareTo(((ForVectorIndexCompare)obj).index);
        }
    }

    public class DrawingCanvas : Canvas
    {
        private List<Visual> visuals = new List<Visual>();
        protected override int VisualChildrenCount
        {
            get
            {
                return visuals.Count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visuals[index];
        }

        public void AddVisual(Visual visual)
        {
            visuals.Add(visual);

            base.AddLogicalChild(visual);
            base.AddVisualChild(visual);
        }

        public void DeleteVisual(Visual visual)
        {
            visuals.Remove(visual);

            base.RemoveLogicalChild(visual);
            base.RemoveVisualChild(visual);
        }
    }
}
