using UnityEngine;
using UnityEngine.UI;

namespace UI.Managers.Components.LayoutResizer
{
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class VerticalResizer : BaseLayoutResizer<VerticalLayoutGroup>
    {
        protected override void Resize()
        {
            int count = transform.childCount;
            float spacing = LayoutGroup.spacing;
            float heightPerChild = (RectTransform.rect.height - LayoutGroup.padding.top -
                                    LayoutGroup.padding.bottom - spacing * (count - 1)) / count;
            float width = RectTransform.rect.width - LayoutGroup.padding.left - LayoutGroup.padding.right;

            foreach (RectTransform child in transform)
            {
                Vector2 size = child.sizeDelta;
                child.sizeDelta = new Vector2(_controlWidth ? width : size.x, _controlHeight ? heightPerChild : size.y);
            }
        }
    }
}