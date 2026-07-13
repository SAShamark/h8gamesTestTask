using System;
using Game.Entities.Units;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Character
{
    [Serializable]
    public class ShootingLogic
    {
        [SerializeField] private Transform _shootPoint;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private LayerMask _enemyLayers;
        [SerializeField] private float _shootRange = 8f;
        [SerializeField] private float _shootInterval = 0.35f;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _aimRotationSpeed = 540f;
        [SerializeField] private float _aimAngleTolerance = 3f;
        [SerializeField] private float _damagePerUpgrade = 5f;
        [SerializeField, Range(0.1f, 1f)] private float _shootIntervalMultiplierPerUpgrade = 0.9f;
        [SerializeField] private float _minimumShootInterval = 0.12f;
        [SerializeField] private int _initialPoolSize = 8;

        private readonly Collider[] _targetResults = new Collider[32];
        private ObjectPool<Projectile> _projectilePool;
        private Rigidbody _ownerRigidbody;
        private MovementLogic _movementLogic;
        private float _nextShotTime;

        public bool IsShooting { get; private set; }

        public void Init(Rigidbody ownerRigidbody, MovementLogic movementLogic)
        {
            _ownerRigidbody = ownerRigidbody;
            _movementLogic = movementLogic;
            _projectilePool = new ObjectPool<Projectile>(_projectilePrefab, _initialPoolSize, _shootPoint);
        }

        public void Tick(float deltaTime)
        {
            EnemyControl target = FindClosestTarget();
            IsShooting = false;

            if (target == null || _movementLogic.IsMoving)
            {
                return;
            }

            Vector3 aimDirection = target.AimPosition - _shootPoint.position;
            aimDirection.y = 0f;

            Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
            Quaternion rotation = Quaternion.RotateTowards(
                _ownerRigidbody.rotation,
                targetRotation,
                _aimRotationSpeed * deltaTime);
            _ownerRigidbody.MoveRotation(rotation);

            float aimAngle = Vector3.Angle(rotation * Vector3.forward, aimDirection);
            if (aimAngle > _aimAngleTolerance)
            {
                return;
            }

            IsShooting = true;
            if (Time.time < _nextShotTime)
            {
                return;
            }

            _nextShotTime = Time.time + _shootInterval;
            Projectile projectile = _projectilePool.GetFreeElement();
            projectile.Launch(_shootPoint.position, target, _projectileSpeed, _damage, ReturnProjectile);
        }

        private EnemyControl FindClosestTarget()
        {
            int targetsCount = Physics.OverlapSphereNonAlloc(
                _shootPoint.position, _shootRange, _targetResults, _enemyLayers);

            EnemyControl closestTarget = null;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < targetsCount; i++)
            {
                EnemyControl target = _targetResults[i].GetComponentInParent<EnemyControl>();
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                float distance = (target.transform.position - _shootPoint.position).sqrMagnitude;
                if (distance >= closestDistance)
                {
                    continue;
                }

                closestTarget = target;
                closestDistance = distance;
            }

            return closestTarget;
        }

        private void ReturnProjectile(Projectile projectile)
        {
            _projectilePool.ReturnToPool(projectile);
        }

        public void UpgradeGun()
        {
            _damage += _damagePerUpgrade;
            _shootInterval = Mathf.Max(_minimumShootInterval, _shootInterval * _shootIntervalMultiplierPerUpgrade);
        }

        public void Stop()
        {
            IsShooting = false;
        }
    }
}
