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
using LiveCharts;
using LiveCharts.Defaults;
using System.ComponentModel;
using MCNPFileEditor.DataClassAndControl;
using LiveCharts.Wpf;

namespace MCNPFileEditor.CrossSectionImageShow
{
    /// <summary>
    /// CellStatisticChart.xaml 的交互逻辑
    /// </summary>
    public partial class CellStatisticChart : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Chart series
        SeriesCollection CellSeries_; // = new SeriesCollection();
        public SeriesCollection CellSeries
        {
            set
            {
                CellSeries_ = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CellSeries_"));
                }
            }
            get
            {
                return CellSeries_;
            }
        }

        public CellStatisticChart()
        {
            InitializeComponent();
            //SetInititalPara();
        }

        public void SetInititalPara(Phantom selectedPhantom)
        {
            CellSeries = null;

            if (selectedPhantom != null)
            {
                int[] cellCount = new int[1000];
                for (int i = 0; i < selectedPhantom.RepeatStructureInAPhantom.DimZ; i++)
                {
                    for (int j = 0; j < selectedPhantom.RepeatStructureInAPhantom.DimY; j++)
                    {
                        for (int k = 0; k < selectedPhantom.RepeatStructureInAPhantom.DimX; k++)
                        {
                            cellCount[selectedPhantom.RepeatStructureInAPhantom.RepeatMatrix[i, j, k]]++;
                        }
                    }
                }

                CellSeries = new SeriesCollection();
                ChartValues<ObservablePoint> newValueList = new ChartValues<ObservablePoint>();

                for (int i = 0; i < 1000; i++)
                {
                    if (i != 150 && i != 119 && cellCount[i] != 0)
                    {
                        newValueList.Add(new ObservablePoint(i, cellCount[i]));
                    }
                }

                ColumnSeries newColumnSeries = new ColumnSeries();

                newColumnSeries.Values = newValueList;

                CellSeries.Add(newColumnSeries);

                cellStaticChartLVC.Series = CellSeries;
            }

        }
    }
}
