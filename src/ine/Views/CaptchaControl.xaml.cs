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
            Action dispose = null, initialize = null;
            EventHandler timerHandler = null;
            RoutedEventHandler solveHandler = null;
            RoutedEventHandler reloadHandler = null;
            RoutedEventHandler cancelHandler = null;

            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            initialize = () =>
            {
                this.panel.Visibility = Visibility.Visible;
                this.captchaImage.Source = this.ApplyImage(captcha);
                this.solve.Click += solveHandler;
                this.solve.IsEnabled = false;
                this.reload.Click += reloadHandler;
                this.reload.IsEnabled = true;
                this.cancel.Click += cancelHandler;
                this.cancel.IsEnabled = true;

                timer.Tick += timerHandler;
                timer.Start();
            };

            dispose = () =>
            {
                this.panel.Visibility = Visibility.Collapsed;

                timer.Tick -= timerHandler;
                timer.Stop();

                this.solve.Click -= solveHandler;
                this.reload.Click -= reloadHandler;
                this.cancel.Click -= cancelHandler;
                this.captchaImage.Source = null;
                this.captchText.Clear();
            };

            solveHandler = (sender, args) =>
            {
                string solution = this.captchText.Text;

                dispose.Invoke();
                completion.SetResult(solution.Trim());
            };

            reloadHandler = async (sender, args) =>
            {
                this.reload.IsEnabled = false;
                this.cancel.IsEnabled = false;
                this.solve.IsEnabled = this.CanBeSolved();

                try
                {
                    await captcha.Reload();
                    this.captchaImage.Source = this.ApplyImage(captcha);
                }
                finally
                {
                    this.reload.IsEnabled = true;
                    this.cancel.IsEnabled = true;
                    this.solve.IsEnabled = this.CanBeSolved();
                }
            };

            cancelHandler = (sender, args) =>
            {
                dispose.Invoke();
                completion.SetResult(null);
            };

            timerHandler = (sender, args) =>
            {
                if (captcha.Cancellation.IsCancellationRequested == true)
                {
                    dispose.Invoke();
                    completion.TrySetCanceled(captcha.Cancellation);
                }
            };

            initialize.Invoke();
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

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            this.solve.IsEnabled = this.CanBeSolved();
        }

        private bool CanBeSolved()
        {
            return this.reload.IsEnabled && String.IsNullOrWhiteSpace(this.captchText.Text) == false;
        }
    }
}
