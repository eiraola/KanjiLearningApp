using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    public sealed class EmptyScreen : ScreenView
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _backButton;

        public void Init(ScreenNavigator navigator)
        {
            _backButton.onClick.AddListener(() => navigator.Pop());
        }
    }
}
