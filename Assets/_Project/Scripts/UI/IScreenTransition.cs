using System;

namespace Kanji.UI
{
    /// <summary>Runs a visual screen swap, possibly delayed (fade). The navigation stack is already updated.</summary>
    public interface IScreenTransition
    {
        void Run(Action swap);
    }
}
