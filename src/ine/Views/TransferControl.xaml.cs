using ine.Core;
using ine.Domain;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public event EventHandler Captcha;

        public async Task Solve(Func<Captcha, Task<string>> solver)
        {
            if (this.model.Captchas.Count > 0)
            {
                CaptchaModel model = this.model.Captchas[0];

                try
                {
                    model.Solution = await solver.Invoke(model.Captcha);
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                finally
                {
                    this.model.Captchas.Remove(model);
                }
            }
        }

        public class ControlModel : ViewModelBase
        {
            public ControlModel()
            {
                this.Scheduler = new Scheduler();
                this.Resources = new ResourceModel[0];
                this.Captchas = new NotificationList<CaptchaModel>();
            }

            public Scheduler Scheduler { get; set; }
            public ResourceModel[] Resources { get; set; }
            public NotificationList<CaptchaModel> Captchas { get; set; }

            public bool CanStart { get; set; }

            public Resource[] GetStopped()
            {
                return this.Resources.Where(x => x.IsWorking() == false).Select(x => x.Source).ToArray();
            }

            public void AddResources(Resource[] resources)
            {
                this.Resources = this.Resources.Concat(resources.Where(this.NotContain).Select(this.Create)).ToArray();
                this.Raise("Resources");

                this.RecalculateButtons();
                this.UpdateButtons();
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
            }

            public void UpdateButtons()
            {
                this.Raise("CanStart");
            }
        }

        public class CaptchaModel
        {
            public Captcha Captcha { get; set; }
            public string Solution { get; set; }
        }

        public class ResourceModel : ViewModelBase
        {
            public ControlModel Owner { get; set; }
            public Resource Source { get; set; }

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

                this.Owner.RecalculateButtons();
                this.Owner.UpdateButtons();
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
                string current = String.Format(@"{0:d\.hh\:mm\:ss}", time).TrimStart('0', '.', ':');

                if (current != this.Estimation)
                {
                    this.Estimation = current;
                    this.Raise("Estimation");
                }
            }

            public bool IsWorking()
            {
                return String.IsNullOrWhiteSpace(this.Status) == false;
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

        private void HandleNew(object sender, RoutedEventArgs e)
        {
            this.model.AddResources(NewWindow.Show(Application.Current.MainWindow));
        }

        private void HandleStart(object sender, RoutedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Resource[] resources = StartWindow.Show(Application.Current.MainWindow, this.model.GetStopped());

            foreach (Resource resource in resources)
            {
                ResourceModel model = this.model.GetModel(resource);
                ResourceTask task = new ResourceTask
                {
                    Url = model.Source.Url,
                    Hosting = model.Source.Hosting,
                    Destination = Path.Combine(path, model.Source.Name),
                    Scheduler = this.model.Scheduler,
                    Cancellation = new CancellationToken(),
                    OnCaptcha = GetSolver(Application.Current.Dispatcher),
                    OnStatus = SetStatus(Application.Current.Dispatcher, model),
                    OnProgress = SetProgress(Application.Current.Dispatcher, model),
                    OnSpeed = SetSpeed(Application.Current.Dispatcher, model),
                    OnEstimation = SetEstimation(Application.Current.Dispatcher, model)
                };

                new Facade().Download(task);
            }
        }

        private Action<string> SetStatus(Dispatcher dispatcher, ResourceModel model)
        {
            return status =>
            {
                dispatcher.BeginInvoke(new Action(() => model.SetStatus(status)));
            };
        }

        private Action<long, long> SetProgress(Dispatcher dispatcher, ResourceModel model)
        {
            return (received, total) =>
            {
                dispatcher.BeginInvoke(new Action(() => model.SetProgress(received, total)));
            };
        }

        private Action<long> SetSpeed(Dispatcher dispatcher, ResourceModel model)
        {
            return speed =>
            {
                dispatcher.BeginInvoke(new Action(() => model.SetSpeed(speed)));
            };
        }

        private Action<TimeSpan> SetEstimation(Dispatcher dispatcher, ResourceModel model)
        {
            return estimation =>
            {
                dispatcher.BeginInvoke(new Action(() => model.SetEstimation(estimation)));
            };
        }

        private Func<Captcha, Task<string>> GetSolver(Dispatcher dispatcher)
        {
            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();

            return captcha =>
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    EventHandler<ItemEventArgs<CaptchaModel>> handler = null;
                    CaptchaModel model = new CaptchaModel
                    {
                        Captcha = captcha
                    };

                    handler = (sender, args) =>
                    {
                        if (args.Item == model)
                        {
                            this.model.Captchas.ItemRemoved -= handler;

                            if (String.IsNullOrWhiteSpace(model.Solution) == false)
                            {
                                completion.SetResult(args.Item.Solution);
                            }
                            else
                            {
                                completion.TrySetCanceled(captcha.Cancellation);
                            }
                        }
                    };

                    this.model.Captchas.ItemRemoved += handler;
                    this.model.Captchas.Add(model);

                    if (Captcha != null)
                    {
                        Captcha.Invoke(this, EventArgs.Empty);
                    }
                }));

                return completion.Task;
            };
        }
    }
}
