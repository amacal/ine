using ine.Domain;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ine.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.transfer.OnLog = this.logging.AddLog;
            this.transfer.OnCaptcha = this.HandleCaptcha;
        }

        private async Task<string> HandleCaptcha(Captcha captcha)
        {
            Brush previous = this.captchaHeader.Foreground;
            this.captchaHeader.Foreground = Brushes.Red;

            try
            {
                return await this.captcha.Solve(captcha);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            finally
            {
                this.captchaHeader.Foreground = previous;
            }
        }
    }
}
