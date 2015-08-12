using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ine
{
    public partial class App : Application
    {
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window element = (Window)GetTopLevelControl((DependencyObject)e.Source);
                element.DragMove();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window element = (Window)GetTopLevelControl((DependencyObject)e.Source);
            element.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Window element = (Window)GetTopLevelControl((DependencyObject)e.Source);
            element.WindowState = WindowState.Minimized;
        }

        private static DependencyObject GetTopLevelControl(DependencyObject control)
        {
            DependencyObject tmp = control;
            DependencyObject parent = null;

            while ((tmp = VisualTreeHelper.GetParent(tmp)) != null)
            {
                parent = tmp;
            }

            return parent;
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            ListView listView = (ListView)VisualTreeHelper.GetParent((DependencyObject)e.Source);

            int autoFillColumnIndex = (listView.View as GridView).Columns.Count - 1;
            if (listView.ActualWidth == Double.NaN)
                listView.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            double remainingSpace = listView.ActualWidth;
            for (int i = 0; i < (listView.View as GridView).Columns.Count; i++)
                if (i != autoFillColumnIndex)
                    remainingSpace -= (listView.View as GridView).Columns[i].ActualWidth;
            (listView.View as GridView).Columns[autoFillColumnIndex].Width = remainingSpace - 18 >= 0 ? remainingSpace - 18 : 0;
        }
    }
}
