using ine.Core;
using ine.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ine.Views
{
    public partial class LinkListControl : UserControl
    {
        private readonly ControlModel model;

        public LinkListControl()
        {
            this.model = new ControlModel();
            this.DataContext = this.model;

            this.InitializeComponent();
        }

        public event EventHandler Back;
        public event EventHandler Next;

        public async Task SetText(string text)
        {
            Facade facade = new Facade();
            Dispatcher dispatcher = Application.Current.Dispatcher;

            LinkTask task = new LinkTask
            {
                OnStatus = (link, status) =>
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.model.SetStatus(link, status);
                    }));
                },
                OnCompleted = (link, resource) =>
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.model.Complete(link, resource);
                    }));
                }
            };

            this.model.ClearLinks();
            this.model.SetLinks(await facade.ParseTextToLinks(text));

            task.Links = this.model.GetAnalyzableLinks();
            await facade.Analyze(task);
        }

        public Resource[] GetResources()
        {
            return this.model.GetSelectedResources();
        }

        public class ControlModel : ViewModelBase
        {
            private readonly Dictionary<Uri, LinkModel> cache;

            public ControlModel()
            {
                this.CanBack = true;
                this.CanInvert = true;

                this.cache = new Dictionary<Uri, LinkModel>();
                this.Links = new LinkModel[0];
            }

            public LinkModel[] Links { get; set; }

            public bool CanNext { get; set; }
            public bool CanBack { get; set; }

            public bool CanSelectAll { get; set; }
            public bool CanInvert { get; set; }
            public bool CanUnselectAll { get; set; }

            public void ClearLinks()
            {
                this.Links = new LinkModel[0];
                this.Raise("Links");

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void SetLinks(Link[] links)
            {
                this.Links = links.Select(this.GetOrCreate).ToArray();
                this.Raise("Links");

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            private LinkModel GetOrCreate(Link link)
            {
                LinkModel model;

                if (this.cache.TryGetValue(link.Url, out model) == true)
                {
                    return model;
                }

                model = LinkModel.FromLink(link, this);
                this.cache[link.Url] = model;

                return model;
            }

            public Link[] GetAnalyzableLinks()
            {
                return this.Links.Where(x => x.IsAnalyzed() == false).Select(x => x.Link).ToArray();
            }

            public Resource[] GetSelectedResources()
            {
                return this.Links.Where(x => x.Selected == true).Select(x => x.Resource).ToArray();
            }

            public void SelectAll()
            {
                foreach (LinkModel model in this.Links)
                {
                    model.Select(true);
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void Invert()
            {
                foreach (LinkModel model in this.Links)
                {
                    model.Select(model.Selected == false);
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void UnselectAll()
            {
                foreach (LinkModel model in this.Links)
                {
                    model.Select(false);
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void SetStatus(Link link, string status)
            {
                foreach (LinkModel model in this.Links)
                {
                    if (model.Link.Url == link.Url)
                    {
                        model.SetStatus(status);
                    }
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void Complete(Link link, Resource resource)
            {
                foreach (LinkModel model in this.Links)
                {
                    if (model.Link.Url == link.Url)
                    {
                        model.Complete(resource);
                    }
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void RecalculateButtons()
            {
                this.CanSelectAll = this.Links.Any(x => x.Selected == false);
                this.CanUnselectAll = this.Links.Any(x => x.Selected == true);
                this.CanNext = this.Links.Any(x => x.Selected == true) && this.Links.Where(x => x.Selected == true).All(x => x.Resource != null);
            }

            public void UpdateButtons()
            {
                this.Raise("CanNext");
                this.Raise("CanBack");

                this.Raise("CanSelectAll");
                this.Raise("CanInvert");
                this.Raise("CanUnselectAll");
            }
        }

        public class LinkModel : ViewModelBase
        {
            public ControlModel Owner { get; set; }
            public Link Link { get; set; }
            public Resource Resource { get; set; }

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
            public string Status { get; set; }

            public bool IsAnalyzed()
            {
                return this.Resource != null;
            }

            public void Select(bool value)
            {
                this.selected = value;
                this.Raise("Selected");
            }

            public void SetStatus(string status)
            {
                this.Status = status;
                this.Raise("Status");
            }

            public void Complete(Resource resource)
            {
                this.Resource = resource;
                this.Name = resource.Name;
                this.Size = resource.Size;

                this.Raise("Name");
                this.Raise("Size");
            }

            public static LinkModel FromLink(Link link, ControlModel owner)
            {
                return new LinkModel
                {
                    Owner = owner,
                    Link = link,
                    Name = link.Url.ToString(),
                    Hosting = link.Hosting
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

        private void HandleNext(object sender, RoutedEventArgs e)
        {
            if (this.Next != null)
            {
                this.Next.Invoke(this, EventArgs.Empty);
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
    }
}
