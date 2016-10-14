using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MCNPFileEditor.DataClassAndControl
{
    public class MathFunction
    {
        // 三维向量叉积
        static public Point3D vector3CrossProduct(Point3D vectorA, Point3D vectorB)
        {
            return new Point3D(vectorA.X * vectorB.Z - vectorB.X * vectorA.Z,
                vectorA.Z * vectorB.X - vectorB.X * vectorA.Z,
                vectorA.X * vectorB.Y - vectorB.Y * vectorA.X);
        }

        // 三维向量点积
        static public double vector3DotProduct(Point3D vectorA, Point3D vectorB)
        {
            return vectorA.X * vectorB.X + vectorA.Y * vectorB.Y + vectorA.Z * vectorB.Z;
        }

        // 三维向量二范数
        static public double vector3Norm2(Point3D vectorA)
        {
            return Math.Sqrt(vector3DotProduct(vectorA, vectorA));
        }

        /// <summary>
        /// 线扫描，从多边形得出多边形内部的点
        /// 线扫描算法 参考 http://blog.csdn.net/orbit/article/details/7368996
        /// 相关算法 http://blog.csdn.net/orbit/article/details/7082678
        /// </summary>
        /// <param name="py">输入的多边形，坐标在([0, width], [0, height])之内(可能会略超出)</param>
        /// <param name="width">输出Mask的宽度</param>
        /// <param name="height">输出Mask的高度</param>
        /// <returns>Mask，在多变形内部的点置为true，外部的点为false</returns>
        public static bool[,] SketchMask(Polygon py, int width, int height)
        {
            if (py == null)
            {
                return null;
            }
            else
            {
                List<Point> Points = new List<Point>();
                foreach (var item in py.Points)
                {
                    Points.Add(item);
                }

                return MathFunction.SketchMask(Points, width, height);
            }
        }

        public static bool[,] SketchMask(List<Point> Points, int width, int height)
        {
            if (Points == null)
            {
                return null;
            }
            else
            {
                /////////Only for TEST Should remove
                // Points[1] = new Point(Points[1].X, (int)Points[1].Y + 0.5);
                /////////
                bool[,] SketchMask = new bool[height, width];

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        SketchMask[i, j] = false;
                    }
                }

                // 储存退化的边的索引 List<int>
                List<int> DegenerateOrPallelEdgeIndexColl = new List<int>();

                // 剔除退化成一个点的边
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    if (Points[i].X == Points[i + 1].X && Points[i].Y == Points[i + 1].Y)
                    {
                        Points.RemoveAt(i);
                        i--;
                    }
                }

                // 合并连续的平行直线
                for (int i = 0; i < Points.Count - 2; i++)
                {
                    if (((Points[i + 2].Y - Points[i + 1].Y) / (Points[i + 2].X - Points[i + 1].X))
                        .CompareTo(((Points[i + 1].Y - Points[i].Y) / (Points[i + 1].X - Points[i].X))) == 0)
                    {
                        Points.RemoveAt(i + 1);
                        i--;
                    }
                }
                if (((Points[0].Y - Points[Points.Count - 1].Y) / (Points[0].X - Points[Points.Count - 1].X))
                        .CompareTo(((Points[Points.Count - 1].Y - Points[Points.Count - 2].Y) / (Points[Points.Count - 1].X - Points[Points.Count - 2].X))) == 0)
                {
                    Points.RemoveAt(Points.Count - 1);
                }
                if (((Points[1].Y - Points[0].Y) / (Points[1].X - Points[0].X))
                        .CompareTo(((Points[0].Y - Points[Points.Count - 1].Y) / (Points[0].X - Points[Points.Count - 1].X))) == 0)
                {
                    Points.RemoveAt(Points.Count - 1);
                }

                // 不是多边形了，已经
                if (Points.Count < 3)
                {
                    return null;
                }

                // 储存每个扫描线的活动边表AET
                // 扫描线的个数和height数目是相等的
                // 扫描线的Y坐标为索引加上0.5(取像素的中心点)
                List<tagEdge>[] AET = new List<tagEdge>[height];
                for (int i = 0; i < height; i++)
                {
                    AET[i] = new List<tagEdge>();
                }

                //////////////////////////////////////////////////
                // 当前处理的多边形不会出现连续的平行直线和退化成点的边
                for (int i = 0; i < Points.Count; i++)
                {
                    // 当前直线
                    // Point1 Point2 Point3 Point4
                    // Point4只用在Point2 Point3为平行于X轴的情况
                    Point Point1 = Points[i];
                    Point Point2;
                    Point Point3;
                    Point Point4;
                    if (i + 1 == Points.Count)
                    {
                        Point2 = Points[0];
                        Point3 = Points[1];
                        Point4 = Points[2];
                    }
                    else if (i + 2 == Points.Count)
                    {
                        Point2 = Points[Points.Count - 1];
                        Point3 = Points[0];
                        Point4 = Points[1];
                    }
                    else if (i + 3 == Points.Count)
                    {
                        Point2 = Points[Points.Count - 2];
                        Point3 = Points[Points.Count - 1];
                        Point4 = Points[0];
                    }
                    else
                    {
                        Point2 = Points[i + 1];
                        Point3 = Points[i + 2];
                        Point4 = Points[i + 3];
                    }

                    // 剔除平行边的情况
                    if (Point2.Y.CompareTo(Point1.Y) == 0)
                    {
                        DegenerateOrPallelEdgeIndexColl.Add(i);
                        continue;
                    }
                    else
                    {
                        // 扫描线的Y值等于j
                        int SecondBound;
                        int FirstBound;
                        if (Point2.Y >= Point1.Y)
                        {
                            SecondBound = (int)Math.Floor(Point2.Y - 0.5);
                            FirstBound = (int)Math.Ceiling(Point1.Y - 0.5);
                        }
                        else
                        {
                            SecondBound = (int)Math.Ceiling(Point2.Y - 0.5);
                            FirstBound = (int)Math.Floor(Point1.Y - 0.5);
                        }

                        // 和扫描线没有交点的直线，直接跳过
                        if ((int)Math.Max(Point2.Y - 0.5, Point1.Y - 0.5) < ((int)Math.Min(Point2.Y - 0.5, Point1.Y - 0.5) + 1))
                        {
                            continue;
                        }

                        int lowerone = Math.Min(FirstBound, SecondBound);
                        int biggerone = Math.Max(FirstBound, SecondBound);

                        tagEdge firstTagEdge = new tagEdge();
                        firstTagEdge.dx = (Point2.Y - Point1.Y) / (Point2.X - Point1.X);
                        firstTagEdge.xi = Point2.X - (Point2.Y - lowerone - 0.5) * (Point2.X - Point1.X) / (Point2.Y - Point1.Y);

                        switch (JudgeVectorType(Point1, Point2, Point3))
                        {
                            case VectorType.NormalVector:
                                lowerone = Math.Min(FirstBound, SecondBound);
                                biggerone = Math.Max(FirstBound, SecondBound);
                                AET[lowerone].Add(firstTagEdge);
                                for (lowerone++; lowerone <= biggerone; lowerone++)
                                {
                                    // 水平扫描线的坐标 lowerBound + 0.5
                                    AET[lowerone].Add(new tagEdge()
                                    {
                                        dx = firstTagEdge.dx,  // == AET[lowerBound - 1].Last().dx
                                        xi = AET[lowerone - 1].Last().xi + 1 / firstTagEdge.dx,
                                    });
                                }

                                firstTagEdge = null;
                                break;
                            case VectorType.LeftOrRightVector:
                                if (FirstBound > SecondBound)
                                {
                                    SecondBound++;
                                }
                                else
                                {
                                    SecondBound--;
                                }
                                lowerone = Math.Min(FirstBound, SecondBound);
                                biggerone = Math.Max(FirstBound, SecondBound);
                                AET[lowerone].Add(firstTagEdge);
                                for (lowerone++; lowerone <= biggerone; lowerone++)
                                {
                                    // 水平扫描线的坐标 lowerBound + 0.5
                                    AET[lowerone].Add(new tagEdge()
                                    {
                                        dx = firstTagEdge.dx,  // == AET[lowerBound - 1].Last().dx
                                        xi = AET[lowerone - 1].Last().xi + 1 / firstTagEdge.dx,
                                    });
                                }

                                firstTagEdge = null;
                                break;
                            case VectorType.TopOrBottomVector:
                                lowerone = Math.Min(FirstBound, SecondBound);
                                biggerone = Math.Max(FirstBound, SecondBound);
                                AET[lowerone].Add(firstTagEdge);
                                for (lowerone++; lowerone <= biggerone; lowerone++)
                                {
                                    // 水平扫描线的坐标 lowerBound + 0.5
                                    AET[lowerone].Add(new tagEdge()
                                    {
                                        dx = firstTagEdge.dx,  // == AET[lowerBound - 1].Last().dx
                                        xi = AET[lowerone - 1].Last().xi + 1 / firstTagEdge.dx,
                                    });
                                }

                                firstTagEdge = null;
                                break;
                            case VectorType.OtherVector:
                                // 下面一条线段是平行线
                                // 在下面一天直线向上折，不用特殊处理，向下折需要upperBound - 1
                                if (Point4.Y > Point3.Y) // 向下折
                                {
                                    if (FirstBound > SecondBound)
                                    {
                                        SecondBound++;
                                    }
                                    else
                                    {
                                        SecondBound--;
                                    }
                                }
                                lowerone = Math.Min(FirstBound, SecondBound);
                                biggerone = Math.Max(FirstBound, SecondBound);
                                AET[lowerone].Add(firstTagEdge);
                                for (lowerone++; lowerone <= biggerone; lowerone++)
                                {
                                    // 水平扫描线的坐标 lowerBound + 0.5
                                    AET[lowerone].Add(new tagEdge()
                                    {
                                        dx = firstTagEdge.dx,  // == AET[lowerBound - 1].Last().dx
                                        xi = AET[lowerone - 1].Last().xi + 1 / firstTagEdge.dx,
                                    });
                                }

                                firstTagEdge = null;
                                break;
                            default:
                                break;
                        }
                    }
                }

                // 每条扫描线和多边形的交点按照xi的值从小到大排列
                for (int i = 0; i < height; i++)
                {
                    if (null != AET[i])
                    {
                        AET[i].Sort();
                    }
                }

                // xi排序之后按照一一配对的方式填充点(相应矩阵的点置为true)
                for (int i = 0; i < height; i++)
                {
                    if (null != AET[i])
                    {
                        if (AET[i].Count % 2 != 0)
                        {
                            throw new Exception("交点个数应该是偶数个!");
                        }
                        else
                        {
                            for (int j = 0; j < AET[i].Count / 2; j++)
                            {
                                // 避免像素扩大化
                                for (int k = (int)(AET[i][j * 2].xi + 0.5); k <= (int)(AET[i][j * 2 + 1].xi - 0.5); k++)
                                {
                                    if (k >= 0 && k < width) // 点的范围限制在在0-width 0-height区域之内
                                    {
                                        SketchMask[i, k] = true;
                                    }
                                }
                            }
                        }
                    }
                }

                // 平行的边界绘制
                // foreach (var item in DegenerateOrPallelEdgeIndexColl)
                // {
                //     Point lineBegin = Points[item];
                //     Point lineEnd = Points[item + 1 == Points.Count ? 0 : (item + 1)];
                //     int beginIndex = Math.Min((int)lineBegin.X, (int)lineEnd.X);
                //     int endIndex = Math.Max((int)lineBegin.X, (int)lineEnd.X);
                //     int lineIndex = (int)lineBegin.Y;
                // 
                //     // 避免像素扩大化
                //     for (int k = 1 + beginIndex; k <= endIndex; k++)
                //     {
                // 
                //         if (k >= 0 && k < width && lineIndex >= 0 && lineIndex < height) // 点的范围限制在在0-width 0-height区域之内
                //         {
                //             SketchMask[lineIndex, k] = true;
                //         }
                //     }
                // }

                return SketchMask;
            }
        }

        class tagEdge : IComparable
        {
            public double xi;
            public double dx;
            // public double ymax;

            public int CompareTo(object obj)
            {
                return xi.CompareTo(((tagEdge)obj).xi);
            }
        }

        // 顶点和扫描线可能出现的异常情况，使得交点不为偶数个
        // 参见http://blog.csdn.net/orbit/article/details/7368996
        private enum VectorType
        {
            NormalVector,
            LeftOrRightVector,
            TopOrBottomVector,
            OtherVector,
        }

        /// <summary>
        /// 判断多边形边的特殊情况
        /// 使用线段的终点做判断
        /// 多边形为有序存储
        /// 不能判断平行线或者是退化的边
        /// </summary>
        /// <param name="pointBegin">要判断边的起点</param>
        /// <param name="pointMiddle">要判断边的终点和相邻的下一条边的起点</param>
        /// <param name="pointEnd">相邻的下一条边的终点</param>
        /// <returns>要判断边的终点的顶点类型</returns>
        static private VectorType JudgeVectorType(Point pointBegin, Point pointMiddle, Point pointEnd)
        {
            if (pointMiddle.Y.CompareTo((int)pointMiddle.Y + 0.5) == 0)
            {
                if ((pointBegin.Y.CompareTo(pointMiddle.Y) > 0 && pointEnd.Y.CompareTo(pointMiddle.Y) < 0) ||
                    (pointBegin.Y.CompareTo(pointMiddle.Y) < 0 && pointEnd.Y.CompareTo(pointMiddle.Y) > 0))
                {
                    return VectorType.LeftOrRightVector;
                }
                else if ((pointBegin.Y.CompareTo(pointMiddle.Y) > 0 && pointEnd.Y.CompareTo(pointMiddle.Y) > 0) ||
                    (pointBegin.Y.CompareTo(pointMiddle.Y) < 0 && pointEnd.Y.CompareTo(pointMiddle.Y) < 0))
                {
                    return VectorType.TopOrBottomVector;
                }
                else
                {
                    // 下一条边为平行或者是退化的边
                    return VectorType.OtherVector;
                }
            }
            else
                return VectorType.NormalVector;
        }

        /// <summary>
        /// 计算一副图片矩阵的拉普拉斯算子
        /// </summary>
        /// <param name="pixelMatrix">图像矩阵，对于彩色图片一个像素存在一行，占用连续的若干位置</param>
        /// <param name="imgColorType">图片类型是ARGB或者RGB或者GRAY</param>
        /// <param name="imageHeight">图片的高度</param>
        /// <param name="imageWidth">图片的宽度，彩色图片的一个像素包含若干分量，他们加起来算作一个像素，占用一个宽度</param>
        /// <param name="kernelSize">kernel核的宽度</param>
        /// <returns>计算完成的算子，与输入参数pixelMatrix维度相同</returns>
        static public double[,] CalLaplacianKernel(byte[,] pixelMatrix, int imageHeight, int imageWidth, int kernelSize = 5, string imgColorType = "GRAY")
        {
            if (imgColorType != "ARGB" && imgColorType != "RGB" && imgColorType != "GRAY" && kernelSize !=5 && kernelSize != 9 && imageHeight <= 0 && imageWidth <= 0 || pixelMatrix == null)
            {
                return null;
            }

            // 记录每个像素占用多少位移
            int pixelwidth = 0;
            if (imgColorType == "ARGB")
            {
                pixelwidth = 4;
            }
            else if (imgColorType == "RGB")
            {
                pixelwidth = 3;
            }
            else
            {
                pixelwidth = 1;
            }

            // 拉普拉斯算子
            double[,] LapAns = new double[imageHeight, imageWidth * pixelwidth];

            for (int i = 0; i < imageHeight; i++)
            {
                for (int j = 0; j < imageWidth; j++)
                {
                    int neigthborCount = 0;
                    double[] neighborValueSum = new double[pixelwidth];
                    for (int kk = 0; kk < pixelwidth; kk++)
                    {
                        neighborValueSum[kk] = 0;
                    }
                    if (kernelSize == 5)
                    {
                        for (int mm = -1; mm <= 1; mm++)
                        {
                            if (mm != 0)
                            {
                                if ((i - mm) >= 0 && (i - mm) <= imageHeight)
                                {
                                    neigthborCount++;
                                    for (int kk = 0; kk < pixelwidth; kk++)
                                    {
                                        neighborValueSum[kk] += pixelMatrix[i - mm, j * pixelwidth + kk];
                                    }
                                }
                                if ((j - mm) >= 0 && (j - mm) <= imageWidth)
                                {
                                    neigthborCount++;
                                    for (int kk = 0; kk < pixelwidth; kk++)
                                    {
                                        neighborValueSum[kk] += pixelMatrix[i, (j - mm) * pixelwidth + kk];
                                    }
                                }
                            }
                        }
                    }
                    else if (kernelSize == 9)
                    {
                        for (int mm = -1; mm <= 1; mm++)
                        {
                            for (int nn = -1; nn <= 1; nn++)
                            {
                                if ((i - mm) >= 0 && (i - mm) <= imageHeight && (j - nn) >= 0 && (j - nn) <= imageWidth)
                                {
                                    neigthborCount++;
                                    for (int kk = 0; kk < pixelwidth; kk++)
                                    {
                                        neighborValueSum[kk] += pixelMatrix[i - mm, (j - nn) * pixelwidth + kk];
                                    }
                                }
                            }
                        }
                    }

                    for (int kk = 0; kk < pixelwidth; kk++)
                    {
                        LapAns[i, j * pixelwidth + kk] = (neighborValueSum[kk] - neigthborCount * pixelMatrix[i, j * pixelwidth]);
                    }
                }
            }

            return LapAns;
        }

        static public double[,] ImgLapConvolution(double[,] LapAns, byte[,] pixelMatrix, int imageHeight, int imageWidth, string imgColorType = "GRAY")
        {
            if (imgColorType != "ARGB" && imgColorType != "RGB" && imgColorType != "GRAY" && imageHeight <= 0 && imageWidth <= 0 || pixelMatrix == null || LapAns == null)
            {
                return null;
            }

            // 记录每个像素占用多少位移
            int pixelwidth = 0;
            if (imgColorType == "ARGB")
            {
                pixelwidth = 4;
            }
            else if (imgColorType == "RGB")
            {
                pixelwidth = 3;
            }
            else
            {
                pixelwidth = 1;
            }

            // 卷积的结果
            double[,] ConvolutionAns = new double[imageHeight, imageWidth * pixelwidth];

            // 卷积叠加
            for (int i = 0; i < imageHeight; i++)
            {
                for (int j = 0; j < imageWidth; j++)
                {
                    double[] neighborValueSum = new double[pixelwidth];
                    for (int mm = -1; mm <= 1; mm++)
                    {
                        for (int nn = -1; nn <= 1; nn++)
                        {
                            if ((i - mm) >= 0 && (i - mm) <= imageHeight && (j - nn) >= 0 && (j - nn) <= imageWidth)
                            {
                                for (int kk = 0; kk < pixelwidth; kk++)
                                {
                                    neighborValueSum[kk] += (pixelMatrix[i - mm, (j - nn) * pixelwidth + kk] * LapAns[i - mm, (j - nn) * pixelwidth + kk]);
                                }
                            }
                        }
                    }
                    for (int kk = 0; kk < pixelwidth; kk++)
                    {
                        ConvolutionAns[i, j * pixelwidth + kk] = neighborValueSum[kk];
                    }
                }
            }

            return ConvolutionAns;
        }

        static public short[,] CalLocalCostFZ(double[,] ConvolutionAns, int imageHeight, int imageWidth, string imgColorType = "GRAY")
        {
            if (imgColorType != "ARGB" && imgColorType != "RGB" && imgColorType != "GRAY" && imageHeight <= 0 && imageWidth <= 0 || ConvolutionAns == null)
            {
                return null;
            }
        
            // 记录每个像素占用多少位移
            int pixelwidth = 0;
            if (imgColorType == "ARGB")
            {
                pixelwidth = 4;
            }
            else if (imgColorType == "RGB")
            {
                pixelwidth = 3;
            }
            else
            {
                pixelwidth = 1;
            }
        
            short[,] LocalCostFZAns = new short[imageHeight, imageWidth * pixelwidth];
            for (int i = 0; i < imageHeight; i++) // 代价初始化为1
            {
                for (int j = 0; j < imageWidth; j++)
                {
                    for (int kk = 0; kk < pixelwidth; kk++)
                    {
                        LocalCostFZAns[i, j * pixelwidth + kk] = 1;
                    }
                }
            }

            for (int i = 0; i < imageHeight - 1; i++) // 代价初始化为1
            {
                for (int j = 0; j < imageWidth - 1; j++)
                {
                    for (int kk = 0; kk < pixelwidth; kk++)
                    {
                        double valueNow = ConvolutionAns[i, j * pixelwidth + kk];
                        if (Math.Abs(valueNow) < 0.1) // 足够小
                        {
                            LocalCostFZAns[i, j * pixelwidth + kk] = 0;
                            continue;
                        }
                        else
                        {
                            double valueNextRight = ConvolutionAns[i, (j + 1) * pixelwidth + kk];
                            double valueNextBelow = ConvolutionAns[i + 1, j * pixelwidth + kk];
                            if (valueNextRight * valueNow < 0)
                            {
                                if (Math.Abs(valueNow) < Math.Abs(valueNextRight))
                                {
                                    LocalCostFZAns[i, j * pixelwidth + kk] = 0;
                                }
                                else
                                {
                                    LocalCostFZAns[i, (j + 1) * pixelwidth + kk] = 0;
                                }
                            }
                            if (valueNextBelow * valueNow < 0)
                            {
                                if (Math.Abs(valueNow) < Math.Abs(valueNextBelow))
                                {
                                    LocalCostFZAns[i, j * pixelwidth + kk] = 0;
                                }
                                else
                                {
                                    LocalCostFZAns[i + 1, j * pixelwidth + kk] = 0;
                                }
                            }
                        }
                    }
                }
            }

            return LocalCostFZAns;
        }
    }
}
