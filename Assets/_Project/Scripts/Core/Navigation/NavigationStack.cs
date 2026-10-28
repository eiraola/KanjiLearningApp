using System;
using System.Collections.Generic;

namespace Kanji.Core.Navigation
{
    /// <summary>Pure navigation stack. The root item is never popped.</summary>
    public sealed class NavigationStack<T>
    {
        private readonly List<T> _items = new List<T>();

        public int Count => _items.Count;

        public T Current
        {
            get
            {
                if (_items.Count == 0) throw new InvalidOperationException("Navigation stack is empty.");
                return _items[_items.Count - 1];
            }
        }

        public bool CanPop => _items.Count > 1;

        public void Push(T item) => _items.Add(item);

        public bool TryPop(out T popped)
        {
            if (!CanPop)
            {
                popped = default;
                return false;
            }
            popped = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            return true;
        }

        public void Replace(T item)
        {
            if (_items.Count == 0) throw new InvalidOperationException("Navigation stack is empty.");
            _items[_items.Count - 1] = item;
        }

        public void Clear() => _items.Clear();
    }
}
