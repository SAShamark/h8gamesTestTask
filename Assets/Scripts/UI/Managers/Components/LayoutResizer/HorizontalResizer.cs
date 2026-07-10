using UnityEngine;
using UnityEngine.UI;

namespace UI.Managers.Components.LayoutResizer
{
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class HorizontalResizer : BaseLayoutResizer<HorizontalLayoutGroup>
    {
        protected override void Resize()
        {
            int count = transform.childCount;
            float spacing = LayoutGroup.spacing;
            float widthPerChild = (RectTransform.rect.width - LayoutGroup.padding.left - LayoutGroup.padding.right -
                                   spacing * (count - 1)) / count;
            float height = RectTransform.rect.height - LayoutGroup.padding.top - LayoutGroup.padding.bottom;

            foreach (RectTransform child in transform)
            {
                Vector2 size = child.sizeDelta;
                child.sizeDelta = new Vector2(_controlWidth ? widthPerChild : size.x, _controlHeight ? height : size.y);
            }
        }
    }
}