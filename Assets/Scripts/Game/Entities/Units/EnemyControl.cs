using System;
using Game.Entities.Character;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Units
{
    public class EnemyControl : BaseUnitControl, IProjectileTarget
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Health _health;
        [SerializeField] private EnemyAttackLogic _attackLogic = new();
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Vector3 _projectileSpawnOffset = new(0f, 1f, 0.45f);
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private int _initialProjectilePoolSize = 4;
        [SerializeField] private float _aimHeight = 1f;
        [SerializeField] private float _deathAnimationDuration = 2f;

        private readonly int _isShootingHash = Animator.StringToHash("IsShooting");
        private readonly int _deathHash = Animator.StringToHash("Died");
        private readonly int _victoryHash = Animator.StringToHash("Victory");
        private ObjectPool<Projectile> _projectilePool;
        private IProjectileTarget _target;
        private CharacterControl _defaultTarget;
        private bool _hasWon;

        public event Action<EnemyControl> OnDied;

        public bool IsAlive { get; private set; }
        public Vector3 AimPosition => transform.position + Vector3.up * _aimHeight;

        private void Awake()
        {
            _health.Init();
            _health.OnDeath += Die;
            IsAlive = true;
        }

        public void Init(CharacterControl target)
        {
            _defaultTarget = target;
            _attackLogic.Init(transform);
            _projectilePool = new ObjectPool<Projectile>(
                _projectilePrefab, _initialProjectilePoolSize, transform);
            ResetTarget();
        }

        public void SetTarget(TeammateControl target)
        {
            _target = target;
            _attackLogic.SetTarget(target);
        }

        public void ResetTarget()
        {
            _target = _defaultTarget;
            _attackLogic.SetTarget(_defaultTarget);
        }

        public void PlayVictory()
        {
            _hasWon = true;
            _animator.SetBool(_isShootingHash, false);
            _animator.SetTrigger(_victoryHash);
        }

        private void Update()
        {
            if (IsAlive && !_hasWon)
            {
                bool shouldShoot = _attackLogic.Tick(Time.deltaTime);
                _animator.SetBool(_isShootingHash, _attackLogic.IsTargetInRange);

                if (shouldShoot)
                {
                    Shoot();
                }
            }
        }

        private void Shoot()
        {
            Projectile projectile = _projectilePool.GetFreeElement();
            Vector3 spawnPosition = transform.TransformPoint(_projectileSpawnOffset);
            projectile.Launch(
                spawnPosition, _target, _projectileSpeed, _attackLogic.Damage, ReturnProjectile);
        }

        private void ReturnProjectile(Projectile projectile)
        {
            _projectilePool.ReturnToPool(projectile);
        }

        public void ApplyDamage(float damage)
        {
            _health.ApplyDamage(damage);
        }

        protected override void OnDestroy()
        {
            _health.OnDeath -= Die;
            base.OnDestroy();
        }

        private void Die()
        {
            IsAlive = false;
            _animator.SetBool(_isShootingHash, false);
            _animator.SetTrigger(_deathHash);
            OnDied?.Invoke(this);
            Destroy(gameObject, _deathAnimationDuration);
        }
    }
}
