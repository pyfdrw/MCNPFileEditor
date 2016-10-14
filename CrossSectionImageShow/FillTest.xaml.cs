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
    /// FillTest.xaml 的交互逻辑
    /// </summary>
    public partial class FillTest : Window
    {
        public SketchCollInASlice SketchCollForTheTransverse;
        public CrossSection selectedCrossSection;

        bool isInMeasureMode = false;

        public FillTest()
        {
            InitializeComponent();
            
        }

        void SetInitialParameters()
        {
            fillCheckCanvas.Height = selectedCrossSection.TransverseHeight;
            fillCheckCanvas.Width = selectedCrossSection.TransverseWidth;
            fillCheckImage.Height = selectedCrossSection.TransverseHeight;
            fillCheckImage.Width = selectedCrossSection.TransverseWidth;
            gridLinePath.Height = selectedCrossSection.TransverseHeight;
            gridLinePath.Width = selectedCrossSection.TransverseWidth;

            fillCheckCanvas.AddHandler(UIElement.MouseUpEvent, new MouseButtonEventHandler(img_MouseLeftButtonUp), true);
        }

        void GenPic()
        {
            // 添加边界线，颜色为深色
            // 生成和添加像素点，颜色为浅色
            if (SketchCollForTheTransverse != null)
            {
                // 先生成像素点
                bool[,] SketchMask = new bool[selectedCrossSection.TransverseHeight, selectedCrossSection.TransverseWidth];
                for (int i = 0; i < selectedCrossSection.TransverseHeight; i++)
                {
                    for (int j = 0; j < selectedCrossSection.TransverseWidth; j++)
                    {
                        SketchMask[i, j] = false;
                    }
                }

                foreach (var item in SketchCollForTheTransverse.sketchInfoColl)
                {
                    Polygon oneSketch = new Polygon();
                    foreach (var onepoint in item.vertexColl)
                    {
                        oneSketch.Points.Add(onepoint);
                    }
                    oneSketch.Stroke = Brushes.Black;
                    oneSketch.StrokeThickness = 0.1;
                    oneSketch.SnapsToDevicePixels = true;
                    oneSketch.StrokeEndLineCap = PenLineCap.Round;
                    // oneSketch.Fill = new SolidColorBrush(Color.FromArgb(50, 200,0,0));

                    fillCheckCanvas.Children.Add(oneSketch);

                    bool[,] SketchTmp = MCNPFileEditor.DataClassAndControl.MathFunction.SketchMask(oneSketch, selectedCrossSection.TransverseWidth, selectedCrossSection.TransverseHeight);

                    for (int i = 0; i < selectedCrossSection.TransverseHeight; i++)
                    {
                        for (int j = 0; j < selectedCrossSection.TransverseWidth; j++)
                        {
                            SketchMask[i, j] = SketchMask[i, j] | SketchTmp[i, j];
                        }
                    }
                }

                ///////////////////
                double dpiX; double dpiY;
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpiX = graphics.DpiX;
                    dpiY = graphics.DpiY;
                }

                WriteableBitmap TransverseImg = new WriteableBitmap(selectedCrossSection.TransverseWidth, selectedCrossSection.TransverseHeight, dpiX, dpiY, PixelFormats.Pbgra32, null);


                byte[] arraylistforshow = new byte[selectedCrossSection.TransverseWidth * selectedCrossSection.TransverseHeight * 4];
                for (int i = 0; i < selectedCrossSection.TransverseHeight; i++)
                {
                    for (int j = 0; j < selectedCrossSection.TransverseWidth; j++)
                    {
                        // 描点区域着色，其余区域透明
                        if (SketchMask[i, j])
                        {
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 0] = 0;
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 1] = 0;
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 2] = 255;
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 3] = 126;
                        }
                        else
                        {
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 0] = 255;
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 1] = 255;
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 2] = 255;
                            arraylistforshow[i * TransverseImg.BackBufferStride + j * 4 + 3] = 0;
                        }
                    }
                }

                arraylistforshow[0] = 0; arraylistforshow[1] = 0; arraylistforshow[2] = 0; arraylistforshow[3] = 255;
                arraylistforshow[4] = 255; arraylistforshow[5] = 0; arraylistforshow[6] = 0; arraylistforshow[7] = 255;
                arraylistforshow[8] = 0; arraylistforshow[9] = 255; arraylistforshow[10] = 0; arraylistforshow[11] = 255;
                arraylistforshow[16] = 0; arraylistforshow[17] = 0; arraylistforshow[18] = 255; arraylistforshow[19] = 255;


                Int32Rect myrect = new Int32Rect(0, 0, selectedCrossSection.TransverseWidth, selectedCrossSection.TransverseHeight);

                TransverseImg.WritePixels(myrect, arraylistforshow, TransverseImg.BackBufferStride, 0);

                fillCheckImage.Source = TransverseImg;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            SetInitialParameters();
            GenPic();
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            isInMeasureMode = !isInMeasureMode;
        }

        bool isFirstPoint = true;
        Point lineBegin;
        Point lineEnd;
        private void img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isInMeasureMode)
            {
                coord.Content = e.GetPosition(fillCheckImage).X.ToString() + ", " + e.GetPosition(fillCheckImage).Y;

                if (isFirstPoint)
                {
                    lineBegin = e.GetPosition(fillCheckImage);
                    isFirstPoint = false;
                }
                else
                {
                    isFirstPoint = true;
                    lineEnd = e.GetPosition(fillCheckImage);
                    XDis.Content = Math.Abs(lineBegin.X - lineEnd.X).ToString();
                    YDis.Content = Math.Abs(lineBegin.Y - lineEnd.Y).ToString();
                    Dis.Content = Math.Sqrt((lineBegin.X - lineEnd.X) * (lineBegin.X - lineEnd.X) + (lineBegin.Y - lineEnd.Y) * (lineBegin.Y - lineEnd.Y));

                    fillCheckCanvas.Children.Add(new Line()
                    {
                        X1 = lineBegin.X,
                        X2 = lineEnd.X,
                        Y1 = lineBegin.Y,
                        Y2 = lineEnd.Y,
                        StrokeThickness = 0.1,
                        Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                    });
                }
            }
        }

        private void button_Copy1_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_Copy2_Click(object sender, RoutedEventArgs e)
        {
            gridGroup.Children.Clear();
            Point StartPoint = new Point(0, 0);
            Point EndPoint = new Point(selectedCrossSection.TransverseWidth, 0);
            for (int i = 0; i < selectedCrossSection.TransverseHeight; i++)
            {
                gridGroup.Children.Add(new LineGeometry()
                {
                    StartPoint = new Point(StartPoint.X, StartPoint.Y + i),
                    EndPoint = new Point(EndPoint.X, EndPoint.Y + i),
                });
            }
            EndPoint = new Point(0, selectedCrossSection.TransverseHeight);
            for (int i = 0; i < selectedCrossSection.TransverseWidth; i++)
            {
                gridGroup.Children.Add(new LineGeometry()
                {
                    StartPoint = new Point(StartPoint.X + i, StartPoint.Y),
                    EndPoint = new Point(EndPoint.X + i, EndPoint.Y),
                });
            }
        }

        private void button_Copy3_Click(object sender, RoutedEventArgs e)
        {
            gridGroup.Children.Clear();
            Point StartPoint = new Point(0, 0);
            Point EndPoint = new Point(selectedCrossSection.TransverseWidth, 0);
            for (int i = 0; i < selectedCrossSection.TransverseHeight; i++)
            {
                gridGroup.Children.Add(new LineGeometry()
                {
                    StartPoint = new Point(StartPoint.X, StartPoint.Y + i + 0.5),
                    EndPoint = new Point(EndPoint.X, EndPoint.Y + i + 0.5),
                });
            }
            EndPoint = new Point(0, selectedCrossSection.TransverseHeight);
            for (int i = 0; i < selectedCrossSection.TransverseWidth; i++)
            {
                gridGroup.Children.Add(new LineGeometry()
                {
                    StartPoint = new Point(StartPoint.X + i + 0.5, StartPoint.Y),
                    EndPoint = new Point(EndPoint.X + i + 0.5, EndPoint.Y),
                });
            }
        }
    }
}
