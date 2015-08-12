using ine.Domain;
using System;
using System.Windows;

namespace ine.Views
{
    public partial class NewWindow : Window
    {
        private readonly PasteControl pasteContent;
        private readonly LinkListControl linkListContent;
        private readonly ResourceListControl resourceListContent;

        private readonly WindowModel model;

        public NewWindow()
        {
            this.model = new WindowModel();

            this.DataContext = this.model;
            this.InitializeComponent();

            this.pasteContent = (PasteControl)this.data.Content;
            this.pasteContent.OnLog = this.logging.AddLog;

            this.linkListContent = (LinkListControl)this.Resources["Select"];
            this.linkListContent.OnLog = this.logging.AddLog;

            this.resourceListContent = (ResourceListControl)this.Resources["Confirm"];
            this.resourceListContent.OnLog = this.logging.AddLog;
        }

        public static Resource[] Show(Window owner)
        {
            NewWindow view = new NewWindow();

            view.Owner = owner;
            view.ShowDialog();

            if (view.DialogResult == true)
            {
                return view.model.Resources;
            }

            return new Resource[0];
        }

        public class WindowModel
        {
            public WindowModel()
            {
                this.Resources = new Resource[0];
            }

            public string Text { get; set; }

            public Resource[] Resources { get; set; }
        }

        private async void PasteControl_GoNext(object sender, EventArgs e)
        {
            this.model.Text = this.pasteContent.Text;
            this.data.Content = this.linkListContent;

            await this.linkListContent.SetText(this.model.Text);
        }

        private void LinkListControl_Back(object sender, EventArgs e)
        {
            this.data.Content = this.pasteContent;
        }

        private void LinkListControl_Next(object sender, EventArgs e)
        {
            this.model.Resources = this.linkListContent.GetResources();
            this.data.Content = this.resourceListContent;
            this.resourceListContent.SetResources(this.model.Resources);
        }

        private void ResourceListControl_Back(object sender, EventArgs e)
        {
            this.data.Content = this.linkListContent;
        }

        private void ResourceListControl_Schedule(object sender, EventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
