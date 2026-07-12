using System;
using DG.Tweening;
using UnityEngine;

namespace Game.Entities.Character
{
    [Serializable]
    public class InventoryItemAnimation
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _moveDuration = 0.525f;
        [SerializeField] private float _jumpPower = 1.15f;
        [SerializeField] private int _jumpCount = 1;
        [SerializeField] private float _spinAngle = 360f;
        [SerializeField] private float _settleScale = 1.12f;
        [SerializeField] private float _settleDuration = 0.15f;

        public bool Enabled => _enabled;

        public void Play(Transform item, Vector3 targetLocalPosition, Vector3 targetLocalRotation)
        {
            item.DOKill();

            Sequence sequence = DOTween.Sequence();
            sequence.Append(item.DOLocalJump(targetLocalPosition, _jumpPower, _jumpCount, _moveDuration)
                .SetEase(Ease.OutCubic));
            sequence.Join(item.DOLocalRotate(targetLocalRotation + new Vector3(0f, _spinAngle, 0f), _moveDuration,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic));
            sequence.Append(item.DOScale(item.localScale * _settleScale, _settleDuration * 0.5f)
                .SetEase(Ease.OutQuad));
            sequence.Append(item.DOScale(item.localScale, _settleDuration * 0.5f)
                .SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                item.localPosition = targetLocalPosition;
                item.localEulerAngles = targetLocalRotation;
            });
        }
    }
}
