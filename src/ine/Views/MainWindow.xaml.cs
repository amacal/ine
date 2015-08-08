using ine.Domain;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ine.Views
{
    public partial class MainWindow : Window
    {
        private readonly WindowViewModel model;
        private readonly Scheduler scheduler;

        public MainWindow()
        {
            this.model = new WindowViewModel();
            this.scheduler = new Scheduler();

            this.DataContext = this.model;
            this.InitializeComponent();
        }

        private void HandleAddLinks(object sender, RoutedEventArgs e)
        {
            this.model.AddLinks(NewWindow.Show(this));
        }

        public class WindowViewModel : INotifyPropertyChanged
        {
            public WindowViewModel()
            {
                this.Links = new LinkViewModel[0];
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public LinkViewModel[] Links { get; set; }

            public void AddLinks(Resource[] links)
            {
                this.Links = this.Links.Concat((links.Where(x => this.Links.Any(y => y.Source.Url == x.Url) == false)).Select(LinkViewModel.FromLink)).ToArray();

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Links"));
                }
            }
        }

        public class LinkViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

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

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Status"));
                }
            }

            public void SetProgress(long received, long total)
            {
                this.Completed = String.Format("{0:f2}%", 100.0 * received / total);

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Completed"));
                }
            }

            public void SetSpeed(long speed)
            {
                this.Speed = String.Format("{0:f2} kB/s", 1.0 * speed / 1024);

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Speed"));
                }
            }

            public static LinkViewModel FromLink(Resource link)
            {
                return new LinkViewModel
                {
                    Source = link,
                    Name = link.Name,
                    Hosting = link.Hosting,
                    Size = link.Size
                };
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            LinkViewModel model = (LinkViewModel)((FrameworkElement)sender).DataContext;

            ResourceTask task = new ResourceTask
            {
                Url = model.Source.Url,
                Hosting = model.Source.Hosting,
                Destination = Path.Combine(path, model.Source.Name),
                Scheduler = this.scheduler,
                Cancellation = new CancellationToken(),
                OnCaptcha = GetSolver(Application.Current.Dispatcher),
                OnStatus = SetStatus(Application.Current.Dispatcher, model),
                OnProgress = SetProgress(Application.Current.Dispatcher, model),
                OnSpeed = SetSpeed(Application.Current.Dispatcher, model)
            };

            await new Facade().Download(task);
        }

        private Action<string> SetStatus(Dispatcher dispatcher, LinkViewModel model)
        {
            return status =>
            {
                dispatcher.BeginInvoke(new Action(() => model.SetStatus(status)));
            };
        }

        private Action<long, long> SetProgress(Dispatcher dispatcher, LinkViewModel model)
        {
            return (received, total) =>
            {
                dispatcher.BeginInvoke(new Action(() => model.SetProgress(received, total)));
            };
        }

        private Action<long> SetSpeed(Dispatcher dispatcher, LinkViewModel model)
        {
            return speed =>
            {
                dispatcher.BeginInvoke(new Action(() => model.SetSpeed(speed)));
            };
        }

        private Func<byte[], Task<string>> GetSolver(Dispatcher dispatcher)
        {
            return data =>
            {
                TaskCompletionSource<string> completion = new TaskCompletionSource<string>();

                dispatcher.BeginInvoke(new Action(() =>
                {
                    var image = new BitmapImage();
                    using (var memory = new MemoryStream(data))
                    {
                        memory.Position = 0;
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.UriSource = null;
                        image.StreamSource = memory;
                        image.EndInit();
                    }

                    image.Freeze();

                    RoutedEventHandler handler = null;

                    handler = (sender, args) =>
                    {
                        string captcha = this.captchText.Text;

                        this.solveButton.Click -= handler;
                        this.captchaImage.Source = null;
                        this.captchText.Clear();

                        completion.SetResult(captcha);
                    };

                    this.captchaImage.Source = image;
                    this.solveButton.Click += handler;
                }));

                return completion.Task;
            };
        }
    }
}
