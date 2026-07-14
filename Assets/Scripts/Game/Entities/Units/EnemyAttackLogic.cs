using System;
using Game.Entities.Character;
using UnityEngine;

namespace Game.Entities.Units
{
    [Serializable]
    public class EnemyAttackLogic
    {
        [SerializeField, Min(0.1f)] private float _attackRange = 4.5f;
        [SerializeField, Min(0.1f)] private float _attackInterval = 0.9f;
        [SerializeField, Min(0f)] private float _damage = 4f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 540f;

        private Transform _owner;
        private IProjectileTarget _target;
        private float _squaredAttackRange;
        private float _nextAttackTime;

        public bool IsTargetInRange { get; private set; }
        public float AttackRange => _attackRange;
        public float Damage => _damage;

        public void Init(Transform owner)
        {
            _owner = owner;
            _squaredAttackRange = _attackRange * _attackRange;
            _nextAttackTime = Time.time + UnityEngine.Random.Range(0f, _attackInterval);
        }

        public void SetTarget(IProjectileTarget target)
        {
            _target = target;
            IsTargetInRange = false;
        }

        public bool Tick(float deltaTime)
        {
            Vector3 direction = _target.AimPosition - _owner.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > _squaredAttackRange)
            {
                IsTargetInRange = false;
                return false;
            }

            IsTargetInRange = true;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _owner.rotation = Quaternion.RotateTowards(
                _owner.rotation,
                targetRotation,
                _rotationSpeed * deltaTime);

            if (Time.time < _nextAttackTime)
            {
                return false;
            }

            _nextAttackTime = Time.time + _attackInterval;
            return true;
        }
    }
}
