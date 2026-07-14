using UnityEngine;

namespace Game
{
    public class UnitSlotView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _emptySprite;
        [SerializeField] private Sprite _occupiedSprite;

        private void Awake()
        {
            SetOccupied(false);
        }

        public void SetOccupied(bool isOccupied)
        {
            _spriteRenderer.sprite = isOccupied ? _occupiedSprite : _emptySprite;
        }
    }
}
