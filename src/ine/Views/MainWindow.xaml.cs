using System;
using System.Windows;
using System.Windows.Media;

namespace ine.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void HandleCaptcha(object sender, EventArgs e)
        {
            Brush previous = this.captchaHeader.Foreground;

            this.captchaHeader.Foreground = Brushes.Red;
            await this.transfer.Solve(this.captcha.Solve);
            this.captchaHeader.Foreground = previous;
        }
    }
}
