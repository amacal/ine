using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ine.Views
{
    public partial class CaptchaControl : UserControl
    {
        public CaptchaControl()
        {
            this.InitializeComponent();
        }

        public Task<string> Solve(byte[] data)
        {
            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();

            this.Dispatcher.BeginInvoke(new Action(() =>
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
        }
    }
}
