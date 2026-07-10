using UnityEngine;

namespace UI.Managers.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool _isHorizontalControl = true;
        [SerializeField] private bool _isVerticalControl;
        
        public void FitToSafeArea()
        {
            var rectTransform = GetComponent<RectTransform>();
            var rootCanvas = GetComponentInParent<Canvas>();
            if (null == rootCanvas)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;
            Rect pixelRect = rootCanvas.pixelRect;

            if (_isHorizontalControl)
            {
                anchorMin.x = safeArea.x / pixelRect.width;
                anchorMax.x = (safeArea.x + safeArea.width) / pixelRect.width;
            }

            if (_isVerticalControl)
            {
                anchorMin.y = safeArea.y / pixelRect.height;
                anchorMax.y = (safeArea.y + safeArea.height) / pixelRect.height;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }
}