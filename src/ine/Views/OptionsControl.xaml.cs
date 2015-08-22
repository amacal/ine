using ine.Core;
using ine.Domain;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ine.Views
{
    public partial class OptionsControl : UserControl
    {
        private readonly ControlModel model;

        public OptionsControl()
        {
            this.model = new ControlModel();
            this.DataContext = this.model;

            this.InitializeComponent();
        }

        public Action<Configuration> OnConfiguration;

        public void SetConfiguration(Configuration configuration)
        {
            this.model.SetConfiguration(configuration);
        }

        public class ControlModel : ViewModelBase
        {
            public Configuration Configuration { get; set; }

            public string DownloadPath { get; set; }

            public void SetConfiguration(Configuration configuration)
            {
                this.Configuration = configuration;
                this.DownloadPath = this.Configuration.DownloadPath;
                this.Raise("DownloadPath");
            }

            public void SetDownloadPath(string path)
            {
                this.Configuration.DownloadPath = path;
                this.DownloadPath = path;
                this.Raise("DownloadPath");
            }
        }

        private void HandleChangeDownloadPath(object sender, RoutedEventArgs e)
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                dialog.Title = "ine";
                dialog.IsFolderPicker = true;
                dialog.InitialDirectory = this.model.DownloadPath;
                dialog.AddToMostRecentlyUsedList = false;
                dialog.AllowNonFileSystemItems = false;
                dialog.DefaultDirectory = this.model.DownloadPath;
                dialog.EnsureFileExists = true;
                dialog.EnsurePathExists = true;
                dialog.EnsureReadOnly = false;
                dialog.EnsureValidNames = true;
                dialog.Multiselect = false;
                dialog.ShowPlacesList = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    this.model.SetDownloadPath(dialog.FileName);
                    this.OnConfiguration.Invoke(this.model.Configuration);
                }
            }
        }
    }
}
