using System;
using DG.Tweening;
using Game.Entities.Units.Character;
using UnityEngine;

namespace Game.Entities.Areas
{
    public class FlagCaptureArea : BaseArea
    {
        [SerializeField] private Transform _flagCloth;
        [SerializeField] private Renderer _flagRenderer;
        [SerializeField] private float _lowerDistance = 2.5f;
        [SerializeField] private float _lowerDuration = 0.65f;
        [SerializeField] private float _colorDuration = 0.25f;
        [SerializeField] private float _raiseDuration = 0.75f;
        [SerializeField] private Color _capturedColor = new(0.55f, 0.55f, 0.55f, 1f);

        private Material _flagMaterial;
        private Sequence _captureSequence;
        private bool _isUnlocked;
        private bool _isCaptured;
        private bool _isCharacterInside;

        public event Action OnCaptured;

        private void Awake()
        {
            _flagMaterial = _flagRenderer.material;
        }

        public void Unlock()
        {
            _isUnlocked = true;

            if (_isCharacterInside)
            {
                Capture();
            }
        }

        protected override void HandleCharacterEnter(CharacterControl character)
        {
            _isCharacterInside = true;

            if (_isUnlocked && character.IsAlive)
            {
                Capture();
            }
        }

        protected override void HandleCharacterExit(CharacterControl character)
        {
            _isCharacterInside = false;
        }

        private void Capture()
        {
            if (_isCaptured)
            {
                return;
            }

            _isCaptured = true;
            float raisedPosition = _flagCloth.localPosition.y;
            float loweredPosition = raisedPosition - _lowerDistance;

            _captureSequence = DOTween.Sequence();
            _captureSequence.Append(_flagCloth.DOLocalMoveY(loweredPosition, _lowerDuration)
                .SetEase(Ease.InOutSine));
            _captureSequence.Append(_flagMaterial.DOColor(_capturedColor, "_BaseColor", _colorDuration)
                .SetEase(Ease.InOutSine));
            _captureSequence.Append(_flagCloth.DOLocalMoveY(raisedPosition, _raiseDuration)
                .SetEase(Ease.InOutSine));
            _captureSequence.SetLink(gameObject);
            _captureSequence.OnComplete(() => OnCaptured?.Invoke());
        }

        private void OnDestroy()
        {
            _captureSequence?.Kill();
        }
    }
}
