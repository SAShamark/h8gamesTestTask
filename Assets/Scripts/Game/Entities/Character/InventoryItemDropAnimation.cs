using System;
using DG.Tweening;
using UnityEngine;

namespace Game.Entities.Character
{
    [Serializable]
    public class InventoryItemDropAnimation
    {
        [SerializeField] private float _minimumDropDistance = 1.25f;
        [SerializeField] private float _maximumDropDistance = 3.25f;
        [SerializeField] private float _minimumFlyDuration = 0.45f;
        [SerializeField] private float _maximumFlyDuration = 0.75f;
        [SerializeField] private float _jumpPower = 1.6f;
        [SerializeField] private float _maximumStartDelay = 0.15f;
        [SerializeField] private float _spinAngle = 540f;
        [SerializeField] private float _groundOffset = 0.1f;

        public void Play(Item item, Vector3 origin, int itemIndex, int itemsCount)
        {
            Transform itemTransform = item.Transform;
            itemTransform.DOKill();
            item.ResetScale();
            itemTransform.SetParent(null, true);

            float angle = itemIndex * 137.5f + UnityEngine.Random.Range(-12f, 12f);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            float spread = Mathf.Sqrt((itemIndex + 1f) / itemsCount);
            float distance = Mathf.Lerp(_minimumDropDistance, _maximumDropDistance, spread);
            float duration = Mathf.Lerp(_minimumFlyDuration, _maximumFlyDuration, spread);
            Vector3 targetPosition = origin + direction * distance;
            targetPosition.y = origin.y + _groundOffset;

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(UnityEngine.Random.Range(0f, _maximumStartDelay));
            sequence.Append(itemTransform.DOJump(targetPosition, _jumpPower, 1, duration)
                .SetEase(Ease.OutQuad));
            sequence.Join(itemTransform.DORotate(
                itemTransform.eulerAngles + new Vector3(_spinAngle, _spinAngle, 0f),
                duration,
                RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            sequence.OnComplete(item.PrepareForSpawn);
        }
    }
}
