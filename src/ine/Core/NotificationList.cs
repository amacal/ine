using System;
using System.Collections.ObjectModel;

namespace ine.Core
{
    public class NotificationList<T> : Collection<T>
    {
        public event EventHandler<ItemEventArgs<T>> ItemInserted;
        public event EventHandler<ItemEventArgs<T>> ItemRemoved;

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            this.RaiseItemInserted(index, item);
        }

        private void RaiseItemInserted(int index, T item)
        {
            if (ItemInserted != null)
            {
                ItemInserted(this, new ItemEventArgs<T>(index, item));
            }
        }

        protected override void RemoveItem(int index)
        {
            T item = this[index];

            base.RemoveItem(index);
            this.RaiseItemRemoved(index, item);
        }

        private void RaiseItemRemoved(int index, T item)
        {
            if (ItemRemoved != null)
            {
                ItemRemoved(this, new ItemEventArgs<T>(index, item));
            }
        }
    }

    public class ItemEventArgs<T> : EventArgs
    {
        public int Index;
        public T Item;

        public ItemEventArgs(int index, T item)
        {
            this.Index = index;
            this.Item = item;
        }
    }
}
