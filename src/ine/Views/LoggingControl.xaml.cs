using ine.Core;
using ine.Domain;
using ine.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ine.Views
{
    public partial class LoggingControl : UserControl
    {
        private readonly ControlModel model;

        public LoggingControl()
        {
            this.model = new ControlModel();
            this.DataContext = this.model;

            this.InitializeComponent();
            this.Unloaded += HandleUnloaded;
            this.Loaded += HandleLoaded;
        }

        public void AddLog(LogEntry entry)
        {
            this.model.AddLog(entry);
        }

        public class ControlModel : ViewModelBase
        {
            public ControlModel()
            {
                this.Entries = new ObservableCollection<EntryModel>();
            }

            public ObservableCollection<EntryModel> Entries { get; set; }
            public double VerticalOffset { get; set; }

            public void AddLog(LogEntry entry)
            {
                this.Entries.Add(EntryModel.FromEntry(entry));

                if (this.Entries.Count > 1000)
                {
                    this.Entries.RemoveAt(0);
                }
            }
        }

        public class EntryModel
        {
            public LogEntry Entry { get; set; }

            public string Time { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }

            public bool HasAttachment { get; set; }

            public static EntryModel FromEntry(LogEntry entry)
            {
                return new EntryModel
                {
                    Entry = entry,
                    Level = entry.Level,
                    Message = entry.Message,
                    HasAttachment = entry.Attachment != null,
                    Time = DateTime.Now.ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'ff", CultureInfo.InvariantCulture)
                };
            }
        }

        private void HandleShow(object sender, RoutedEventArgs e)
        {
            FrameworkElement source = (FrameworkElement)e.Source;
            EntryModel model = (EntryModel)source.DataContext;

            model.Entry.Show();
        }

        private void HandleImageInitialized(object sender, EventArgs e)
        {
            Image image = (Image)sender;
            string content = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAACDUlEQVQ4T42T38thQRjHv8evsikrJf/E2+7V2j9g/wAXlLIRIq/SKsUSpYgopYRSiFJCcbM3e7/b2n9AculilXCxq9eiMztzTrSOd7VzM3NmnvnM9/k+z+FAR7vdrtLpka3vjJrT6QxIzzm20Ww2CT28e5s+ws5rbrf7CiIA6vU68Xg8WK1WIIQIIDaf10ajEZvNBqPRCBzH1bxe7wUiACqVCvH7/ZhMJs+qMJlMWC6XUKlU6Pf7AiRABwsWAKVSiQSDQazX6ysFZyUGgwGLxQJKpVKAdLtdIZ1QKBQQAIVCgYTD4bsKpNKKxSIikQgnADKZDInFYthut5f8z69L/WC+MEW5XA6JREIEJJNJkkqlUPy2B09kdIenqXD4deQRenjCfD6/EsA8YfHpdFoERKNRks1m8f7TBmp6X6tQQKOWY3+UI/r66cYXvV6PeDyOfD4vAqgZhOX0+JmmwMugVsjxQiXDiar5+GqP2Wx2o4B5Rs0XAbQipFwuo/v9B15qNPR1FUsCpxPBG8PhxhedTgdWtWq1KgJoExHaTHhX/YrVzwMOv4+Qy2mNeYIvH95iOp3eKPD5fGg0GiLA4XCQVquF3W53Cfy7I6UV0Wq1cLlc6HQ6IsBmsxHWHP/qRGkPsCrY7Xb0ej0RYLFYyGAwAM/z0thnv2UyGaxWK4bDoQgwm83/8ztLYbXxeBz4A+zTACATSX04AAAAAElFTkSuQmCC";

            image.Source = content.ToBitmap();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            this.list.ScrollToVerticalOffset(this.model.VerticalOffset);
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            this.model.VerticalOffset = this.list.GetVerticalOffset();
        }
    }
}
