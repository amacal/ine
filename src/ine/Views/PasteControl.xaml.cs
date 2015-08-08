using ine.Core;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ine.Views
{
    public partial class PasteControl : UserControl
    {
        private readonly ControlModel model;

        public PasteControl()
        {
            this.model = new ControlModel();
            this.DataContext = this.model;

            this.InitializeComponent();
        }

        public event EventHandler Next;

        public string Text
        {
            get { return this.model.Text; }
        }

        public class ControlModel : ViewModelBase
        {
            public ControlModel()
            {
                this.Text = String.Empty;
                this.CanPaste = true;
            }

            public string Text { get; set; }

            public bool CanNext { get; set; }
            public bool CanPaste { get; set; }
            public bool CanClear { get; set; }

            public void UpdateButtons()
            {
                this.Raise("CanNext");
                this.Raise("CanClear");
                this.Raise("CanPaste");
            }
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            this.model.Text = this.pasteBox.Text;
            this.model.CanNext = String.IsNullOrWhiteSpace(this.model.Text) == false;
            this.model.CanClear = String.IsNullOrEmpty(this.model.Text) == false;

            this.model.UpdateButtons();
        }

        private void HandleNext(object sender, RoutedEventArgs e)
        {
            if (this.Next != null)
            {
                this.Next.Invoke(this, EventArgs.Empty);
            }
        }

        private void HandlePaste(object sender, RoutedEventArgs e)
        {
            this.pasteBox.Paste();
        }

        private void HandleClear(object sender, RoutedEventArgs e)
        {
            this.pasteBox.Clear();
        }
    }
}
