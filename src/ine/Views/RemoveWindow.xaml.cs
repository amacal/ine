using ine.Core;
using ine.Domain;
using System.Linq;
using System.Windows;

namespace ine.Views
{
    public partial class RemoveWindow : Window
    {
        private readonly WindowModel model;

        public RemoveWindow()
        {
            this.model = new WindowModel();
            this.DataContext = this.model;

            this.InitializeComponent();
        }

        public static Resource[] Show(Window owner, Resource[] resources)
        {
            RemoveWindow view = new RemoveWindow();

            view.model.AddResources(resources);
            view.Owner = owner;
            view.ShowDialog();

            if (view.DialogResult == true)
            {
                return view.model.GetSelected();
            }

            return new Resource[0];
        }

        public class WindowModel : ViewModelBase
        {
            public WindowModel()
            {
                this.CanInvert = true;
            }

            public ResourceModel[] Resources { get; set; }

            public bool CanRemove { get; set; }
            public bool CanSelectAll { get; set; }
            public bool CanInvert { get; set; }
            public bool CanUnselectAll { get; set; }

            public Resource[] GetSelected()
            {
                return this.Resources.Where(x => x.Selected == true).Select(x => x.Source).ToArray();
            }

            public void AddResources(Resource[] resources)
            {
                this.Resources = resources.Select(this.Create).ToArray();
                this.Raise("Resources");

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            private ResourceModel Create(Resource resource)
            {
                return ResourceModel.FromResource(resource, this);
            }

            public void SelectAll()
            {
                foreach (ResourceModel model in this.Resources)
                {
                    model.Select(true);
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void Invert()
            {
                foreach (ResourceModel model in this.Resources)
                {
                    model.Select(model.Selected == false);
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void UnselectAll()
            {
                foreach (ResourceModel model in this.Resources)
                {
                    model.Select(false);
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void RecalculateButtons()
            {
                this.CanSelectAll = this.Resources.Any(x => x.Selected == false);
                this.CanUnselectAll = this.Resources.Any(x => x.Selected == true);
                this.CanRemove = this.Resources.Any(x => x.Selected == true);
            }

            public void UpdateButtons()
            {
                this.Raise("CanSelectAll");
                this.Raise("CanInvert");
                this.Raise("CanUnselectAll");
                this.Raise("CanRemove");
            }
        }

        public class ResourceModel : ViewModelBase
        {
            public WindowModel Owner { get; set; }
            public Resource Source { get; set; }

            private bool selected;
            public bool Selected
            {
                get { return this.selected; }
                set
                {
                    this.selected = value;
                    this.Raise("Selected");

                    this.Owner.RecalculateButtons();
                    this.Owner.UpdateButtons();
                }
            }

            public string Name { get; set; }
            public string Hosting { get; set; }
            public string Size { get; set; }

            public void Select(bool value)
            {
                this.selected = value;
                this.Raise("Selected");
            }

            public static ResourceModel FromResource(Resource resource, WindowModel owner)
            {
                return new ResourceModel
                {
                    Owner = owner,
                    Source = resource,
                    Name = resource.Name,
                    Hosting = resource.Hosting,
                    Size = resource.Size
                };
            }
        }

        private void HandleSelectAll(object sender, RoutedEventArgs e)
        {
            this.model.SelectAll();
        }

        private void HandleInvert(object sender, RoutedEventArgs e)
        {
            this.model.Invert();
        }

        private void HandleUnselectAll(object sender, RoutedEventArgs e)
        {
            this.model.UnselectAll();
        }

        private void HandleStop(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
