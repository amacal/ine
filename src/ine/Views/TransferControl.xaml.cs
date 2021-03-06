﻿using ine.Core;
using ine.Domain;
using ine.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace ine.Views
{
    public partial class TransferControl : UserControl
    {
        private readonly ControlModel model;

        public TransferControl()
        {
            this.model = new ControlModel();
            this.DataContext = this.model;

            this.InitializeComponent();
        }

        public Action<LogEntry> OnLog { get; set; }
        public Func<Resource[], Task> OnResources { get; set; }
        public Func<Captcha, Task<string>> OnCaptcha { get; set; }

        public bool IsWorking()
        {
            return this.model.IsWorking();
        }

        public void SetConfiguration(Configuration configuration)
        {
            this.model.SetConfiguration(configuration);
        }

        public void SetResources(Resource[] resources)
        {
            this.model.AddResources(resources);
        }

        public async Task StopAll(CancellationToken cancellation)
        {
            this.Stop(this.model.GetStoppable());

            while (this.model.IsWorking() == true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellation);
            }
        }

        public class ControlModel : ViewModelBase
        {
            private ICollectionView resources;

            public ControlModel()
            {
                this.Scheduler = new Scheduler();
                this.Resources = new ObservableCollection<ResourceModel>();
                this.resources = CollectionViewSource.GetDefaultView(this.Resources);
            }

            public Scheduler Scheduler { get; set; }
            public Configuration Configuration { get; set; }
            public ObservableCollection<ResourceModel> Resources { get; set; }

            public bool CanStart { get; set; }
            public bool CanStop { get; set; }
            public bool CanRemove { get; set; }

            public double VerticalOffset { get; set; }

            public bool IsWorking()
            {
                return this.Resources.Any(x => x.IsWorking() == true);
            }

            public Resource[] GetStartable()
            {
                return this.Resources.Where(x => x.IsWorking() == false && x.IsCompleted == false).Select(x => x.Source).ToArray();
            }

            public Resource[] GetStoppable()
            {
                return this.Resources.Where(x => x.IsWorking() == true).Select(x => x.Source).ToArray();
            }

            public Resource[] GetRemovable()
            {
                return this.Resources.Select(x => x.Source).ToArray();
            }

            public Resource[] GetPersistable()
            {
                return this.Resources.Where(x => x.IsCompleted == false).Select(x => x.Source).ToArray();
            }

            public void AddResources(Resource[] resources)
            {
                foreach (ResourceModel model in resources.Where(this.NotContain).Select(this.Create).ToArray())
                {
                    this.Resources.Add(model);
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void RemoveResources(Resource[] resources)
            {
                foreach (ResourceModel model in this.Resources.ToArray())
                {
                    if (resources.Contains(model.Source) == true)
                    {
                        this.Resources.Remove(model);
                    }
                }

                this.RecalculateButtons();
                this.UpdateButtons();
            }

            public void SetConfiguration(Configuration configuration)
            {
                this.Configuration = configuration;
            }

            public void Filter(string text)
            {
                if (String.IsNullOrWhiteSpace(text) == false)
                {
                    this.resources.Filter = data =>
                    {
                        return data is ResourceModel && ((ResourceModel)data).Filter(text) == true;
                    };
                }
                else
                {
                    this.resources.Filter = null;
                }
            }

            private bool NotContain(Resource resouce)
            {
                return this.Resources.Any(y => y.Source.Url == resouce.Url) == false;
            }

            private ResourceModel Create(Resource resource)
            {
                return ResourceModel.FromResource(resource, this);
            }

            public ResourceModel GetModel(Resource resource)
            {
                return this.Resources.Single(x => x.Source == resource);
            }

            public void RecalculateButtons()
            {
                this.CanStart = this.Resources.Any(x => x.IsWorking() == false);
                this.CanStop = this.Resources.Any(x => x.IsWorking() == true);
                this.CanRemove = this.Resources.Any();
            }

            public void UpdateButtons()
            {
                this.Raise("CanStart");
                this.Raise("CanStop");
                this.Raise("CanRemove");
            }
        }

        public class ResourceModel : ViewModelBase
        {
            public ControlModel Owner { get; set; }
            public Resource Source { get; set; }
            public CancellationTokenSource Cancellation { get; set; }

            public bool IsCompleted { get; set; }

            public string Name { get; set; }
            public string Hosting { get; set; }
            public string Size { get; set; }
            public string Status { get; set; }
            public string Completed { get; set; }
            public string Speed { get; set; }
            public string Estimation { get; set; }

            public void SetStatus(string value)
            {
                this.Status = value;
                this.Raise("Status");
            }

            public void SetProgress(long received, long total)
            {
                string current = String.Format("{0:f2}%", 100.0 * received / total);

                if (current != this.Completed)
                {
                    this.Completed = current;
                    this.Raise("Completed");
                }
            }

            public void SetSpeed(long speed)
            {
                string current = String.Format("{0:f2} kB/s", 1.0 * speed / 1024);

                if (current != this.Speed)
                {
                    this.Speed = current;
                    this.Raise("Speed");
                }
            }

            public void SetEstimation(TimeSpan time)
            {
                string current = String.Format(@"{0:d\.hh\:mm\:ss}", time);

                if (time.TotalMinutes >= 1.0)
                {
                    current = current.TrimStart('0', '.', ':');
                }
                else
                {
                    current = current.Substring(current.Length - 4, 4);
                }

                if (current != this.Estimation)
                {
                    this.Estimation = current;
                    this.Raise("Estimation");
                }
            }

            public void Start(CancellationTokenSource cancellation)
            {
                this.Cancellation = cancellation;
                this.IsCompleted = false;

                this.Owner.RecalculateButtons();
                this.Owner.UpdateButtons();
            }

            public void Complete(bool result)
            {
                this.Cancellation = null;
                this.Estimation = null;
                this.Raise("Estimation");

                if (result == true)
                {
                    this.IsCompleted = true;
                }

                this.Owner.RecalculateButtons();
                this.Owner.UpdateButtons();
            }

            public bool IsWorking()
            {
                return this.Cancellation != null;
            }

            public bool Filter(string text)
            {
                return this.Name.ToLower().Contains(text.ToLower());
            }

            public static ResourceModel FromResource(Resource resource, ControlModel owner)
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

        private async void HandleNew(object sender, RoutedEventArgs e)
        {
            Resource[] resources = NewWindow.Show(Application.Current.MainWindow);

            if (resources.Length > 0)
            {
                this.OnLog.Information("Adding {0} item(s).", resources.Length);
            }

            this.model.AddResources(resources);
            await this.Persist();
        }

        private async void HandleStart(object sender, RoutedEventArgs e)
        {
            string path = this.model.Configuration.DownloadPath;
            Resource[] resources = StartWindow.Show(Application.Current.MainWindow, this.model.GetStartable());

            if (resources.Length > 0)
            {
                this.OnLog.Information("Scheduling {0} item(s).", resources.Length);
            }

            if (String.IsNullOrEmpty(path) == true)
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            foreach (Resource resource in resources)
            {
                CancellationTokenSource cancellation = new CancellationTokenSource();
                ResourceModel model = this.model.GetModel(resource);
                ResourceTask task = new ResourceTask
                {
                    Url = model.Source.Url,
                    Hosting = model.Source.Hosting,
                    Destination = Path.Combine(path, model.Source.Name),
                    Scheduler = this.model.Scheduler,
                    Cancellation = cancellation.Token,
                    OnCaptcha = GetSolver(Application.Current.Dispatcher),
                    OnLog = Log(Application.Current.Dispatcher),
                    OnStatus = SetStatus(Application.Current.Dispatcher, model),
                    OnProgress = SetProgress(Application.Current.Dispatcher, model),
                    OnSpeed = SetSpeed(Application.Current.Dispatcher, model),
                    OnEstimation = SetEstimation(Application.Current.Dispatcher, model),
                    OnCompleted = Complete(Application.Current.Dispatcher, model)
                };

                new Facade().Download(task);
                model.Start(cancellation);
            }

            await this.Persist();
        }

        private void HandleStop(object sender, RoutedEventArgs e)
        {
            Resource[] resources = StopWindow.Show(Application.Current.MainWindow, this.model.GetStoppable());

            if (resources.Length > 0)
            {
                this.OnLog.Information("Stopping {0} item(s).", resources.Length);
            }

            this.Stop(resources);
        }

        private void Stop(Resource[] resources)
        {
            foreach (Resource resource in resources)
            {
                ResourceModel model = this.model.GetModel(resource);
                CancellationTokenSource cancellation = model.Cancellation;

                if (cancellation != null)
                {
                    cancellation.Cancel();
                }
            }
        }

        private async void HandleRemove(object sender, RoutedEventArgs e)
        {
            Resource[] resources = RemoveWindow.Show(Application.Current.MainWindow, this.model.GetRemovable());

            if (resources.Length > 0)
            {
                this.OnLog.Information("Removing {0} item(s).", resources.Length);
            }

            foreach (Resource resource in resources)
            {
                ResourceModel model = this.model.GetModel(resource);
                CancellationTokenSource cancellation = model.Cancellation;

                if (cancellation != null)
                {
                    cancellation.Cancel();
                }
            }

            this.model.RemoveResources(resources);
            await this.Persist();
        }

        private void HandleFilterChanged(object sender, TextChangedEventArgs e)
        {
            this.model.Filter(this.filter.Text);
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            this.list.ScrollToVerticalOffset(this.model.VerticalOffset);
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            this.model.VerticalOffset = this.list.GetVerticalOffset();
        }


        private Action<string> SetStatus(Dispatcher dispatcher, ResourceModel model)
        {
            return status =>
            {
                dispatcher.Handle(() => model.SetStatus(status));
            };
        }

        private Action<long, long> SetProgress(Dispatcher dispatcher, ResourceModel model)
        {
            return (received, total) =>
            {
                dispatcher.Handle(() => model.SetProgress(received, total));
            };
        }

        private Action<long> SetSpeed(Dispatcher dispatcher, ResourceModel model)
        {
            return speed =>
            {
                dispatcher.Handle(() => model.SetSpeed(speed));
            };
        }

        private Action<TimeSpan> SetEstimation(Dispatcher dispatcher, ResourceModel model)
        {
            return estimation =>
            {
                dispatcher.Handle(() => model.SetEstimation(estimation));
            };
        }

        private Action<bool> Complete(Dispatcher dispatcher, ResourceModel model)
        {
            return result =>
            {
                dispatcher.Handle(async () =>
                {
                    model.Complete(result);
                    await this.Persist();
                });
            };
        }

        private Action<LogEntry> Log(Dispatcher dispatcher)
        {
            return entry =>
            {
                dispatcher.Handle(() => this.OnLog.Invoke(entry));
            };
        }

        private Func<Captcha, Task<string>> GetSolver(Dispatcher dispatcher)
        {
            return captcha =>
            {
                TaskCompletionSource<string> completion = new TaskCompletionSource<string>();

                dispatcher.Handle(async () =>
                {
                    string solution = await this.OnCaptcha.Invoke(captcha);
                    if (String.IsNullOrWhiteSpace(solution) == false)
                    {
                        completion.SetResult(solution);
                    }
                    else
                    {
                        completion.TrySetCanceled(captcha.Cancellation);
                    }
                });

                return completion.Task;
            };
        }

        private Task Persist()
        {
            return this.OnResources.Invoke(this.model.GetPersistable());
        }
    }
}
