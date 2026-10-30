using UnityEngine;
using UnityEngine.EventSystems;

namespace Kanji.UI
{
    /// <summary>
    /// Sits next to the popup body's ScrollRect: Unity sends drag events to every handler on that object,
    /// so the card sees drags that start over the body and takes the horizontal ones.
    /// </summary>
    public sealed class CardSwipeRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CardSwipe Target { get; set; }

        public void OnBeginDrag(PointerEventData e) { if (Target != null) Target.Begin(e, true); }
        public void OnDrag(PointerEventData e) { if (Target != null) Target.Move(e); }
        public void OnEndDrag(PointerEventData e) { if (Target != null) Target.End(e); }
    }
}
