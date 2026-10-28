using System.Collections.Generic;
using System.Globalization;
using Kanji.Core.Model;
using Kanji.Core.Reading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>End-of-reading summary (args: ReadingStats). Accept and Back both return to the story list.</summary>
    public sealed class StatsScreen : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private TMP_Text _total;
        [SerializeField] private TMP_Text _passed;
        [SerializeField] private TMP_Text _failed;
        [SerializeField] private TMP_Text _percent;
        [SerializeField] private ScrollRect _failedScroll;
        [SerializeField] private RectTransform _failedContent;
        [SerializeField] private TextMeshProUGUI _failedRowTemplate;
        [SerializeField] private GameObject _noFailedMessage;

        private readonly List<TextMeshProUGUI> _rows = new List<TextMeshProUGUI>();
        private ScreenNavigator _navigator;

        internal ReadingStats Stats { get; private set; }

        public void Init(ScreenNavigator navigator)
        {
            _navigator = navigator;
            _backButton.onClick.AddListener(Accept);
            _acceptButton.onClick.AddListener(Accept);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            Stats = args as ReadingStats ?? new ReadingStats(0, null);
            _total.text = Format(Stats.Total);
            _passed.text = Format(Stats.Passed);
            _failed.text = Format(Stats.Failed);
            _percent.text = UiStrings.Percent(Stats.Percent);

            IReadOnlyList<KanjiEntry> failed = Stats.FailedEntries;
            for (int i = 0; i < failed.Count; i++)
            {
                TextMeshProUGUI row;
                if (i < _rows.Count) row = _rows[i];
                else
                {
                    row = Instantiate(_failedRowTemplate, _failedContent);
                    _rows.Add(row);
                }
                row.text = string.IsNullOrWhiteSpace(failed[i].Reading)
                    ? failed[i].Kanji
                    : failed[i].Kanji + "   " + failed[i].Reading;
                row.gameObject.SetActive(true);
            }
            for (int i = failed.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
            _noFailedMessage.SetActive(failed.Count == 0);
            _failedScroll.StopMovement();
            _failedContent.anchoredPosition = Vector2.zero;
        }

        private void Accept()
        {
            if (_navigator != null && _navigator.Current == (IScreen)this) _navigator.Pop();
        }

        private static string Format(int n) => n.ToString(CultureInfo.InvariantCulture);
    }
}
