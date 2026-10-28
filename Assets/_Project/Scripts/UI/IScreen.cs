namespace Kanji.UI
{
    public interface IScreen
    {
        ScreenId Id { get; }
        void OnShow(object args);
        void OnHide();
        /// <summary>True = back consumed by the screen; false = default Pop.</summary>
        bool HandleBack();
    }
}
