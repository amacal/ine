﻿using ine.Domain;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading;
using System;

namespace ine.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.transfer.OnLog = this.HandleLog;
            this.transfer.OnCaptcha = this.HandleCaptcha;
        }

        private void HandleLog(LogEntry entry)
        {
            this.logging.AddLog(entry);
            new Facade().PersistLogs(entry);
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

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (this.transfer.IsWorking() == true)
            {
                e.Cancel = CloseWindow.Show(this) == false;
            }

            if (this.transfer.IsWorking() == true && e.Cancel == false)
            {
                TimeSpan timeout = TimeSpan.FromSeconds(10);
                CancellationTokenSource cancellation = new CancellationTokenSource();

                e.Cancel = true;
                await this.transfer.StopAll(cancellation.Token);
                this.Close();
            }

            base.OnClosing(e);
        }
    }
}
