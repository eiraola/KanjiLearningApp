using System;
using System.Collections.Generic;
using Kanji.Core.Navigation;

namespace Kanji.UI
{
    /// <summary>Synchronous logical stack; only the visual hide/show is deferred by the optional transition.</summary>
    public sealed class ScreenNavigator
    {
        private readonly IReadOnlyDictionary<ScreenId, IScreen> _screens;
        private readonly IScreenTransition _transition;
        private readonly NavigationStack<ScreenId> _stack = new NavigationStack<ScreenId>();

        public ScreenNavigator(IReadOnlyDictionary<ScreenId, IScreen> screens, IScreenTransition transition = null)
        {
            _screens = screens ?? throw new ArgumentNullException(nameof(screens));
            _transition = transition;
        }

        public IScreen Current => _stack.Count == 0 ? null : _screens[_stack.Current];
        public bool CanGoBack => _stack.CanPop;
        public event Action<ScreenId> Changed;

        public void Push(ScreenId id, object args = null)
        {
            var from = Current;
            _stack.Push(id);
            Swap(from, args);
        }

        public bool Pop()
        {
            if (!_stack.CanPop) return false;
            var from = Current;
            _stack.TryPop(out _);
            Swap(from, null);
            return true;
        }

        public void Replace(ScreenId id, object args = null)
        {
            var from = Current;
            if (_stack.Count == 0) _stack.Push(id); else _stack.Replace(id);
            Swap(from, args);
        }

        private void Swap(IScreen from, object args)
        {
            var to = Current;
            var toId = _stack.Current;
            Action apply = () =>
            {
                from?.OnHide();
                to.OnShow(args);
                Changed?.Invoke(toId);
            };
            if (from == null || _transition == null) apply(); else _transition.Run(apply);
        }
    }
}
