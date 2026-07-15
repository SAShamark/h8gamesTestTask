using System;
using UnityEngine;

namespace Game.Entities.Units.Base
{
    [Serializable]
    public class Health
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private HealthBarView _healthBarView;

        private float _currentHealth;
        private bool _isDead;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float NormalizedHealth => _currentHealth / _maxHealth;

        public event Action<float> OnHealthChanged;
        public event Action OnDeath;

        public virtual void Init()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
            _healthBarView.Init(NormalizedHealth);
            OnHealthChanged?.Invoke(NormalizedHealth);
        }

        public virtual void ApplyDamage(float damage)
        {
            if (_isDead)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            _healthBarView.ShowDamage(NormalizedHealth);
            OnHealthChanged?.Invoke(NormalizedHealth);

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                OnDeath?.Invoke();
            }
        }

        public void Heal(float value)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + value);
            _healthBarView.ShowHeal(NormalizedHealth);
            OnHealthChanged?.Invoke(NormalizedHealth);
        }

        public void ShowBuff(bool isVisible)
        {
            _healthBarView.ShowBuff(isVisible);
        }

        public void HideBar()
        {
            _healthBarView.Hide();
        }
    }
}
