using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ine.Extensions
{
    public static class ScrollExtensions
    {
        public static void ScrollToBottom(this ListView target)
        {
            FindScrollViewer(target).ScrollToBottom();
        }

        public static void ScrollToVerticalOffset(this ListView target, double value)
        {
            FindScrollViewer(target).ScrollToVerticalOffset(value);
        }

        public static double GetVerticalOffset(this ListView target)
        {
            return FindScrollViewer(target).VerticalOffset;
        }

        private static ScrollViewer FindScrollViewer(DependencyObject target)
        {
            if (target is ScrollViewer)
            {
                return target as ScrollViewer;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(target); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(target, i);
                ScrollViewer found = FindScrollViewer(child);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
