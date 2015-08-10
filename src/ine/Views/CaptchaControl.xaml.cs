using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ine.Views
{
    public partial class CaptchaControl : UserControl
    {
        public CaptchaControl()
        {
            this.InitializeComponent();
        }

        public Task<string> Solve(byte[] data, CancellationToken cancellation)
        {
            EventHandler timerHandler = null;
            RoutedEventHandler solveHandler = null;

            BitmapImage image = new BitmapImage();
            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            using (var memory = new MemoryStream(data))
            {
                memory.Seek(0, SeekOrigin.Begin);

                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = memory;
                image.EndInit();
                image.Freeze();
            }

            solveHandler = (sender, args) =>
            {
                string captcha = this.captchText.Text;

                timer.Tick -= timerHandler;
                timer.Stop();

                this.solveButton.Click -= solveHandler;
                this.captchaImage.Source = null;
                this.captchText.Clear();

                completion.SetResult(captcha);
            };

            timerHandler = (sender, args) =>
            {
                if (cancellation.IsCancellationRequested == true)
                {
                    timer.Tick -= timerHandler;
                    timer.Stop();

                    this.solveButton.Click -= solveHandler;
                    this.captchaImage.Source = null;
                    this.captchText.Clear();

                    completion.TrySetCanceled(cancellation);
                }
            };

            this.captchaImage.Source = image;
            this.solveButton.Click += solveHandler;

            timer.Tick += timerHandler;
            timer.Start();

            return completion.Task;
        }
    }
}
