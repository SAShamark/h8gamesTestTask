using DG.Tweening;
using Game.Entities.Character;
using TMPro;
using UnityEngine;

namespace Game.Entities.Areas
{
    public class DeliveryArea : BaseArea
    {
        [SerializeField] private int _requiredItemsCount = 5;
        [SerializeField] private SpriteRenderer _fillRenderer;
        [SerializeField] private TMP_Text _itemsCountText;
        [SerializeField] private float _deliveryInterval = 0.05f;
        [SerializeField] private float _hitScale = 1.12f;
        [SerializeField] private float _hitScaleDuration = 0.12f;

        private int _deliveredItemsCount;
        private float _nextDeliveryTime;
        private Vector3 _fillDefaultScale;
        private Vector3 _textDefaultScale;
        private bool _isCompleted;

        private void Awake()
        {
            _fillDefaultScale = _fillRenderer.transform.localScale;
            _textDefaultScale = _itemsCountText.transform.localScale;
            UpdateItemsCountText();
        }

        private void OnTriggerStay(Collider other)
        {
            if (_isCompleted || Time.time < _nextDeliveryTime)
            {
                return;
            }

            var inventoryOwner = other.GetComponent<IInventoryOwner>();
            if (inventoryOwner == null)
            {
                return;
            }

            DeliverItem(inventoryOwner.Inventory);
        }

        private void DeliverItem(Inventory inventory)
        {
            _nextDeliveryTime = Time.time + _deliveryInterval;
            if (!inventory.TryDeliverLastItem(transform, OnItemDelivered))
            {
                return;
            }
        }

        private void OnItemDelivered()
        {
            _deliveredItemsCount++;
            UpdateItemsCountText();
            PlayDeliveryHit();

            if (_deliveredItemsCount >= _requiredItemsCount)
            {
                _isCompleted = true;
                Complete();
            }
        }

        private void Complete()
        {
            gameObject.SetActive(false);
        }

        private void UpdateItemsCountText()
        {
            _itemsCountText.text = $"{_deliveredItemsCount}/{_requiredItemsCount}";
        }

        private void PlayDeliveryHit()
        {
            _fillRenderer.transform.DOKill();
            _itemsCountText.transform.DOKill();
            _fillRenderer.transform.localScale = _fillDefaultScale;
            _itemsCountText.transform.localScale = _textDefaultScale;

            _fillRenderer.transform.DOPunchScale(_fillDefaultScale * (_hitScale - 1f), _hitScaleDuration, 1, 0.3f);
            _itemsCountText.transform.DOPunchScale(_textDefaultScale * (_hitScale - 1f), _hitScaleDuration, 1, 0.3f);
        }
    }
}