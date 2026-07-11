using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Entities.Character
{
    [Serializable]
    public class Health
    {
        [SerializeField] private float _maxHealth = 100f;

        [SerializeField] private Image _backgroundFill;
        [SerializeField] private Image _damageFill;
        [SerializeField] private Image _healthFill;

        [SerializeField] private Color _backgroundColor = new(1f, 0f, 0f);
        [SerializeField] private Color _damageColor = new(1, 1, 1);
        [SerializeField] private Color _healthColor = new(0.15f, 1f, 0.35f);

        [Header("Damage Feel")]
        [SerializeField] private float _healthFillDuration = 0.12f;
        [SerializeField] private float _damageDelay = 0.18f;
        [SerializeField] private float _damageFillDuration = 0.32f;
        [SerializeField] private float _hitPunchScale = 1.18f;
        [SerializeField] private float _hitPunchDuration = 0.18f;

        private float _currentHealth;
        private Sequence _damageSequence;
        private Vector3 _healthFillDefaultScale;

        public event Action<int> OnHealthChanged;
        public event Action OnDeath;

        public void Init()
        {
            _currentHealth = _maxHealth;
            _healthFillDefaultScale = _healthFill.transform.localScale;
            _backgroundFill.color = _backgroundColor;
            _damageFill.color = _damageColor;
            _healthFill.color = _healthColor;
            SetFillAmount(1f);
        }

        public void Dispose()
        {
            _damageSequence?.Kill();
        }

        public void ApplyDamage(float damage)
        {
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            AnimateDamage(_currentHealth / _maxHealth);
        }

        public void Heal(float value)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + value);
            AnimateHeal(_currentHealth / _maxHealth);
        }

        private void AnimateDamage(float targetFill)
        {
            _damageSequence?.Kill();

            _damageSequence = DOTween.Sequence();
            _damageSequence.Join(_healthFill.DOFillAmount(targetFill, _healthFillDuration).SetEase(Ease.OutQuad));
            _damageSequence.Join(_healthFill.transform.DOScale(_healthFillDefaultScale * _hitPunchScale,
                _hitPunchDuration * 0.5f).SetEase(Ease.OutBack));
            _damageSequence.Append(_healthFill.transform.DOScale(_healthFillDefaultScale, _hitPunchDuration * 0.5f)
                .SetEase(Ease.OutQuad));
            _damageSequence.AppendInterval(_damageDelay);
            _damageSequence.Append(_damageFill.DOFillAmount(targetFill, _damageFillDuration).SetEase(Ease.OutCubic));
        }

        private void AnimateHeal(float targetFill)
        {
            _damageSequence?.Kill();

            _damageSequence = DOTween.Sequence();
            _damageSequence.Join(_healthFill.DOFillAmount(targetFill, _healthFillDuration).SetEase(Ease.OutQuad));
            _damageSequence.Join(_damageFill.DOFillAmount(targetFill, _healthFillDuration).SetEase(Ease.OutQuad));
            _damageSequence.Join(_healthFill.transform.DOScale(_healthFillDefaultScale, _hitPunchDuration)
                .SetEase(Ease.OutQuad));
        }

        private void SetFillAmount(float fillAmount)
        {
            _damageFill.fillAmount = fillAmount;
            _healthFill.fillAmount = fillAmount;
        }
    }
}