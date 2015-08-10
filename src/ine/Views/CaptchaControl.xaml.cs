using ine.Domain;
using System;
using System.IO;
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

        public Task<string> Solve(Captcha captcha)
        {
            EventHandler timerHandler = null;
            RoutedEventHandler solveHandler = null;
            RoutedEventHandler reloadHandler = null;

            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            solveHandler = (sender, args) =>
            {
                string solution = this.captchText.Text;

                timer.Tick -= timerHandler;
                timer.Stop();

                this.solve.Click -= solveHandler;
                this.reload.Click -= reloadHandler;
                this.captchaImage.Source = null;
                this.captchText.Clear();

                completion.SetResult(solution);
            };

            reloadHandler = async (sender, args) =>
            {
                await captcha.Reload();
                this.captchaImage.Source = this.ApplyImage(captcha);
            };

            timerHandler = (sender, args) =>
            {
                if (captcha.Cancellation.IsCancellationRequested == true)
                {
                    timer.Tick -= timerHandler;
                    timer.Stop();

                    this.solve.Click -= solveHandler;
                    this.reload.Click -= reloadHandler;
                    this.captchaImage.Source = null;
                    this.captchText.Clear();

                    completion.TrySetCanceled(captcha.Cancellation);
                }
            };

            this.captchaImage.Source = this.ApplyImage(captcha);
            this.solve.Click += solveHandler;
            this.reload.Click += reloadHandler;

            timer.Tick += timerHandler;
            timer.Start();

            return completion.Task;
        }

        private BitmapImage ApplyImage(Captcha captcha)
        {
            BitmapImage image = new BitmapImage();

            using (var memory = new MemoryStream(captcha.Data))
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

            return image;
        }
    }
}
