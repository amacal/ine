using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ine.Views
{
    public partial class CloseWindow : Window
    {
        public CloseWindow()
        {
            this.InitializeComponent();
        }

        public static bool Show(Window owner)
        {
            CloseWindow view = new CloseWindow();

            view.Owner = owner;
            view.ShowDialog();

            return view.DialogResult == true;
        }

        private void HandleYes(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void HandleNo(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = this.DialogResult == null;
            base.OnClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
    }
}
