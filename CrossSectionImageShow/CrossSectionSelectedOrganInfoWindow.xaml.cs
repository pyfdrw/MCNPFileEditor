﻿using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// CrossSectionSelectedOrganInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CrossSectionSelectedOrganInfoWindow : Window
    {
        public CrossSectionSelectedOrganInfoWindow()
        {
            InitializeComponent();
        }

        public void RefreshInfo()
        {
            // 获取绑定的表达式进行更新
            BindingExpression newBE1 = BindingOperations.GetBindingExpression(CrossSectionTypeLabel, Label.ContentProperty);
            if (newBE1 != null)
            {
                newBE1.UpdateTarget();
            }

            BindingExpression newBE2 = BindingOperations.GetBindingExpression(OrganIndexLabel, Label.ContentProperty);
            if (newBE2 != null)
            {
                newBE2.UpdateTarget();
            }

            BindingExpression newBE3 = BindingOperations.GetBindingExpression(MaterialIndexLabel, Label.ContentProperty);
            if (newBE3 != null)
            {
                newBE3.UpdateTarget();
            }

            BindingExpression newBE4 = BindingOperations.GetBindingExpression(MaterialDensityLabel, Label.ContentProperty);
            if (newBE4 != null)
            {
                newBE4.UpdateTarget();
            }

            BindingExpression newBE5 = BindingOperations.GetBindingExpression(OrganNameLabel, Label.ContentProperty);
            if (newBE5 != null)
            {
                newBE5.UpdateTarget();
            }

            BindingExpression newBE6 = BindingOperations.GetBindingExpression(OrganColorLabel, Label.BackgroundProperty);
            if (newBE6 != null)
            {
                newBE6.UpdateTarget();
            }

            BindingExpression newBE7 = BindingOperations.GetBindingExpression(MouseLocationXLabel, Label.ContentProperty);
            if (newBE7 != null)
            {
                newBE7.UpdateTarget();
            }

            BindingExpression newBE8 = BindingOperations.GetBindingExpression(MouseLocationYLabel, Label.ContentProperty);
            if (newBE8 != null)
            {
                newBE8.UpdateTarget();
            }
        }
    }

    public class FloatvalueConvertWithFixed2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return DependencyProperty.UnsetValue;
            }
            else
            {
                try
                {
                    double mouseLocation = (double)value;
                    return mouseLocation.ToString("F2");
                }
                catch (Exception e)
                {
                    return DependencyProperty.UnsetValue;
                }
            }
            //throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 反向转换不作处理
            return DependencyProperty.UnsetValue;
            //throw new NotImplementedException();
        }
    }
}
