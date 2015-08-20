using ine.Domain;
using ine.Extensions;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            KeyEventHandler keyHandler = null;
            RoutedEventHandler playHandler = null;
            RoutedEventHandler solveHandler = null;
            RoutedEventHandler reloadHandler = null;
            RoutedEventHandler cancelHandler = null;
            RoutedEventHandler toAudioHandler = null;
            RoutedEventHandler toTextHandler = null;

            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            initialize = () =>
            {
                this.panel.Visibility = Visibility.Visible;

                this.Refresh(captcha);
                this.ApplyButtons(captcha);

                this.play.Click += playHandler;
                this.solve.Click += solveHandler;
                this.solve.IsEnabled = false;
                this.reload.Click += reloadHandler;
                this.reload.IsEnabled = true;
                this.cancel.Click += cancelHandler;
                this.cancel.IsEnabled = true;
                this.toAudio.Click += toAudioHandler;
                this.toText.Click += toTextHandler;
                this.text.KeyDown += keyHandler;

                timer.Tick += timerHandler;
                timer.Start();
            };

            dispose = () =>
            {
                this.panel.Visibility = Visibility.Collapsed;

                timer.Tick -= timerHandler;
                timer.Stop();

                this.play.Click -= playHandler;
                this.solve.Click -= solveHandler;
                this.reload.Click -= reloadHandler;
                this.cancel.Click -= cancelHandler;
                this.toAudio.Click -= toAudioHandler;
                this.toText.Click -= toTextHandler;
                this.text.KeyDown -= keyHandler;

                this.image.Source = null;
                this.text.Clear();
            };

            playHandler = async (sender, args) =>
            {
                this.Lock();

                try
                {
                    await captcha.Play();
                }
                finally
                {
                    this.Unlock();
                }
            };

            solveHandler = (sender, args) =>
            {
                completion.SetResult(this.text.Text.Trim());
                dispose.Invoke();
            };

            keyHandler = (sender, args) =>
            {
                if (this.solve.IsEnabled == true && args.Key == Key.Enter)
                {
                    completion.SetResult(this.text.Text.Trim());
                    dispose.Invoke();
                }
            };

            reloadHandler = async (sender, args) =>
            {
                this.Lock();

                try
                {
                    await captcha.Reload.Invoke();
                    this.Refresh(captcha);
                }
                finally
                {
                    this.Unlock();
                }
            };

            toAudioHandler = async (sender, args) =>
            {
                this.Lock();

                try
                {
                    await captcha.ToAudio.Invoke();
                    this.Refresh(captcha);
                    this.ApplyButtons(captcha);
                }
                finally
                {
                    this.Unlock();
                }
            };

            toTextHandler = async (sender, args) =>
            {
                this.Lock();

                try
                {
                    await captcha.ToImage.Invoke();
                    this.Refresh(captcha);
                    this.ApplyButtons(captcha);
                }
                finally
                {
                    this.Unlock();
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

        private void ApplyButtons(Captcha captcha)
        {
            int count = 3;

            if (captcha.Type == "image" && captcha.ToAudio != null)
            {
                count++;
                this.toAudio.Visibility = Visibility.Visible;
            }
            else
            {
                this.toAudio.Visibility = Visibility.Collapsed;
            }

            if (captcha.Type == "audio" && captcha.ToImage != null)
            {
                count++;
                this.toText.Visibility = Visibility.Visible;
            }
            else
            {
                this.toText.Visibility = Visibility.Collapsed;
            }

            this.text.Margin = new Thickness(0, 0, 5 + 100 * count, 0);
        }

        private void Lock()
        {
            this.reload.IsEnabled = false;
            this.cancel.IsEnabled = false;
            this.toAudio.IsEnabled = false;
            this.toText.IsEnabled = false;
            this.solve.IsEnabled = this.CanBeSolved();
            this.play.IsEnabled = false;
        }

        private void Unlock()
        {
            this.reload.IsEnabled = true;
            this.cancel.IsEnabled = true;
            this.toAudio.IsEnabled = true;
            this.toText.IsEnabled = true;
            this.solve.IsEnabled = this.CanBeSolved();
            this.play.IsEnabled = true;
        }

        private void Refresh(Captcha captcha)
        {
            if (captcha.Type == "image")
            {
                this.image.Source = captcha.ToBitmap();
                this.play.Visibility = Visibility.Collapsed;
            }

            if (captcha.Type == "audio")
            {
                this.image.Source = null;
                this.play.Visibility = Visibility.Visible;
            }
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            this.solve.IsEnabled = this.CanBeSolved();
        }

        private bool CanBeSolved()
        {
            return this.reload.IsEnabled && String.IsNullOrWhiteSpace(this.text.Text) == false;
        }
    }
}
