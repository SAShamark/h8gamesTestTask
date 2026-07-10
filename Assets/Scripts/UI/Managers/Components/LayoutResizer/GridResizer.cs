using UnityEngine;
using UnityEngine.UI;

namespace UI.Managers.Components.LayoutResizer
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridResizer : BaseLayoutResizer<GridLayoutGroup>
    {
        protected override void Resize()
        {
            int columns = LayoutGroup.constraintCount;
            float totalSpacing = LayoutGroup.spacing.x * (columns - 1);
            float availableWidth = RectTransform.rect.width - LayoutGroup.padding.left - LayoutGroup.padding.right - totalSpacing;

            float cellSize = availableWidth / columns;

            LayoutGroup.cellSize = new Vector2(
                _controlWidth ? cellSize : LayoutGroup.cellSize.x,
                _controlHeight ? cellSize : LayoutGroup.cellSize.y
            );
        }
    }
}