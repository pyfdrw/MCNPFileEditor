using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCNPFileEditor.DataClassAndControl
{
    /// <summary>
    /// 所有体模的集合
    /// </summary>
    public class PhantomsCollection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        ObservableCollection<Phantom> AllPhantoms;
    }

    /// <summary>
    /// 单个体模
    /// </summary>
    public class Phantom : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        RepeatStructure RepeatStructureInAPhantom_;
        RepeatStructure RepeatStructureInAPhantom
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
    }

    /// <summary>
    /// 单个体模内部包含的重复结构
    /// </summary>
    public class RepeatStructure : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

    }

    /// <summary>
    /// 每个体模的cell信息，假设所有体模使用相同的器官列表
    /// </summary>
    public class CellsCollection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

    }

    /// <summary>
    /// 单个体模中单个cell的信息
    /// </summary>
    public class Cell : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

    }
}
