using System;
using System.Windows.Threading;

namespace ine.Extensions
{
    public static class DispatcherExtensions
    {
        public static void Handle(this Dispatcher dispatcher, Action action)
        {
            dispatcher.BeginInvoke(action);
        }
    }
}
