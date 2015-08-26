using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ine
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            new Facade().Handle(e.Exception, "Dispatcher caused unhandled exception");
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            new Facade().Handle((Exception)e.ExceptionObject, "AppDomain caused unhandled exception");
        }

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
            GridView view = listView.View as GridView;

            if (listView.ActualWidth == Double.NaN)
                listView.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

            int autoFillColumnIndex = view.Columns.Count - 1;
            double maxWidth = view.Columns.Max(x => x.ActualWidth);

            double remainingSpace = listView.ActualWidth;
            for (int i = 0; i < view.Columns.Count; i++)
            {
                if (view.Columns[i].ActualWidth != maxWidth)
                {
                    remainingSpace -= view.Columns[i].ActualWidth;
                }
                else
                {
                    autoFillColumnIndex = i;
                }
            }

            view.Columns[autoFillColumnIndex].Width = remainingSpace - 18 >= 0 ? remainingSpace - 18 : 0;
        }
    }
}
