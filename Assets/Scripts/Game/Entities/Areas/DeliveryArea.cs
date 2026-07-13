using DG.Tweening;
using Game.Entities.Character;
using Services.Currency;
using TMPro;
using UnityEngine;

namespace Game.Entities.Areas
{
    public class DeliveryArea : BaseArea
    {
        [SerializeField] private CurrencyType _currencyType;
        [SerializeField] private int _requiredItemsCount = 5;
        [SerializeField] private SpriteRenderer _fillRenderer;
        [SerializeField] private TMP_Text _itemsCountText;
        [SerializeField] private float _deliveryInterval = 0.05f;
        [SerializeField] private float _fillDuration = 0.25f;
        [SerializeField] private float _hitScale = 1.12f;
        [SerializeField] private float _hitScaleDuration = 0.12f;

        private int _deliveredItemsCount;
        private int _itemsInDeliveryCount;
        private float _nextDeliveryTime;
        private Vector3 _fillDefaultScale;
        private Vector3 _fillDefaultLocalPosition;
        private Vector3 _fillDirection;
        private float _fillSize;
        private Vector3 _textDefaultScale;
        private Tween _fillTween;
        private float _visualFill;
        private bool _isCompleted;
        private bool _isCharacterInside;
        private bool _canDeliver = true;

        private void Awake()
        {
            _fillDefaultScale = _fillRenderer.transform.localScale;
            _fillDefaultLocalPosition = _fillRenderer.transform.localPosition;
            _fillDirection = _fillRenderer.transform.localRotation * Vector3.up;
            _fillSize = _fillRenderer.sprite.bounds.size.y * _fillDefaultScale.y;
            _textDefaultScale = _itemsCountText.transform.localScale;
            SetFill(0f);
            UpdateItemsCountText();
        }

        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            if (other.GetComponent<IInventoryOwner>() == null)
            {
                return;
            }

            _isCharacterInside = true;
            _canDeliver = true;
            TryDeliverItem(other);
        }

        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);

            if (other.GetComponent<IInventoryOwner>() == null)
            {
                return;
            }

            _isCharacterInside = false;
            _canDeliver = true;
        }

        private void OnTriggerStay(Collider other)
        {
            TryDeliverItem(other);
        }

        private void TryDeliverItem(Collider other)
        {
            if (!_canDeliver || _isCompleted || Time.time < _nextDeliveryTime)
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
            if (_deliveredItemsCount + _itemsInDeliveryCount >= _requiredItemsCount)
            {
                return;
            }

            _nextDeliveryTime = Time.time + _deliveryInterval;
            if (!inventory.TryDeliverItem(_currencyType, transform, OnItemDelivered))
            {
                return;
            }

            _itemsInDeliveryCount++;
        }

        private void OnItemDelivered()
        {
            _itemsInDeliveryCount--;
            _deliveredItemsCount++;
            UpdateItemsCountText();
            PlayDeliveryHit();

            float targetFill = _deliveredItemsCount / (float)_requiredItemsCount;

            if (_deliveredItemsCount >= _requiredItemsCount)
            {
                _isCompleted = true;
                AnimateFill(targetFill).OnComplete(Complete);
                return;
            }

            AnimateFill(targetFill);
        }

        private void Complete()
        {
            _fillTween = null;
            NotifyCompleted();
        }

        public void ResetProgress()
        {
            _fillTween?.Kill();
            _deliveredItemsCount = 0;
            _itemsInDeliveryCount = 0;
            _nextDeliveryTime = 0f;
            _isCompleted = false;
            _canDeliver = !_isCharacterInside;
            SetFill(0f);
            UpdateItemsCountText();
        }

        private void UpdateItemsCountText()
        {
            _itemsCountText.text = $"{_deliveredItemsCount}/{_requiredItemsCount}";
        }

        private void PlayDeliveryHit()
        {
            _itemsCountText.transform.DOKill();
            _itemsCountText.transform.localScale = _textDefaultScale;

            _itemsCountText.transform.DOPunchScale(_textDefaultScale * (_hitScale - 1f), _hitScaleDuration, 1, 0.3f);
        }

        private Tween AnimateFill(float targetFill)
        {
            _fillTween?.Kill();
            _fillTween = DOTween.To(() => _visualFill, SetFill, targetFill, _fillDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject);

            return _fillTween;
        }

        private void SetFill(float fill)
        {
            _visualFill = fill;

            Vector3 scale = _fillDefaultScale;
            scale.y *= fill;
            _fillRenderer.transform.localScale = scale;

            Vector3 position = _fillDefaultLocalPosition;
            position -= _fillDirection * (_fillSize * (1f - fill) * 0.5f);
            _fillRenderer.transform.localPosition = position;
        }

        private void OnDestroy()
        {
            _fillTween?.Kill();
        }
    }
}
