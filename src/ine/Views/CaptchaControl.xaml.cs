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
            RoutedEventHandler handler = null;
            BitmapImage image = new BitmapImage();
            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();

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

            return completion.Task;
        }
    }
}
