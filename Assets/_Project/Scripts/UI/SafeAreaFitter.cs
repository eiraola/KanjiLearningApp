using UnityEngine;

namespace Kanji.UI
{
    /// <summary>Fits the RectTransform anchors to Screen.safeArea.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied;

        private void OnEnable() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            var area = Screen.safeArea;
            if (area == _applied || Screen.width <= 0 || Screen.height <= 0) return;
            _applied = area;

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rt.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
