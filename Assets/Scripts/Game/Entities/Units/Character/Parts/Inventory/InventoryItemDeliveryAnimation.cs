using System;
using DG.Tweening;
using UnityEngine;

namespace Game.Entities.Units.Character.Parts.Inventory
{
    [Serializable]
    public class InventoryItemDeliveryAnimation
    {
        [SerializeField] private float _flyDuration = 0.35f;
        [SerializeField] private float _jumpPower = 1.1f;
        [SerializeField] private int _jumpCount = 1;
        [SerializeField] private float _spinAngle = 360f;
        [SerializeField] private Vector3 _targetWorldOffset = new(0f, 0.25f, 0f);

        public void Play(Item item, Transform deliveryTarget, Action onComplete)
        {
            Transform itemTransform = item.Transform;
            itemTransform.DOKill();
            item.ResetScale();
            itemTransform.SetParent(deliveryTarget.parent, true);

            Vector3 targetPosition = deliveryTarget.position + _targetWorldOffset;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(itemTransform.DOJump(targetPosition, _jumpPower, _jumpCount, _flyDuration)
                .SetEase(Ease.InOutQuad));
            sequence.Join(itemTransform.DORotate(itemTransform.eulerAngles + new Vector3(0f, _spinAngle, 0f),
                _flyDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            sequence.OnComplete(() => onComplete.Invoke());
        }
    }
}
