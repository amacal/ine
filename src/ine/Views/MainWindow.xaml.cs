using System;
using System.Windows;

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
            await this.transfer.Solve(this.captcha.Solve);
        }
    }
}
