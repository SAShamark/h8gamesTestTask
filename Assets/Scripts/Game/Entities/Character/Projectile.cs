using System;
using UnityEngine;

namespace Game.Entities.Character
{
    public class Projectile : MonoBehaviour
    {
        private IProjectileTarget _target;
        private Action<Projectile> _onComplete;
        private float _speed;
        private float _damage;

        public void Launch(Vector3 startPosition, IProjectileTarget target, float speed, float damage,
            Action<Projectile> onComplete)
        {
            transform.SetParent(null, true);
            transform.position = startPosition;
            _target = target;
            _speed = speed;
            _damage = damage;
            _onComplete = onComplete;
        }

        private void Update()
        {
            if (!_target.IsAlive)
            {
                Complete();
                return;
            }

            Vector3 targetPosition = _target.AimPosition;
            Vector3 direction = targetPosition - transform.position;
            float movementDistance = _speed * Time.deltaTime;

            if (direction.sqrMagnitude <= movementDistance * movementDistance)
            {
                _target.ApplyDamage(_damage);
                Complete();
                return;
            }

            transform.position += direction.normalized * movementDistance;
            transform.forward = direction;
        }

        private void Complete()
        {
            _onComplete.Invoke(this);
        }
    }
}
