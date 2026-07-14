using System;
using DG.Tweening;
using Game.Entities.Character;
using Services.ObjectPool;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Entities.Units
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyControl : BaseUnitControl, IProjectileTarget
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Health _health;
        [SerializeField] private ProjectileHitFeedback _hitFeedback;
        [SerializeField] private EnemyAttackLogic _attackLogic = new();
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Vector3 _projectileSpawnOffset = new(0f, 1f, 0.45f);
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private int _initialProjectilePoolSize = 4;
        [SerializeField] private float _aimHeight = 1f;
        [SerializeField] private float _deathAnimationDuration = 2f;
        [SerializeField] private float _deathSinkDistance = 1.5f;
        [SerializeField] private float _deathSinkDuration = 0.65f;

        [Header("Teammate Chase")]
        [SerializeField, Min(0.1f)] private float _chaseRangePadding = 2f;
        [SerializeField, Min(0.01f)] private float _homeStoppingDistance = 0.12f;
        [SerializeField, Min(0f)] private float _movingAnimationThreshold = 0.08f;

        private readonly int _isShootingHash = Animator.StringToHash("IsShooting");
        private readonly int _isMovingHash = Animator.StringToHash("IsMoving");
        private readonly int _deathHash = Animator.StringToHash("Died");
        private readonly int _victoryHash = Animator.StringToHash("Victory");
        private NavMeshAgent _navMeshAgent;
        private ObjectPool<Projectile> _projectilePool;
        private IProjectileTarget _target;
        private CharacterControl _defaultTarget;
        private Vector3 _homePosition;
        private bool _isTargetingTeammate;
        private bool _hasWon;
        private Tween _deathTween;

        public event Action<EnemyControl> OnDied;

        public bool IsAlive { get; private set; }
        public int LifeVersion => 0;
        public Vector3 AimPosition => transform.position + Vector3.up * _aimHeight;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _health.Init();
            _health.OnDeath += Die;
            IsAlive = true;
        }

        public void Init(CharacterControl target)
        {
            _defaultTarget = target;
            _homePosition = transform.position;
            _attackLogic.Init(transform);
            _projectilePool = new ObjectPool<Projectile>(
                _projectilePrefab, _initialProjectilePoolSize, transform);
            ResetTarget();
        }

        public void SetTarget(TeammateControl target)
        {
            _target = target;
            _isTargetingTeammate = true;
            _attackLogic.SetTarget(target);
        }

        public void ResetTarget()
        {
            _target = _defaultTarget;
            _isTargetingTeammate = false;
            _attackLogic.SetTarget(_defaultTarget);
        }

        public void PlayVictory()
        {
            _hasWon = true;
            StopMovement();
            _animator.SetBool(_isShootingHash, false);
            _animator.SetTrigger(_victoryHash);
        }

        public void ResumeCombat()
        {
            _hasWon = false;
            _animator.Rebind();
            _animator.Update(0f);
            ResetTarget();
        }

        private void Update()
        {
            if (IsAlive && !_hasWon)
            {
                if (_isTargetingTeammate)
                {
                    UpdateTeammateCombat();
                }
                else
                {
                    UpdateCharacterCombat();
                }
            }
        }

        private void UpdateTeammateCombat()
        {
            float chaseRange = _attackLogic.AttackRange + _chaseRangePadding;
            Vector3 targetFromHome = _target.AimPosition - _homePosition;
            targetFromHome.y = 0f;

            if (!_target.IsAlive || targetFromHome.sqrMagnitude > chaseRange * chaseRange)
            {
                ReturnHome();
                return;
            }

            Vector3 direction = _target.AimPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > _attackLogic.AttackRange * _attackLogic.AttackRange)
            {
                MoveTo(_target.AimPosition, _attackLogic.AttackRange);
                return;
            }

            StopMovement();
            UpdateAttack();
        }

        private void UpdateCharacterCombat()
        {
            Vector3 homeOffset = _homePosition - transform.position;
            homeOffset.y = 0f;

            if (homeOffset.sqrMagnitude > _homeStoppingDistance * _homeStoppingDistance)
            {
                ReturnHome();
                return;
            }

            StopMovement();
            UpdateAttack();
        }

        private void UpdateAttack()
        {
            bool shouldShoot = _attackLogic.Tick(Time.deltaTime);
            _animator.SetBool(_isShootingHash, _attackLogic.IsTargetInRange);

            if (shouldShoot)
            {
                Shoot();
            }
        }

        private void ReturnHome()
        {
            MoveTo(_homePosition, _homeStoppingDistance);
        }

        private void MoveTo(Vector3 destination, float stoppingDistance)
        {
            _animator.SetBool(_isShootingHash, false);
            _navMeshAgent.stoppingDistance = stoppingDistance;
            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(destination);
            _animator.SetBool(_isMovingHash,
                _navMeshAgent.velocity.sqrMagnitude > _movingAnimationThreshold * _movingAnimationThreshold);
        }

        private void StopMovement()
        {
            _navMeshAgent.isStopped = true;
            _animator.SetBool(_isMovingHash, false);
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

        public void PlayHitFeedback(Vector3 hitPosition)
        {
            _hitFeedback.Play(hitPosition);
        }

        protected override void OnDestroy()
        {
            _deathTween?.Kill();
            _health.OnDeath -= Die;
            base.OnDestroy();
        }

        private void Die()
        {
            IsAlive = false;
            _health.HideBar();
            StopMovement();
            _animator.SetBool(_isShootingHash, false);
            _animator.SetTrigger(_deathHash);
            OnDied?.Invoke(this);

            _navMeshAgent.updatePosition = false;
            _deathTween = DOTween.Sequence()
                .AppendInterval(_deathAnimationDuration)
                .Append(transform.DOMoveY(
                        transform.position.y - _deathSinkDistance,
                        _deathSinkDuration)
                    .SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(gameObject))
                .SetLink(gameObject);
        }
    }
}
