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

        public async Task Solve(Func<byte[], CancellationToken, Task<string>> solver)
        {
            if (this.model.Captchas.Count > 0)
            {
                CaptchaModel captcha = this.model.Captchas[0];

                try
                {
                    captcha.Solution = await solver.Invoke(captcha.Data, captcha.Cancellation);
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                finally
                {
                    this.model.Captchas.Remove(captcha);
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

            public void AddResources(Resource[] resources)
            {
                this.Resources = this.Resources.Concat((resources.Where(x => this.Resources.Any(y => y.Source.Url == x.Url) == false)).Select(ResourceModel.FromResource)).ToArray();
                this.Raise("Resources");
            }
        }

        public class CaptchaModel
        {
            public DateTime Inserted { get; set; }
            public byte[] Data { get; set; }
            public string Solution { get; set; }
            public CancellationToken Cancellation { get; set; }
        }

        public class ResourceModel : ViewModelBase
        {
            public Resource Source { get; set; }

            public string Name { get; set; }
            public string Hosting { get; set; }
            public string Size { get; set; }
            public string Status { get; set; }
            public string Completed { get; set; }
            public string Speed { get; set; }

            public void SetStatus(string value)
            {
                this.Status = value;
                this.Raise("Status");
            }

            public void SetProgress(long received, long total)
            {
                this.Completed = String.Format("{0:f2}%", 100.0 * received / total);
                this.Raise("Completed");
            }

            public void SetSpeed(long speed)
            {
                this.Speed = String.Format("{0:f2} kB/s", 1.0 * speed / 1024);
                this.Raise("Speed");
            }

            public static ResourceModel FromResource(Resource link)
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

        private void HandleAddLinks(object sender, RoutedEventArgs e)
        {
            this.model.AddResources(NewWindow.Show(Application.Current.MainWindow));
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ResourceModel model = (ResourceModel)((FrameworkElement)sender).DataContext;

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
                OnSpeed = SetSpeed(Application.Current.Dispatcher, model)
            };

            await new Facade().Download(task);
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

        private Func<byte[], CancellationToken, Task<string>> GetSolver(Dispatcher dispatcher)
        {
            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();

            return (data, cancellation) =>
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    EventHandler<ItemEventArgs<CaptchaModel>> handler = null;
                    CaptchaModel captcha = new CaptchaModel
                    {
                        Data = data,
                        Inserted = DateTime.Now,
                        Cancellation = cancellation
                    };

                    handler = (sender, args) =>
                    {
                        if (args.Item == captcha)
                        {
                            this.model.Captchas.ItemRemoved -= handler;

                            if (String.IsNullOrWhiteSpace(captcha.Solution) == false)
                            {
                                completion.SetResult(args.Item.Solution);
                            }
                            else
                            {
                                completion.TrySetCanceled(cancellation);
                            }
                        }
                    };

                    this.model.Captchas.ItemRemoved += handler;
                    this.model.Captchas.Add(captcha);

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
