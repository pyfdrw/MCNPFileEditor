using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using ClipperLib;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

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
                            // TODO 进行错误检查
                            // throw new Exception("交点个数应该是偶数个!");
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

        /// <summary>  
        /// 判断点是否在多边形内.  
        /// ----------原理----------  
        /// 注意到如果从P作水平向左的射线的话，如果P在多边形内部，那么这条射线与多边形的交点必为奇数，  
        /// 如果P在多边形外部，则交点个数必为偶数(0也在内)。  
        /// </summary>  
        /// <param name="checkPoint">要判断的点</param>  
        /// <param name="polygonPoints">多边形的顶点</param>  
        /// <returns></returns>  
        public static bool IsInPolygon(Point checkPoint, List<Point> polygonPoints)
        {
            bool inside = false;
            int pointCount = polygonPoints.Count;
            Point p1, p2;
            for (int i = 0, j = pointCount - 1; i < pointCount; j = i, i++)//第一个点和最后一个点作为第一条线，之后是第一个点和第二个点作为第二条线，之后是第二个点与第三个点，第三个点与第四个点...  
            {
                p1 = polygonPoints[i];
                p2 = polygonPoints[j];
                if (checkPoint.Y < p2.Y)
                {//p2在射线之上  
                    if (p1.Y <= checkPoint.Y)
                    {//p1正好在射线中或者射线下方  
                        if ((checkPoint.Y - p1.Y) * (p2.X - p1.X) > (checkPoint.X - p1.X) * (p2.Y - p1.Y))//斜率判断,在P1和P2之间且在P1P2右侧  
                        {
                            //射线与多边形交点为奇数时则在多边形之内，若为偶数个交点时则在多边形之外。  
                            //由于inside初始值为false，即交点数为零。所以当有第一个交点时，则必为奇数，则在内部，此时为inside=(!inside)  
                            //所以当有第二个交点时，则必为偶数，则在外部，此时为inside=(!inside)  
                            inside = (!inside);
                        }
                    }
                }
                else if (checkPoint.Y < p1.Y)
                {
                    //p2正好在射线中或者在射线下方，p1在射线上  
                    if ((checkPoint.Y - p1.Y) * (p2.X - p1.X) < (checkPoint.X - p1.X) * (p2.Y - p1.Y))//斜率判断,在P1和P2之间且在P1P2右侧  
                    {
                        inside = (!inside);
                    }
                }
            }
            return inside;
        }

        /// <summary>
        /// 判断线段与多边形是否有交点
        /// </summary>
        /// <param name="checkPointA">线段起点</param>
        /// <param name="CheckPointB">线段终点</param>
        /// <param name="polygonPoints">多边形</param>
        /// <returns></returns>
        public static bool IsSegmentInteractPolygon(Point checkPointA, Point checkPointB, List<Point> polygonPoints)
        {
            // 线段一头在多边形内部，一头在多边形外部
            if (IsInPolygon(checkPointA, polygonPoints) && !IsInPolygon(checkPointB, polygonPoints)
                || !IsInPolygon(checkPointA, polygonPoints) && IsInPolygon(checkPointB, polygonPoints))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 计算两个线段的交点
        /// </summary>
        /// <param name="checkPointA">1起点</param>
        /// <param name="checkPointB">1终点</param>
        /// <param name="checkPointC">2起点</param>
        /// <param name="checkPointD">2终点</param>
        /// <returns>交点坐标</returns>
        public static Point CalCrossPoint(Point checkPointA, Point checkPointB, Point checkPointC, Point checkPointD)
        {
            double lambdaValue = ((checkPointC.X - checkPointA.X) -
                                  (checkPointB.X - checkPointA.X)/(checkPointB.Y - checkPointA.Y) * (checkPointC.Y - checkPointB.Y))/
                                 ((checkPointB.X - checkPointA.X)/(checkPointB.Y - checkPointA.Y)*
                                  (checkPointD.Y - checkPointC.Y) - (checkPointD.X - checkPointC.X));
            if (lambdaValue < 0 || lambdaValue > 1)
            {
                return new Point(-10000, -10000);
            }
            double tValue = ((checkPointC.X - checkPointA.X) + lambdaValue*(checkPointD.X - checkPointC.X))/
                            (checkPointB.X - checkPointA.X);
            if (tValue < 0 || tValue > 1)
            {
                return new Point(-10000, -10000);
            }
            else return new Point(
                checkPointA.X + tValue * (checkPointB.X - checkPointA.X),
                checkPointA.Y+ tValue * (checkPointB.Y - checkPointA.Y)
                );
        }

        /// <summary>
        /// 计算两个多边形经过bool运算后的多边形
        /// </summary>
        /// <param name="firstPolygon">第一个多边形</param>
        /// <param name="secondPolygon">第二个多边形</param>
        /// <param name="theClipType">多边形布尔运算的类型</param>
        /// <param name="scale">使用的库函数均为整数下的操作，浮点数需要乘以一个放大倍数然后去整，这个就是放大倍数</param>
        /// <returns>返回运算后的多边形组，注意是除以了放大倍数scale后的正确结果</returns>
        public static List<Polygon> CalPolygonsBoolOp(List<Polygon> firstPolygon, List<Polygon> secondPolygon, ClipType theClipType, float scale = 1000)
        {
            Paths subjs = new Paths();
            Paths clips = new Paths();

            foreach (Polygon polygon in firstPolygon)
            {
                Path subj = new Path();
                foreach (var point in polygon.Points)
                {
                    subj.Add(new IntPoint(point.X * scale, point.Y * scale));
                }
                subjs.Add(subj);
            }
            foreach (Polygon polygon in secondPolygon)
            {
                Path clip = new Path();
                foreach (Point point in polygon.Points)
                {
                    clip.Add(new IntPoint(point.X * scale, point.Y * scale));
                }
                clips.Add(clip);
            }

            Paths solution = new Paths();
            Clipper c = new Clipper();
            c.AddPaths(subjs, PolyType.ptSubject, true);
            c.AddPaths(clips, PolyType.ptClip, true);
            c.Execute(theClipType, solution);

            List<Polygon> calculatedPolygons = new List<Polygon>();
            foreach (Path intPoints in solution)
            {
                Polygon polygonTmp = new Polygon();
                foreach (IntPoint intPoint in intPoints)
                {
                    polygonTmp.Points.Add(new Point((double)intPoint.X / scale, (double)intPoint.Y / scale));
                }
                calculatedPolygons.Add(polygonTmp);
            }

            return calculatedPolygons;
        }

        /// <summary>
        /// 计算两个多边形经过bool运算后的多边形,需要保证firstsketchInfoColl与secondsketchInfoColl所有的SketchInfo有相同的cellIndex
        /// </summary>
        /// <param name="firstPolygon">第一个多边形</param>
        /// <param name="secondPolygon">第二个多边形</param>
        /// <param name="theClipType">多边形布尔运算的类型</param>
        /// <param name="scale">使用的库函数均为整数下的操作，浮点数需要乘以一个放大倍数然后去整，这个就是放大倍数</param>
        /// <returns>返回运算后的多边形组，注意是除以了放大倍数scale后的正确结果</returns>
        public static List<SketchInfo> CalPolygonsBoolOp(List<SketchInfo> firstsketchInfoColl, List<SketchInfo> secondsketchInfoColl, ClipType theClipType, int outCellIndex,  float scale = 1000)
        {
            Paths subjs = new Paths();
            Paths clips = new Paths();

            foreach (SketchInfo sketchInfo in firstsketchInfoColl)
            {
                Path subj = new Path();
                foreach (var point in sketchInfo.vertexColl)
                {
                    subj.Add(new IntPoint(point.X * scale, point.Y * scale));
                }
                subjs.Add(subj);
            }
            foreach (SketchInfo sketchInfo in secondsketchInfoColl)
            {
                Path clip = new Path();
                foreach (Point point in sketchInfo.vertexColl)
                {
                    clip.Add(new IntPoint(point.X * scale, point.Y * scale));
                }
                clips.Add(clip);
            }

            Paths solution = new Paths();
            Clipper c = new Clipper();
            c.AddPaths(subjs, PolyType.ptSubject, true);
            c.AddPaths(clips, PolyType.ptClip, true);
            c.Execute(theClipType, solution);

            List<SketchInfo> calculatedPolygons = new List<SketchInfo>();
            foreach (Path intPoints in solution)
            {
                SketchInfo polygonTmp = new SketchInfo(outCellIndex);
                foreach (IntPoint intPoint in intPoints)
                {
                    polygonTmp.vertexColl.Add(new Point((double)intPoint.X / scale, (double)intPoint.Y / scale));
                }
                calculatedPolygons.Add(polygonTmp);
            }

            return calculatedPolygons;
        }

        public static Polygon CalCirclePoints(Point center, double radius, int lineNum = 0)
        {
            if (lineNum == 0)
            {
                lineNum = (int)Math.Round(radius) * 100; // 1~100 2~200 3 ~300
            }
            Polygon calculatedPolygon = new Polygon();
            double angleFraction = Math.PI * 2 / lineNum;
            for (int i = 0; i < lineNum; i++)
            {
                double angleTmp = i * angleFraction ;
                calculatedPolygon.Points.Add(new Point(center.X + Math.Cos(angleTmp) * radius, center.Y + Math.Sin(angleTmp) * radius));
            }
            if (0 != calculatedPolygon.Points.Count)
            {
                return calculatedPolygon;
            }
            else
            {
                throw new Exception("生成圆形的多边形定点时出现未知错误");
            }
        }

        public static SketchInfo CalCirclePoints(Point center, double radius, int cellIndex, int lineNum = 0)
        {
            if (lineNum == 0)
            {
                lineNum = (int)Math.Round(radius) * 10; // 1~10 2~20 3 ~30
            }
            SketchInfo calculatedPolygon = new SketchInfo(cellIndex);
            double angleFraction = Math.PI * 2 / lineNum;
            for (int i = 0; i < lineNum; i++)
            {
                double angleTmp = i * angleFraction;
                calculatedPolygon.vertexColl.Add(new Point(center.X + Math.Cos(angleTmp) * radius, center.Y + Math.Sin(angleTmp) * radius));
            }
            if (0 != calculatedPolygon.vertexColl.Count)
            {
                return calculatedPolygon;
            }
            else
            {
                throw new Exception("生成圆形的多边形定点时出现未知错误");
            }
        }
    }

    
}
