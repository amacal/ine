using ine.Core;
using ine.Domain;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
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

            public void AddLog(LogEntry entry)
            {
                this.Entries.Add(new EntryModel
                {
                    Entry = entry,
                    Level = entry.Level,
                    Message = entry.Message,
                    Time = DateTime.Now.ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'ff", CultureInfo.InvariantCulture)
                });
            }
        }

        public class EntryModel
        {
            public LogEntry Entry { get; set; }

            public string Time { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
        }
    }
}
