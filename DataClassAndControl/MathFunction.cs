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
                bool[,] SketchMask = new bool[height, width];

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        SketchMask[i, j] = false;
                    }
                }

                // 储存退化的边的索引
                List<int> DegenerateOrPallelEdgeIndexColl = new List<int>();
             
                // 剔除退化成一个点的边
                for (int i = 0; i < py.Points.Count - 1; i++)
                {
                    if (py.Points[i].X == py.Points[i + 1].X && py.Points[i].Y == py.Points[i + 1].Y)
                    {
                        py.Points.RemoveAt(i);
                        i--;
                    }
                }

                // 合并连续的平行直线
                for (int i = 0; i < py.Points.Count - 2; i++)
                {
                    if (((py.Points[i + 2].Y - py.Points[i + 1].Y) / (py.Points[i + 2].X - py.Points[i + 1].X))
                        .CompareTo(((py.Points[i + 1].Y - py.Points[i].Y) / (py.Points[i + 1].X - py.Points[i].X))) == 0)
                    {
                        py.Points.RemoveAt(i + 1);
                        i--;
                    }
                }
                if (((py.Points[0].Y - py.Points[py.Points.Count - 1].Y) / (py.Points[0].X - py.Points[py.Points.Count - 1].X))
                        .CompareTo(((py.Points[py.Points.Count - 1].Y - py.Points[py.Points.Count - 2].Y) / (py.Points[py.Points.Count - 1].X - py.Points[py.Points.Count - 2].X))) == 0)
                {
                    py.Points.RemoveAt(py.Points.Count - 1);
                }
                if (((py.Points[1].Y - py.Points[0].Y) / (py.Points[1].X - py.Points[0].X))
                        .CompareTo(((py.Points[0].Y - py.Points[py.Points.Count - 1].Y) / (py.Points[0].X - py.Points[py.Points.Count - 1].X))) == 0)
                {
                    py.Points.RemoveAt(py.Points.Count - 1);
                }

                // 不是多边形了，已经
                if (py.Points.Count < 3)
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
                for (int i = 0; i < py.Points.Count; i++)
                {
                    // 当前直线
                    // Point1 Point2 Point3 Point4
                    // Point4只用在Point2 Point3为平行于X轴的情况
                    Point Point1 = py.Points[i];
                    Point Point2;
                    Point Point3;
                    Point Point4;
                    if (i + 1 == py.Points.Count)
                    {
                        Point2 = py.Points[0];
                        Point3 = py.Points[1];
                        Point4 = py.Points[2];
                    }
                    else if (i + 2 == py.Points.Count)
                    {
                        Point2 = py.Points[py.Points.Count - 1];
                        Point3 = py.Points[0];
                        Point4 = py.Points[1];
                    }
                    else if (i + 3 == py.Points.Count)
                    {
                        Point2 = py.Points[py.Points.Count - 2];
                        Point3 = py.Points[py.Points.Count - 1];
                        Point4 = py.Points[0];
                    }
                    else
                    {
                        Point2 = py.Points[py.Points.Count - 3];
                        Point3 = py.Points[py.Points.Count - 2];
                        Point4 = py.Points[py.Points.Count - 1];
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
                        int upperBound = (int)(Point2.Y + 0.5);
                        int lowerBound = (int)(Point1.Y + 0.5);

                        // 和扫描线没有交点的直线，直接跳过
                        if (lowerBound == upperBound)
                        {
                            continue;
                        }

                        tagEdge firstTagEdge = new tagEdge();
                        firstTagEdge.dx = (Point2.Y - Point1.Y) / (Point2.X - Point1.X);
                        firstTagEdge.xi = Point2.X - (Point2.Y - lowerBound - 0.5) * (Point2.X - Point1.X) / (Point2.Y - Point1.Y);

                        switch (JudgeVectorType(Point1, Point2, Point3))
                        {
                            case VectorType.NormalVector:
                                AET[lowerBound].Add(firstTagEdge);
                                for (lowerBound++; lowerBound < upperBound; lowerBound++)
                                {
                                    // 水平扫描线的坐标 lowerBound + 0.5
                                    AET[lowerBound].Add(new tagEdge()
                                    {
                                        dx = firstTagEdge.dx,  // == AET[lowerBound - 1].Last().dx
                                        xi = AET[lowerBound - 1].Last().xi + 1 / firstTagEdge.dx,
                                    });
                                }

                                firstTagEdge = null;
                                break;
                            case VectorType.LeftOrRightVector:
                                upperBound--;
                                AET[lowerBound].Add(firstTagEdge);
                                for (lowerBound++; lowerBound < upperBound; lowerBound++)
                                {
                                    // 水平扫描线的坐标 lowerBound + 0.5
                                    AET[lowerBound].Add(new tagEdge()
                                    {
                                        dx = firstTagEdge.dx,  // == AET[lowerBound - 1].Last().dx
                                        xi = AET[lowerBound - 1].Last().xi + 1 / firstTagEdge.dx,
                                    });
                                }
                                break;
                            case VectorType.TopOrBottomVector:
                                break;
                            case VectorType.OtherVector:
                                // 下面一条线段是平行线
                                // 在下面一天直线向上折，不用特殊处理，向下折需要upperBound - 1
                                if (Point4.Y > Point3.Y) // 向下折
                                {
                                    upperBound--;
                                }
                                AET[lowerBound].Add(firstTagEdge);
                                for (lowerBound++; lowerBound < upperBound; lowerBound++)
                                {
                                    // 水平扫描线的坐标 lowerBound + 0.5
                                    AET[lowerBound].Add(new tagEdge()
                                    {
                                        dx = firstTagEdge.dx,  // == AET[lowerBound - 1].Last().dx
                                        xi = AET[lowerBound - 1].Last().xi + 1 / firstTagEdge.dx,
                                    });
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

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
                return xi.CompareTo(obj);
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
    }
}
