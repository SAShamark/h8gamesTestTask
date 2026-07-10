using UnityEngine;
using UnityEngine.UI;

namespace UI.Managers.Components.LayoutResizer
{
    [ExecuteAlways]
    public abstract class BaseLayoutResizer<T> : MonoBehaviour where T : LayoutGroup
    {
        [SerializeField] protected RectTransform _contentRect;
        [SerializeField] protected bool _controlWidth = true;
        [SerializeField] protected bool _controlHeight = true;

        protected T LayoutGroup;
        protected RectTransform RectTransform;

        protected virtual void Awake()
        {
            LayoutGroup = GetComponent<T>();
            RectTransform = _contentRect ? _contentRect : GetComponent<RectTransform>();
        }

        protected virtual void Start()
        {
            Resize();
        }

        /// <summary>
        /// Called automatically when the RectTransform size changes
        /// </summary>
        protected virtual void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying || LayoutGroup == null || RectTransform == null)
                return;

            Resize();
        }

        [ContextMenu("Resize Now")]
        protected void ResizeNow()
        {
            Resize();
        }

        protected abstract void Resize();
    }
}