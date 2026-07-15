using System;
using UnityEngine;

namespace Game.Entities.Units.Base
{
    public abstract class BaseUnitControl : MonoBehaviour, IProjectileTarget
    {
        [SerializeField] private Health _health;
        [SerializeField] private ProjectileHitFeedback _hitFeedback;
        [SerializeField] private float _aimHeight = 1f;

        public Health Health => _health;
        public bool IsAlive { get; protected set; }
        public Vector3 AimPosition => transform.position + Vector3.up * _aimHeight;

        public event Action Died;

        protected ProjectileHitFeedback HitFeedback => _hitFeedback;

        protected void InitializeUnit()
        {
            _health.Init();
            _health.OnDeath += Die;
            IsAlive = true;
        }

        protected virtual void OnDestroy()
        {
            _health.OnDeath -= Die;
        }

        public void ApplyDamage(float damage)
        {
            _health.ApplyDamage(damage);
        }

        public virtual void PlayHitFeedback(Vector3 hitPosition)
        {
            _hitFeedback.Play(hitPosition);
        }

        protected void MarkAsDead()
        {
            IsAlive = false;
            _health.HideBar();
            Died?.Invoke();
        }

        protected abstract void Die();
    }
}
