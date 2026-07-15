using System;
using UnityEngine;

namespace Game.Entities.Units
{
    public class Projectile : MonoBehaviour
    {
        private IProjectileTarget _target;
        private Action<Projectile> _onComplete;
        private float _speed;
        private float _damage;
        private bool _isActive;

        public void Launch(Vector3 startPosition, IProjectileTarget target, float speed, float damage,
            Action<Projectile> onComplete)
        {
            transform.SetParent(null, true);
            transform.position = startPosition;
            _target = target;
            _target.Died += HandleTargetDied;
            _speed = speed;
            _damage = damage;
            _onComplete = onComplete;
            _isActive = true;
        }

        private void Update()
        {
            MoveToTarget();
        }

        private void MoveToTarget()
        {
            Vector3 targetPosition = _target.AimPosition;
            Vector3 direction = targetPosition - transform.position;
            float movementDistance = _speed * Time.deltaTime;

            if (direction.sqrMagnitude <= movementDistance * movementDistance)
            {
                HitTarget(targetPosition);
                return;
            }

            transform.position += direction.normalized * movementDistance;
            transform.forward = direction;
        }

        private void HitTarget(Vector3 hitPosition)
        {
            IProjectileTarget hitTarget = _target;
            UnsubscribeFromTarget();
            hitTarget.ApplyDamage(_damage);
            hitTarget.PlayHitFeedback(hitPosition);
            ReturnToPool();
        }

        private void HandleTargetDied()
        {
            Complete();
        }

        private void Complete()
        {
            UnsubscribeFromTarget();
            ReturnToPool();
        }

        private void UnsubscribeFromTarget()
        {
            if (!_isActive)
            {
                return;
            }

            _target.Died -= HandleTargetDied;
        }

        private void ReturnToPool()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _onComplete.Invoke(this);
        }
    }
}
