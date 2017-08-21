﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace MCNPFileEditor.DataClassAndControl
{
    /// <summary>
    /// 所有体模的集合
    /// </summary>
    public class PhantomsCollection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Phantom> AllPhantoms;

        public PhantomsCollection(string[] filePathCollection, int repeatIndex, string mode, string OrganNamefilePath = "")
        {
            AllPhantoms = new ObservableCollection<Phantom>();
            foreach (var item in filePathCollection)
            {
                AllPhantoms.Add(new Phantom(item, repeatIndex, mode, OrganNamefilePath));
            }
        }

        public PhantomsCollection()
        {
        }

        // 两个体模的名称属性是一样的即认为是相同的体模
        public static void AutoDeleteRepeatPhantom(PhantomsCollection thePhantomsCollection)
        {
            if (thePhantomsCollection != null && thePhantomsCollection.AllPhantoms.Count != 0)
            {

            }
            for (int i = 0; i < thePhantomsCollection.AllPhantoms.Count; i++)
            {
                if (thePhantomsCollection.AllPhantoms.Where(x => (x.PhantomName.Equals(thePhantomsCollection.AllPhantoms[i].PhantomName) && x != thePhantomsCollection.AllPhantoms[i])).ToList().Any())
                {
                    thePhantomsCollection.AllPhantoms.RemoveAt(i);
                    i--;
                }
            }
        }

        // 统一所有体模的器官颜色
        // 均和排在首位的体模器官颜色保持一致
        public static void UniformOrganColorList(PhantomsCollection thePhantomsCollection)
        {
            if (thePhantomsCollection == null || thePhantomsCollection.AllPhantoms.Count <= 1)
            {
                return;
            }
            else
            {
                for (int i = 0; i < thePhantomsCollection.AllPhantoms[0].CellsCollectionInAPhantom.AllCells.Length; i++)
                {
                    if (thePhantomsCollection.AllPhantoms[0].CellsCollectionInAPhantom.AllCells[i] != null)
                    {
                        Color colortmp = thePhantomsCollection.AllPhantoms[0].CellsCollectionInAPhantom.AllCells[i].CellColor;
                        foreach (var item in thePhantomsCollection.AllPhantoms)
                        {
                            if (item.CellsCollectionInAPhantom.AllCells[i] != null)
                            {
                                item.CellsCollectionInAPhantom.AllCells[i].CellColor = Color.FromArgb(colortmp.A, colortmp.R, colortmp.G, colortmp.B);
                            }
                        }
                    }

                }
            }
        }
    }

    /// <summary>
    /// 单个体模
    /// </summary>
    public class Phantom : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        RepeatStructure RepeatStructureInAPhantom_;
        public RepeatStructure RepeatStructureInAPhantom
        {
            set
            {
                RepeatStructureInAPhantom_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("RepeatStructureInAPhantom_"));
                }
            }
            get
            {
                return RepeatStructureInAPhantom_;
            }
        }

        CellsCollection CellsCollectionInAPhantom_;
        public CellsCollection CellsCollectionInAPhantom
        {
            set
            {
                CellsCollectionInAPhantom_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CellsCollectionInAPhantom_"));
                }
            }
            get
            {
                return CellsCollectionInAPhantom_;
            }
        }

        string PhantomName_;
        public string PhantomName
        {
            set
            {
                PhantomName_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PhantomName_"));
                }
            }
            get
            {
                return PhantomName_;
            }
        }

        string FilePath_;
        public string FilePath
        {
            set
            {
                FilePath_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FilePath_"));
                }
            }
            get
            {
                return FilePath_;
            }
        }

        // 所有手绘的靶区边界集合
        // 三个截面
        // 不同的是索引是从1开始的
        public SketchCollInASlice[] SketchCollForAllTransverse;
        public SketchCollInASlice[] SketchCollForAllFrontal;
        public SketchCollInASlice[] SketchCollForAllSagittal;

        public Phantom(string filePath, int repeatIndex, string mode, string OrganNamefilePath = "")
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath + " File Not Found!");
            }
            else
            {
                SketchCollForAllTransverse = new SketchCollInASlice[1500];
                SketchCollForAllFrontal = new SketchCollInASlice[1500];
                SketchCollForAllSagittal = new SketchCollInASlice[1500];
                for (int i = 0; i < SketchCollForAllTransverse.Length; i++)
                {
                    SketchCollForAllTransverse[i] = new SketchCollInASlice();
                    SketchCollForAllFrontal[i] = new SketchCollInASlice();
                    SketchCollForAllSagittal[i] = new SketchCollInASlice();
                }

                // MCNP 输入文件以空行作为分割标志，第一行是可选的注释行，直接跳过
                string[] alllines = File.ReadAllLines(filePath);
                int lintCount = alllines.Count();

                PhantomName = Path.GetFileNameWithoutExtension(filePath);
                FilePath = filePath;

                int lineNumTmp = 1; // 跳过第一行

                // 变量初始化
                RepeatStructureInAPhantom = new RepeatStructure();
                CellsCollectionInAPhantom = new CellsCollection();
                CellsCollectionInAPhantom.AllCells = new Cell[200];

                RepeatStructureInAPhantom.RepCellIndex = repeatIndex;

                // 文件处理
                for (; lineNumTmp < lintCount; lineNumTmp++)
                {
                    if (alllines[lineNumTmp].Length == 0)
                    {
                        break;
                    }
                    else
                    {
                        if (alllines[lineNumTmp][0] == 'C' || alllines[lineNumTmp][0] == 'c') // 略过注释行
                        {
                            continue;
                        }
                        else
                        {
                            if ((alllines[lineNumTmp][0] >= '0' && alllines[lineNumTmp][0] <= '9' || alllines[lineNumTmp][0] == ' ') &&
                                (alllines[lineNumTmp][1] >= '0' && alllines[lineNumTmp][1] <= '9' || alllines[lineNumTmp][1] == ' ') &&
                                (alllines[lineNumTmp][2] >= '0' && alllines[lineNumTmp][2] <= '9' || alllines[lineNumTmp][2] == ' ') &&
                                (alllines[lineNumTmp][3] >= '0' && alllines[lineNumTmp][3] <= '9' || alllines[lineNumTmp][3] == ' ') &&
                                (alllines[lineNumTmp][4] >= '0' && alllines[lineNumTmp][4] <= '9' || alllines[lineNumTmp][4] == ' ')) // 一行cell
                            {
                                string[] allElement = alllines[lineNumTmp].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                                if (repeatIndex == Convert.ToInt32(allElement[0]))
                                {
                                    // 存储重复结构
                                    #region
                                    // begin end X
                                    int indexOfEqual = allElement[5].IndexOf('=');
                                    int indexOfColon = allElement[5].IndexOf(':');
                                    string numStrTmp = "";
                                    for (int mm = 1; mm < indexOfColon - indexOfEqual; mm++)
                                    {
                                        numStrTmp += allElement[5][indexOfEqual + mm];
                                    }
                                    RepeatStructureInAPhantom.LowerBoundX = Convert.ToInt32(numStrTmp);
                                    numStrTmp = "";
                                    numStrTmp = allElement[5].Substring(indexOfColon + 1);
                                    RepeatStructureInAPhantom.UpperBoundX = Convert.ToInt32(numStrTmp);

                                    // begin end Y
                                    indexOfEqual = -1;
                                    indexOfColon = allElement[6].IndexOf(':');
                                    numStrTmp = "";
                                    for (int mm = 1; mm < indexOfColon - indexOfEqual; mm++)
                                    {
                                        numStrTmp += allElement[6][indexOfEqual + mm];
                                    }
                                    RepeatStructureInAPhantom.LowerBoundY = Convert.ToInt32(numStrTmp);
                                    numStrTmp = "";
                                    numStrTmp = allElement[6].Substring(indexOfColon + 1);
                                    RepeatStructureInAPhantom.UpperBoundY = Convert.ToInt32(numStrTmp);

                                    // begin end Z
                                    indexOfEqual = -1;
                                    indexOfColon = allElement[7].IndexOf(':');
                                    numStrTmp = "";
                                    for (int mm = 1; mm < indexOfColon - indexOfEqual; mm++)
                                    {
                                        numStrTmp += allElement[7][indexOfEqual + mm];
                                    }
                                    RepeatStructureInAPhantom.LowerBoundZ = Convert.ToInt32(numStrTmp);
                                    numStrTmp = "";
                                    numStrTmp = allElement[7].Substring(indexOfColon + 1);
                                    RepeatStructureInAPhantom.UpperBoundZ = Convert.ToInt32(numStrTmp);

                                    // DIM
                                    RepeatStructureInAPhantom.DimX = (RepeatStructureInAPhantom.UpperBoundX - RepeatStructureInAPhantom.LowerBoundX + 1);
                                    RepeatStructureInAPhantom.DimY = (RepeatStructureInAPhantom.UpperBoundY - RepeatStructureInAPhantom.LowerBoundY + 1);
                                    RepeatStructureInAPhantom.DimZ = (RepeatStructureInAPhantom.UpperBoundZ - RepeatStructureInAPhantom.LowerBoundZ + 1);

                                    // 暂存所有体素
                                    short[] repeatVoxel = new short[RepeatStructureInAPhantom.DimX * RepeatStructureInAPhantom.DimY * RepeatStructureInAPhantom.DimZ];

                                    short uIndexBefore = 0; // r的出现，需要保留前面的那个universe编号
                                    int ourLocationNow = 0; // 下一个需要写入的universe的编号
                                    for (int j = lineNumTmp + 1; j < alllines.Count(); j++)
                                    {
                                        if ((alllines[j][0] == ' ' && alllines[j][1] == ' ' && alllines[j][2] == ' ' && alllines[j][3] == ' ' && alllines[j][4] == ' ') ||
                                            Regex.IsMatch(alllines[j], @" *&\s*") ||
                                            (!Regex.IsMatch(alllines[j], @" *&\s*") && j - 1 >= 0 && Regex.IsMatch(alllines[j - 1], @" *&\s*")))
                                        {
                                            lineNumTmp = j - 1;
                                            string alllinesjtmp = Regex.Replace(alllines[j], @"&", "");
                                            allElement = alllinesjtmp.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                                            for (int mm = 0; mm < allElement.Length; mm++)
                                            {
                                                // 使用了r
                                                if (allElement[mm][allElement[mm].Length - 1] == 'r' || allElement[mm][allElement[mm].Length - 1] == 'R')
                                                {
                                                    allElement[mm] = allElement[mm].ToLower();
                                                    allElement[mm] = System.Text.RegularExpressions.Regex.Replace(allElement[mm], "r", "");
                                                    int repCount = Convert.ToInt32(allElement[mm]);
                                                    for (int nn = 0; nn < repCount; nn++)
                                                    {
                                                        repeatVoxel[ourLocationNow + nn] = uIndexBefore;
                                                    }
                                                    ourLocationNow = (ourLocationNow + repCount);
                                                }
                                                else
                                                {
                                                    repeatVoxel[ourLocationNow] = Convert.ToInt16(allElement[mm]);
                                                    uIndexBefore = repeatVoxel[ourLocationNow];
                                                    ourLocationNow++;
                                                }
                                            }
                                        }
                                        else  // 非连续行
                                        {
                                            lineNumTmp = j - 1;
                                            break;
                                        }
                                    }

                                    //[Z,Y,X]
                                    RepeatStructureInAPhantom.RepeatMatrix = new short[RepeatStructureInAPhantom.DimZ, RepeatStructureInAPhantom.DimY, RepeatStructureInAPhantom.DimX];

                                    for (int mm = 0; mm < repeatVoxel.Length; mm++)
                                    {
                                        RepeatStructureInAPhantom.RepeatMatrix[(int)mm / (RepeatStructureInAPhantom.DimX * RepeatStructureInAPhantom.DimY),
                                            (int)mm % (RepeatStructureInAPhantom.DimX * RepeatStructureInAPhantom.DimY) / RepeatStructureInAPhantom.DimX,
                                            (int)mm % RepeatStructureInAPhantom.DimX]
                                            = repeatVoxel[mm];
                                    }
                                    repeatVoxel = null;
                                    #endregion

                                    // 存储cell信息
                                    for (int j = lineNumTmp + 1; j < alllines.Count(); j++)
                                    {
                                        if (alllines[j].Length == 0)
                                        {
                                            lineNumTmp = j - 1;
                                            break;
                                        }
                                        else
                                        {
                                            if (Regex.IsMatch(alllines[j], @"^ *[C|c]"))
                                                continue;
                                            else
                                            {
                                                Cell newCell = new Cell(alllines[j], mode);
                                                CellsCollectionInAPhantom.AllCells[newCell.CellIndex] = newCell;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                for (lineNumTmp = lineNumTmp + 1; lineNumTmp < lintCount; lineNumTmp++)
                {
                    if (alllines[lineNumTmp][0] == 'C' || alllines[lineNumTmp][0] == 'c') // 略过注释行
                    {
                        continue;
                    }
                    else
                    {
                        string[] allElement = alllines[lineNumTmp].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (allElement[0].Equals("1") && allElement[1].ToLower().Equals("rpp"))
                        {
                            RepeatStructureInAPhantom.ResolutionX = (float)Convert.ToDouble(allElement[3]);
                            RepeatStructureInAPhantom.ResolutionY = (float)Convert.ToDouble(allElement[5]);
                            RepeatStructureInAPhantom.ResolutionZ = (float)Convert.ToDouble(allElement[7]);
                            break;
                        }
                    }
                }

                // 更新cell的名称
                if (OrganNamefilePath != "" && File.Exists(OrganNamefilePath))
                {
                    alllines = File.ReadAllLines(OrganNamefilePath);
                    foreach (var item in alllines)
                    {
                        if (item.Length > 1)
                        {
                            string[] allElement = item.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (allElement.Count() >= 1)
                            {
                                try
                                {
                                    CellsCollectionInAPhantom.AllCells[Convert.ToInt32(allElement[0])].OrganName = "";
                                    for (int j = 1; j < allElement.Count(); j++)
                                    {
                                        CellsCollectionInAPhantom.AllCells[Convert.ToInt32(allElement[0])].OrganName += (allElement[j] + " ");
                                    }
                                }
                                catch
                                {
                                    allElement = item.Split(new char[] { '	' }, System.StringSplitOptions.RemoveEmptyEntries);
                                    if (allElement.Count() >= 2)
                                    {
                                        CellsCollectionInAPhantom.AllCells[Convert.ToInt32(allElement[0])].OrganName = "";
                                        for (int j = 1; j < allElement.Count(); j++)
                                        {
                                            CellsCollectionInAPhantom.AllCells[Convert.ToInt32(allElement[0])].OrganName += (allElement[j] + " ");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public Phantom()
        {
            SketchCollForAllTransverse = new SketchCollInASlice[1500];
            SketchCollForAllFrontal = new SketchCollInASlice[1500];
            SketchCollForAllSagittal = new SketchCollInASlice[1500];
            for (int i = 0; i < SketchCollForAllTransverse.Length; i++)
            {
                SketchCollForAllTransverse[i] = new SketchCollInASlice();
                SketchCollForAllFrontal[i] = new SketchCollInASlice();
                SketchCollForAllSagittal[i] = new SketchCollInASlice();
            }
        }

        public void ClearSketchCollForAll()
        {
            if (SketchCollForAllTransverse != null)
            {
                foreach (var item in SketchCollForAllTransverse)
                {
                    if (item.sketchInfoColl != null)
                    {
                        item.sketchInfoColl.Clear();
                    }
                }
            }

            if (SketchCollForAllFrontal != null)
            {
                foreach (var item in SketchCollForAllFrontal)
                {
                    if (item.sketchInfoColl != null)
                    {
                        item.sketchInfoColl.Clear();
                    }
                }
            }

            if (SketchCollForAllSagittal != null)
            {
                foreach (var item in SketchCollForAllSagittal)
                {
                    if (item.sketchInfoColl != null)
                    {
                        item.sketchInfoColl.Clear();
                    }
                }
            }
        }
        
        public void MoveOrgan(List<int> OrgansTobeMove, string organsMoveDirection, int organsMoceDis, int additionOrgan, bool shouldForceReplace)
        {
            if (OrgansTobeMove == null || (organsMoveDirection != "X" && organsMoveDirection != "Y" && organsMoveDirection != "Z"))
            {
                return;
            }

            int[,,] cellTempMatrix = new int[RepeatStructureInAPhantom.DimZ, RepeatStructureInAPhantom.DimY, RepeatStructureInAPhantom.DimX]; // 暂存需要替换的体素
            for (int i = 0; i < RepeatStructureInAPhantom.DimZ; i++)
            {
                for (int j = 0; j < RepeatStructureInAPhantom.DimY; j++)
                {
                    for (int k = 0; k < RepeatStructureInAPhantom.DimX; k++)
                    {
                        cellTempMatrix[i, j, k] = -1;
                    }
                }
            }

            // 找到所有需要替换的元素位置，并沿着organsMoveDirection正方向平移organsMoceDis像素
            for (int i = 0; i < RepeatStructureInAPhantom.DimZ; i++)
            {
                for (int j = 0; j < RepeatStructureInAPhantom.DimY; j++)
                {
                    for (int k = 0; k < RepeatStructureInAPhantom.DimX; k++)
                    {
                        // RepeatStructureInAPhantom.RepeatMatrix[i,j,k]
                        if (OrgansTobeMove.Find(x => x == RepeatStructureInAPhantom.RepeatMatrix[i, j, k]) == (RepeatStructureInAPhantom.RepeatMatrix[i, j, k]))
                        {
                            // cellTempMatrix[i, j, k] = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                            if (organsMoveDirection == "X")
                            {
                                if ((k + organsMoceDis) < RepeatStructureInAPhantom.DimX && (k + organsMoceDis) >= 0)
                                {
                                    cellTempMatrix[i, j, k + organsMoceDis] = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                }
                            }
                            else if (organsMoveDirection == "Y")
                            {
                                if ((j + organsMoceDis) < RepeatStructureInAPhantom.DimY && (j + organsMoceDis) >= 0)
                                {
                                    cellTempMatrix[i, j + organsMoceDis, k] = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                }
                            }
                            else if (organsMoveDirection == "Z")
                            {
                                if ((i + organsMoceDis) < RepeatStructureInAPhantom.DimZ && (i + organsMoceDis) >= 0)
                                {
                                    cellTempMatrix[i + organsMoceDis, j, k] = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                }
                            }
                            RepeatStructureInAPhantom.RepeatMatrix[i, j, k] = (short)additionOrgan;
                        }
                    }
                }
            }

            // 不同替换方式
            if (shouldForceReplace)
            {
                for (int i = 0; i < RepeatStructureInAPhantom.DimZ; i++)
                {
                    for (int j = 0; j < RepeatStructureInAPhantom.DimY; j++)
                    {
                        for (int k = 0; k < RepeatStructureInAPhantom.DimX; k++)
                        {
                            if (cellTempMatrix[i, j, k] != -1)
                            {
                                RepeatStructureInAPhantom.RepeatMatrix[i, j, k] = (short)cellTempMatrix[i, j, k];
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < RepeatStructureInAPhantom.DimZ; i++)
                {
                    for (int j = 0; j < RepeatStructureInAPhantom.DimY; j++)
                    {
                        for (int k = 0; k < RepeatStructureInAPhantom.DimX; k++)
                        {
                            if (cellTempMatrix[i, j, k] != -1 && (RepeatStructureInAPhantom.RepeatMatrix[i, j, k] == 119 || RepeatStructureInAPhantom.RepeatMatrix[i, j, k] == 150))
                            {
                                RepeatStructureInAPhantom.RepeatMatrix[i, j, k] = (short)cellTempMatrix[i, j, k];
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 按照MCNP输入文件格式输出文件
        /// </summary>
        /// <param name="outputfilePath">输出文件完整路径</param>
        public void OutPutPhantom(string outputfilePath)
        {
            if (!string.IsNullOrEmpty(outputfilePath) && Directory.Exists(Path.GetDirectoryName(outputfilePath)) &&
                RepeatStructureInAPhantom != null && CellsCollectionInAPhantom != null)
            {
                using (var fs = new FileStream(outputfilePath, FileMode.Create))
                {
                    StreamWriter sw = new StreamWriter(fs);

                    sw.WriteLine(outputfilePath);
                    sw.WriteLine("C * *****************************************************************************");
                    sw.WriteLine("C                               Cells cards");
                    sw.WriteLine("C * *****************************************************************************");
                    sw.WriteLine("888   0  2                          $ exterior - zero importance");
                    sw.WriteLine("555   0 - 2     fill = " + RepeatStructureInAPhantom.RepCellIndex + "            $ Others phantom box");
                    // RepeatStructureInAPhantom.RepCellIndex
                    sw.WriteLine("C * ***************Image data start from here*******************************");
                    sw.Write("C Voxel size : ");
                    sw.Write(RepeatStructureInAPhantom.ResolutionX.ToString("f3").PadLeft(7) + " X " +
                        RepeatStructureInAPhantom.ResolutionY.ToString("f3").PadLeft(7) + " X " +
                        RepeatStructureInAPhantom.ResolutionZ.ToString("f3").PadLeft(7) + Environment.NewLine);
                    sw.Write("C Voxel size : ");
                    sw.Write(RepeatStructureInAPhantom.DimX.ToString("f3").PadLeft(7) + " " +
                        RepeatStructureInAPhantom.DimY.ToString("f3").PadLeft(7) + " " +
                        RepeatStructureInAPhantom.DimZ.ToString("f3").PadLeft(7) + Environment.NewLine);
                    sw.Write(Convert.ToString(RepeatStructureInAPhantom.RepCellIndex) + "   0 -1 lat=1 u=" + RepeatStructureInAPhantom.RepCellIndex + " fill=");
                    sw.Write(RepeatStructureInAPhantom.LowerBoundX.ToString() + ":" + RepeatStructureInAPhantom.UpperBoundX.ToString() + " " +
                        RepeatStructureInAPhantom.LowerBoundY.ToString() + ":" + RepeatStructureInAPhantom.UpperBoundY.ToString() + " " +
                        RepeatStructureInAPhantom.LowerBoundZ.ToString() + ":" + RepeatStructureInAPhantom.UpperBoundZ.ToString() + Environment.NewLine);
                    int lastVoxel = -1;
                    int samevoxelcount = 0;
                    string newline = "     ";

                    for (int i = 0; i < RepeatStructureInAPhantom.DimZ; i++)
                    {
                        for (int j = 0; j < RepeatStructureInAPhantom.DimY; j++)
                        {
                            for (int k = 0; k < RepeatStructureInAPhantom.DimX; k++)
                            {
                                if (RepeatStructureInAPhantom.RepeatMatrix[i, j, k] == lastVoxel)
                                {
                                    lastVoxel = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                    CellsCollectionInAPhantom.AllCells[RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].NumSum++;
                                    samevoxelcount++;
                                }
                                else
                                {
                                    // 新的体素
                                    if (samevoxelcount == 0)
                                    {
                                        newline += (RepeatStructureInAPhantom.RepeatMatrix[i, j, k].ToString() + " ");
                                        CellsCollectionInAPhantom.AllCells[RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].NumSum++;
                                        lastVoxel = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                    }
                                    else
                                    {
                                        CellsCollectionInAPhantom.AllCells[RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].NumSum++;
                                        newline += (samevoxelcount.ToString() + "r ");
                                        samevoxelcount = 0;
                                        newline += (RepeatStructureInAPhantom.RepeatMatrix[i, j, k].ToString() + " ");
                                        lastVoxel = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                    }
                                    // 一行不要超过80字符
                                    if (newline.Length >= 69)
                                    {
                                        sw.Write(newline + Environment.NewLine);
                                        newline = "     ";
                                    }
                                }
                            }
                        }
                    }
                    if (samevoxelcount != 0)
                    {
                        newline += (" " + samevoxelcount.ToString() + "r");
                    }
                    if (newline != null && newline != "     ")
                    {
                        sw.Write(newline + Environment.NewLine);
                        newline = "";
                    }

                    foreach (var item in CellsCollectionInAPhantom.AllCells)
                    {
                        if (item == null)
                        {
                            continue;
                        }
                        if (item.MaterialIndex == 0)
                        {
                            sw.Write(item.CellIndex.ToString("d").PadRight(5) + item.MaterialIndex.ToString("d").PadRight(5) + "          " + item.SurfaceComposation.ToString("d").PadRight(5) +
                                "u = " + item.UniverseIndex.ToString("d").PadRight(5) +
                                "vol = " + (item.NumSum * RepeatStructureInAPhantom.ResolutionX * RepeatStructureInAPhantom.ResolutionY * RepeatStructureInAPhantom.ResolutionZ).ToString("f4").PadRight(13) +
                                "$ n = " + item.NumSum.ToString("d").PadRight(13) + item.OrganName + Environment.NewLine);
                        }
                        else
                        {
                            sw.Write(item.CellIndex.ToString("d").PadRight(5) + item.MaterialIndex.ToString("d").PadRight(5) + item.DensityValue.ToString("f6").PadRight(10) +
                                item.SurfaceComposation.ToString("d").PadRight(5) +
                                "u = " + item.UniverseIndex.ToString("d").PadRight(5) +
                                "vol = " + (item.NumSum * RepeatStructureInAPhantom.ResolutionX * RepeatStructureInAPhantom.ResolutionY * RepeatStructureInAPhantom.ResolutionZ).ToString("f4").PadRight(13) +
                                "$ n = " + item.NumSum.ToString("d").PadRight(13) + item.OrganName + Environment.NewLine);
                        }
                    }

                    //surface
                    sw.Write(Environment.NewLine);
                    sw.Write("C ******************************************************************************" + Environment.NewLine);
                    sw.Write("C                               Surface cards" + Environment.NewLine);
                    sw.Write("C ******************************************************************************" + Environment.NewLine);
                    sw.Write("1    rpp ");
                    sw.Write(0.ToString("f2").PadRight(6, ' ') + RepeatStructureInAPhantom.ResolutionX.ToString("f2").PadRight(6, ' ') +
                        0.ToString("f2").PadRight(6, ' ') + RepeatStructureInAPhantom.ResolutionY.ToString("f2").PadRight(6, ' ') +
                        0.ToString("f2").PadRight(6, ' ') + RepeatStructureInAPhantom.ResolutionZ.ToString("f2").PadRight(6, ' '));
                    sw.Write("  $ Voxel size" + Environment.NewLine);
                    sw.Write("2    rpp ");
                    sw.Write(0.ToString("f2").PadRight(6, ' ') + (RepeatStructureInAPhantom.DimX * RepeatStructureInAPhantom.ResolutionX).ToString("f2").PadRight(6, ' ') +
                        0.ToString("f2").PadRight(6, ' ') + (RepeatStructureInAPhantom.DimY * RepeatStructureInAPhantom.ResolutionY).ToString("f2").PadRight(6, ' ') +
                        0.ToString("f2").PadRight(6, ' ') + (RepeatStructureInAPhantom.DimZ * RepeatStructureInAPhantom.ResolutionZ).ToString("f2").PadRight(6, ' '));
                    sw.Write("  $ Box" + Environment.NewLine);
                    sw.Write("3    pz  -1e2    $ XY plane used in universe definition");
                    sw.Flush();
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// 按照Archer的输入文件需求输出体模几何
        /// </summary>
        /// <param name="outputfilePath">输出文件完整路径</param>
        public void OutPutPhantomForArcher(string outputfilePath)
        {
            if (!string.IsNullOrEmpty(outputfilePath) && Directory.Exists(Path.GetDirectoryName(outputfilePath)) &&
                RepeatStructureInAPhantom != null && CellsCollectionInAPhantom != null)
            {
                using (var fs = new FileStream(outputfilePath, FileMode.Create))
                {
                    StreamWriter sw = new StreamWriter(fs);

                    sw.WriteLine(outputfilePath);
                    sw.WriteLine("C * *****************************************************************************");
                    sw.WriteLine("C                               Geo For Archer");
                    sw.WriteLine("C * *****************************************************************************");
                    sw.WriteLine("num-voxel-x    " + " = " + RepeatStructureInAPhantom.DimX.ToString("f3").PadLeft(7));
                    sw.WriteLine("num-voxel-y    " + " = " + RepeatStructureInAPhantom.DimY.ToString("f3").PadLeft(7));
                    sw.WriteLine("num-voxel-z    " + " = " + RepeatStructureInAPhantom.DimZ.ToString("f3").PadLeft(7));
                    sw.WriteLine("dim-voxel-x    " + " = " + RepeatStructureInAPhantom.ResolutionX.ToString("f3").PadLeft(7));
                    sw.WriteLine("dim-voxel-y    " + " = " + RepeatStructureInAPhantom.ResolutionY.ToString("f3").PadLeft(7));
                    sw.WriteLine("dim-voxel-z    " + " = " + RepeatStructureInAPhantom.ResolutionZ.ToString("f3").PadLeft(7));
                    // RepeatStructureInAPhantom.RepCellIndex
                    sw.WriteLine("C * ***************Image data start from here*******************************");
                    int lastVoxel = -1;
                    int samevoxelcount = 0;
                    string newline = "     ";

                    for (int i = 0; i < RepeatStructureInAPhantom.DimZ; i++)
                    {
                        for (int j = 0; j < RepeatStructureInAPhantom.DimY; j++)
                        {
                            for (int k = 0; k < RepeatStructureInAPhantom.DimX; k++)
                            {
                                if (RepeatStructureInAPhantom.RepeatMatrix[i, j, k] == lastVoxel)
                                {
                                    lastVoxel = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                    CellsCollectionInAPhantom.AllCells[RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].NumSum++;
                                    samevoxelcount++;
                                }
                                else
                                {
                                    // 新的体素
                                    if (samevoxelcount == 0)
                                    {
                                        newline += (RepeatStructureInAPhantom.RepeatMatrix[i, j, k].ToString() + " ");
                                        CellsCollectionInAPhantom.AllCells[RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].NumSum++;
                                        lastVoxel = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                    }
                                    else
                                    {
                                        CellsCollectionInAPhantom.AllCells[RepeatStructureInAPhantom.RepeatMatrix[i, j, k]].NumSum++;
                                        newline += (samevoxelcount.ToString() + "r ");
                                        samevoxelcount = 0;
                                        newline += (RepeatStructureInAPhantom.RepeatMatrix[i, j, k].ToString() + " ");
                                        lastVoxel = RepeatStructureInAPhantom.RepeatMatrix[i, j, k];
                                    }
                                    // 一行不要超过80字符
                                    if (newline.Length >= 69)
                                    {
                                        sw.Write(newline + Environment.NewLine);
                                        newline = "     ";
                                    }
                                }
                            }
                        }
                    }
                    if (samevoxelcount != 0)
                    {
                        newline += (" " + samevoxelcount.ToString() + "r");
                    }
                    if (newline != "     ")
                    {
                        sw.Write(newline + Environment.NewLine);
                        newline = "";
                    }

                    foreach (var item in CellsCollectionInAPhantom.AllCells)
                    {
                        if (item == null)
                        {
                            continue;
                        }
                        if (item.MaterialIndex == 0)
                        {
                            sw.Write(item.CellIndex.ToString("d").PadRight(5) + item.MaterialIndex.ToString("d").PadRight(5) + "          " + item.SurfaceComposation.ToString("d").PadRight(5) +
                                     "u = " + item.UniverseIndex.ToString("d").PadRight(5) +
                                     "vol = " + (item.NumSum * RepeatStructureInAPhantom.ResolutionX * RepeatStructureInAPhantom.ResolutionY * RepeatStructureInAPhantom.ResolutionZ).ToString("f4").PadRight(13) +
                                     "$ n = " + item.NumSum.ToString("d").PadRight(13) + item.OrganName + Environment.NewLine);
                        }
                        else
                        {
                            sw.Write(item.CellIndex.ToString("d").PadRight(5) + item.MaterialIndex.ToString("d").PadRight(5) + item.DensityValue.ToString("f6").PadRight(10) +
                                     item.SurfaceComposation.ToString("d").PadRight(5) +
                                     "u = " + item.UniverseIndex.ToString("d").PadRight(5) +
                                     "vol = " + (item.NumSum * RepeatStructureInAPhantom.ResolutionX * RepeatStructureInAPhantom.ResolutionY * RepeatStructureInAPhantom.ResolutionZ).ToString("f4").PadRight(13) +
                                     "$ n = " + item.NumSum.ToString("d").PadRight(13) + item.OrganName + Environment.NewLine);
                        }
                    }

                    sw.Flush();
                    sw.Close();
                }
            }
        }
    }

    /// <summary>
    /// 单个体模内部包含的重复结构
    /// </summary>
    public class RepeatStructure : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // 重复结构cell编号
        int RepCellIndex_;
        public int RepCellIndex
        {
            set
            {
                RepCellIndex_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("RepCellIndex_"));
                }
            }
            get
            {
                return RepCellIndex_;
            }
        }

        // X上下界
        int LowerBoundX_;
        public int LowerBoundX
        {
            set
            {
                LowerBoundX_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LowerBoundX_"));
                }
            }
            get
            {
                return LowerBoundX_;
            }
        }
        int UpperBoundX_;
        public int UpperBoundX
        {
            set
            {
                UpperBoundX_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UpperBoundX_"));
                }
            }
            get
            {
                return UpperBoundX_;
            }
        }

        // Y上下界
        int LowerBoundY_;
        public int LowerBoundY
        {
            set
            {
                LowerBoundY_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LowerBoundY_"));
                }
            }
            get
            {
                return LowerBoundY_;
            }
        }
        int UpperBoundY_;
        public int UpperBoundY
        {
            set
            {
                UpperBoundY_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UpperBoundY_"));
                }
            }
            get
            {
                return UpperBoundY_;
            }
        }

        // Z上下界
        int LowerBoundZ_;
        public int LowerBoundZ
        {
            set
            {
                LowerBoundZ_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LowerBoundZ_"));
                }
            }
            get
            {
                return LowerBoundZ_;
            }
        }
        int UpperBoundZ_;
        public int UpperBoundZ
        {
            set
            {
                UpperBoundZ_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UpperBoundZ_"));
                }
            }
            get
            {
                return UpperBoundZ_;
            }
        }

        // X Y Z 维度
        int DimX_;
        public int DimX
        {
            set
            {
                DimX_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DimX_"));
                }
            }
            get
            {
                return DimX_;
            }
        }
        int DimY_;
        public int DimY
        {
            set
            {
                DimY_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DimY_"));
                }
            }
            get
            {
                return DimY_;
            }
        }
        int DimZ_;
        public int DimZ
        {
            set
            {
                DimZ_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DimZ_"));
                }
            }
            get
            {
                return DimZ_;
            }
        }

        // X Y Z Resolution分辨率
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

        // 重复结构矩阵
        short[,,] RepeatMatrix_;
        public short[,,] RepeatMatrix
        {
            set
            {
                RepeatMatrix_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("RepeatMatrix_"));
                }
            }
            get
            {
                return RepeatMatrix_;
            }
        }

        // 体模外边界外扩，不支持有靶区时的外扩
        public void ExtendBorder(int extendLength, short voidNum = 150)
        {
            short[,,] RepeatMatrixTmp =
                new short[DimZ + 2 * extendLength, DimY + 2 * extendLength, DimX + 2 * extendLength];
            for (int i = 0; i < DimZ + 2 * extendLength; i++)
            {
                for (int j = 0; j < DimY + 2 * extendLength; j++)
                {
                    for (int k = 0; k < DimX + 2 * extendLength; k++)
                    {
                        RepeatMatrixTmp[i, j, k] = voidNum;
                    }
                }
            }
            for (int i = 0; i < DimZ; i++)
            {
                for (int j = 0; j < DimY; j++)
                {
                    for (int k = 0; k < DimX; k++)
                    {
                        RepeatMatrixTmp[i + extendLength, j + extendLength, k + extendLength] = RepeatMatrix[i, j, k];
                    }
                }
            }
            RepeatMatrix = null;
            RepeatMatrix = RepeatMatrixTmp;
            DimX = DimX + 2 * extendLength;
            DimY = DimY + 2 * extendLength;
            DimZ = DimZ + 2 * extendLength;

            UpperBoundX = UpperBoundX + 2 * extendLength;
            UpperBoundY = UpperBoundY + 2 * extendLength;
            UpperBoundZ = UpperBoundZ + 2 * extendLength;
        }
    }

    /// <summary>
    /// 每个体模的cell信息，假设所有体模使用相同的器官列表
    /// </summary>
    public class CellsCollection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Cell[] AllCells;
    }

    /// <summary>
    /// 单个体模中单个cell的信息
    /// </summary>
    public class Cell : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // 指示当前Cell是否为有效Cell
        public bool IsCellEffective;

        // 器官名称
        string OrganName_;
        public string OrganName
        {
            set
            {
                OrganName_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("OrganName"));
                }
            }
            get
            {
                return OrganName_;
            }
        }

        // Cell索引
        int CellIndex_;
        public int CellIndex
        {
            set
            {
                CellIndex_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CellIndex_"));
                }
            }
            get
            {
                return CellIndex_;
            }
        }

        // Universe索引
        int UniverseIndex_;
        public int UniverseIndex
        {
            set
            {
                UniverseIndex_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UniverseIndex_"));
                }
            }
            get
            {
                return UniverseIndex_;
            }
        }

        // 材料索引
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

        // 密度信息
        double DensityValue_;
        public double DensityValue
        {
            set
            {
                DensityValue_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DensityValue_"));
                }
            }
            get
            {
                return DensityValue_;
            }
        }

        // SurfaceCompositation
        int SurfaceComposation_;
        public int SurfaceComposation
        {
            set
            {
                SurfaceComposation_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SurfaceComposation_"));
                }
            }
            get
            {
                return SurfaceComposation_;
            }
        }

        // 显示颜色
        Color CellColor_;
        public Color CellColor
        {
            set
            {
                CellColor_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CellColor_"));
                }
            }
            get
            {
                return CellColor_;
            }
        }

        // 出现次数统计
        public int NumSum = 0;

        /// <summary>
        /// 构造函数
        /// 从一行字符串得到该cell信息
        /// </summary>
        /// <param name="cellLine">字符串行，包含cell信息</param>
        /// <param name="mode">操作模式，分成"complicate"与"simple"两种方式</param>
        public Cell(string cellLine, string mode)
        {
            string[] lineblocks = cellLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            OrganName = "COMMON TISSUE";
            if (lineblocks == null || lineblocks.Count() == 0)
            {
                IsCellEffective = false;
                CellColor = Color.FromArgb(0, 255, 255, 255);
                return;
            }
            else
            {
                IsCellEffective = true;
                CellIndex = Convert.ToInt32(lineblocks[0]);
                Random rd = new Random(CellIndex + DateTime.Now.Millisecond);
                UniverseIndex = CellIndex;
                if (mode == "simple")
                {
                    MaterialIndex = CellIndex;
                    DensityValue = 0;
                    SurfaceComposation = 3;
                    CellColor = Color.FromArgb((byte)rd.Next(200, 255), (byte)rd.Next(0, 255), (byte)rd.Next(0, 255), (byte)rd.Next(0, 255));
                }
                else
                {
                    MaterialIndex = Convert.ToInt32(lineblocks[1]);
                    if (0 == MaterialIndex)
                    {
                        DensityValue = 0;
                        SurfaceComposation = 3;
                        CellColor = Color.FromArgb((byte)rd.Next(200, 255), (byte)rd.Next(0, 255), (byte)rd.Next(0, 255), (byte)rd.Next(0, 255));
                    }
                    else
                    {
                        DensityValue = Convert.ToDouble(lineblocks[2]);
                        SurfaceComposation = 3;
                        CellColor = Color.FromArgb((byte)rd.Next(200, 255), (byte)rd.Next(0, 255), (byte)rd.Next(0, 255), (byte)rd.Next(0, 255));
                    }
                }
            }
        }

        public Cell()
        {
            OrganName = "COMMON TISSUE";
            IsCellEffective = false;
            CellColor = Color.FromArgb(255, 255, 255, 255);
        }
    }

    // 一层Mask的集合
    // 层数按照集合的索引来
    public class SketchCollInASlice
    {
        public List<SketchInfo> sketchInfoColl;

        public SketchCollInASlice()
        {
            sketchInfoColl = new List<SketchInfo>();
        }
    }

    // 单个Mask
    public class SketchInfo
    {
        public int cellIndex;
        public List<Point> vertexColl;
        public MaskType maskType;

        public SketchInfo(int indexcell, Point insertpoint, MaskType masktype)
        {
            if (vertexColl == null)
            {
                vertexColl = new List<Point>();
            }

            cellIndex = indexcell;
            vertexColl.Add(insertpoint);
            maskType = masktype;
        }

        public SketchInfo(int indexcell)
        {
            if (vertexColl == null)
            {
                vertexColl = new List<Point>();
            }

            cellIndex = indexcell;
            maskType = MaskType.PolygonType;
        }
    }

    public enum MaskType
    {
        PointType,
        RectangleType,
        PolygonType,
        CircleType,
    }
}
