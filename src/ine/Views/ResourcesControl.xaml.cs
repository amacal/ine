using ine.Core;
using ine.Domain;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ine.Views
{
    public partial class ResourceListControl : UserControl
    {
        private readonly ControlModel model;

        public ResourceListControl()
        {
            this.model = new ControlModel();
            this.DataContext = this.model;

            this.InitializeComponent();
        }

        public event EventHandler Back;
        public event EventHandler Schedule;

        public void SetResources(Resource[] links)
        {
            this.model.SetResources(links);
        }

        public Resource[] GetResources()
        {
            return this.model.GetResources();
        }

        public class ControlModel : ViewModelBase
        {
            public ControlModel()
            {
                this.CanBack = true;
                this.Resources = new ResourceModel[0];
            }

            public ResourceModel[] Resources { get; set; }

            public bool CanSchedule { get; set; }
            public bool CanBack { get; set; }

            public Resource[] GetResources()
            {
                return this.Resources.Select(x => x.Source).ToArray();
            }

            public void SetResources(Resource[] links)
            {
                this.Resources = links.Select(ResourceModel.FromLink).ToArray();
                this.Raise("Resources");
            }
        }

        public class ResourceModel : ViewModelBase
        {
            public Resource Source { get; set; }

            public string Name { get; set; }
            public string Hosting { get; set; }
            public string Size { get; set; }

            public static ResourceModel FromLink(Resource link)
            {
                return new ResourceModel
                {
                    Source = link,
                    Name = link.Name,
                    Hosting = link.Hosting,
                    Size = link.Size
                };
            }
        }

        private void HandleBack(object sender, RoutedEventArgs e)
        {
            if (this.Back != null)
            {
                this.Back.Invoke(this, EventArgs.Empty);
            }
        }

        private void HandleSchedule(object sender, RoutedEventArgs e)
        {
            if (this.Schedule != null)
            {
                this.Schedule.Invoke(this, EventArgs.Empty);
            }
        }
    }
}