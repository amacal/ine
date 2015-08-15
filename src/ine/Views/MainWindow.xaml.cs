using ine.Domain;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

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

        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.transfer.IsWorking() == true)
            {
                e.Cancel = CloseWindow.Show(this) == false;
            }

            base.OnClosing(e);
        }
    }
}
