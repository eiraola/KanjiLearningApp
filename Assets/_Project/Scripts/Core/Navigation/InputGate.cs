using System;

namespace Kanji.Core.Navigation
{
    /// <summary>Single source of truth for "input is blocked right now" (screen transitions, popup fades).</summary>
    public sealed class InputGate
    {
        private readonly Func<double> _now;
        private int _holds;
        private double _until = double.NegativeInfinity;

        public InputGate(Func<double> now)
        {
            _now = now ?? throw new ArgumentNullException(nameof(now));
        }

        public bool IsBlocked => _holds > 0 || _now() < _until;

        /// <summary>Raised when the gate goes from free to blocked.</summary>
        public event Action Blocked;

        /// <summary>Blocks until now + seconds; a shorter request never shortens an existing block.</summary>
        public void BlockFor(double seconds)
        {
            bool wasBlocked = IsBlocked;
            _until = Math.Max(_until, _now() + seconds);
            if (!wasBlocked && IsBlocked) Blocked?.Invoke();
        }

        public void Hold()
        {
            bool wasBlocked = IsBlocked;
            _holds++;
            if (!wasBlocked) Blocked?.Invoke();
        }

        public void Release()
        {
            if (_holds > 0) _holds--;
        }
    }
}
