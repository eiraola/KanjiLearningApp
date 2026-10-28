using UnityEngine;

namespace Kanji.UI
{
    public abstract class ScreenView : MonoBehaviour, IScreen
    {
        [SerializeField] private ScreenId _id;

        public ScreenId Id => _id;

        public virtual void OnShow(object args) => gameObject.SetActive(true);
        public virtual void OnHide() => gameObject.SetActive(false);
        public virtual bool HandleBack() => false;
    }
}
