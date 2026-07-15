using System;
using Game.Entities.Units.Base;
using Game.Entities.Units.Enemies;
using Game.Entities.Units.Slots;
using Services.ObjectPool;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Entities.Units.Teammates
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class TeammateControl : BaseUnitControl
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _slotStoppingDistance = 0.12f;
        [SerializeField] private float _destinationRefreshDistance = 0.1f;
        [SerializeField] private float _navMeshSampleDistance = 1f;

        [Header("Combat")]
        [SerializeField] private float _attackRange = 2.5f;
        [SerializeField] private float _attackInterval = 0.6f;
        [SerializeField] private float _damage = 10f;
        [SerializeField, Min(1f)] private float _chargeDamageMultiplier = 1.5f;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Vector3 _projectileSpawnOffset = new(0f, 1f, 0.45f);
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private int _initialProjectilePoolSize = 4;
        [SerializeField] private float _attackRotationSpeed = 540f;
        [SerializeField] private float _attackAngleTolerance = 5f;
        [SerializeField] private float _animationSpeedDampTime = 0.12f;
        [SerializeField] private float _movingAnimationThreshold = 0.08f;
        [SerializeField] private float _deathAnimationDuration = 2f;

        private UnitSlots _unitSlots;
        private Transform _reservedSlot;
        private bool _isSlotReleased;
        private NavMeshAgent _navMeshAgent;
        private ObjectPool<Projectile> _projectilePool;
        private EnemyControl _target;
        private Vector3 _lastSlotPosition;
        private bool _isInitialized;
        private bool _isCharging;
        private bool _isChargeBuffed;
        private float _nextAttackTime;

        private readonly int _isShootingHash = Animator.StringToHash("IsShooting");
        private readonly int _isMovingHash = Animator.StringToHash("IsMoving");
        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _deathHash = Animator.StringToHash("Died");
        private readonly int _victoryHash = Animator.StringToHash("Victory");

        public event Action<TeammateControl> OnDied;

        public bool HasReachedSlot { get; private set; }
        private Transform ReservedSlot => _reservedSlot;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.stoppingDistance = _slotStoppingDistance;
            _lastSlotPosition = new Vector3(float.PositiveInfinity, 0f, 0f);
        }

        public void Init(UnitSlots unitSlots, Transform reservedSlot)
        {
            _unitSlots = unitSlots;
            _reservedSlot = reservedSlot;
            InitializeUnit();
            _isInitialized = true;
            _projectilePool = new ObjectPool<Projectile>(
                _projectilePrefab, _initialProjectilePoolSize, transform);

            if (!_navMeshAgent.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out NavMeshHit spawnPoint, _navMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                _navMeshAgent.Warp(spawnPoint.position);
            }

            UpdateDestination();
        }

        private void Update()
        {
            UpdateUnitState();
        }

        private void UpdateUnitState()
        {
            if (!_isInitialized || !_navMeshAgent.isOnNavMesh)
            {
                return;
            }

            UpdateMovementAnimation();

            if (_isCharging)
            {
                UpdateCharge();
                return;
            }

            if ((ReservedSlot.position - _lastSlotPosition).sqrMagnitude >=
                _destinationRefreshDistance * _destinationRefreshDistance)
            {
                UpdateDestination();
            }

            UpdateSlotArrival();
        }

        private void UpdateDestination()
        {
            if (!_navMeshAgent.isOnNavMesh)
            {
                return;
            }

            if (!NavMesh.SamplePosition(ReservedSlot.position, out NavMeshHit slotPoint, _navMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                return;
            }

            _lastSlotPosition = ReservedSlot.position;
            SetHasReachedSlot(false);
            _navMeshAgent.SetDestination(slotPoint.position);
        }

        private void UpdateSlotArrival()
        {
            bool hasReachedSlot = !_navMeshAgent.pathPending &&
                                  _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance &&
                                  _navMeshAgent.velocity.sqrMagnitude <=
                                  _movingAnimationThreshold * _movingAnimationThreshold;

            SetHasReachedSlot(hasReachedSlot);
        }

        public void Charge(EnemyControl target)
        {
            SetHasReachedSlot(false);
            SetSlotOccupied(false);
            _target = target;
            _isCharging = true;
            _navMeshAgent.stoppingDistance = _attackRange;
            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(_target.transform.position);
        }

        public void StopCharge()
        {
            _target = null;
            ReturnToSlot();
        }

        public void StopCombat()
        {
            _isCharging = false;
            _navMeshAgent.isStopped = true;
            StopAnimation();
        }

        public void PlayVictory()
        {
            StopCombat();
            _animator.SetTrigger(_victoryHash);
        }

        private void UpdateCharge()
        {
            if (!_target.IsAlive)
            {
                return;
            }

            Vector3 direction = _target.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > _attackRange * _attackRange)
            {
                _animator.SetBool(_isShootingHash, false);
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(_target.transform.position);
                return;
            }

            _navMeshAgent.isStopped = true;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _attackRotationSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, targetRotation) > _attackAngleTolerance)
            {
                _animator.SetBool(_isShootingHash, false);
                return;
            }

            _animator.SetBool(_isShootingHash, true);
            if (Time.time < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = Time.time + _attackInterval;
            Shoot();
        }

        private void Shoot()
        {
            Projectile projectile = _projectilePool.GetFreeElement();
            Vector3 spawnPosition = transform.TransformPoint(_projectileSpawnOffset);
            float damage = _isChargeBuffed ? _damage * _chargeDamageMultiplier : _damage;
            projectile.Launch(spawnPosition, _target, _projectileSpeed, damage, ReturnProjectile);
        }

        public void SetChargeBuff(bool isBuffed)
        {
            if (_isChargeBuffed == isBuffed)
            {
                return;
            }

            _isChargeBuffed = isBuffed;
            HitFeedback.SetBuffed(isBuffed);
            Health.ShowBuff(isBuffed);
        }

        private void ReturnProjectile(Projectile projectile)
        {
            _projectilePool.ReturnToPool(projectile);
        }

        private void ReturnToSlot()
        {
            _isCharging = false;
            SetHasReachedSlot(false);
            _animator.SetBool(_isShootingHash, false);
            _navMeshAgent.stoppingDistance = _slotStoppingDistance;
            _navMeshAgent.isStopped = false;
            _lastSlotPosition = new Vector3(float.PositiveInfinity, 0f, 0f);
            UpdateDestination();
        }

        private void SetHasReachedSlot(bool hasReachedSlot)
        {
            if (HasReachedSlot == hasReachedSlot)
            {
                return;
            }

            HasReachedSlot = hasReachedSlot;
            SetSlotOccupied(hasReachedSlot);
        }

        private void UpdateMovementAnimation()
        {
            float normalizedSpeed = _navMeshAgent.velocity.magnitude / _navMeshAgent.speed;
            _animator.SetFloat(_speedHash, normalizedSpeed, _animationSpeedDampTime, Time.deltaTime);
            _animator.SetBool(_isMovingHash, normalizedSpeed > _movingAnimationThreshold);
        }

        private void StopAnimation()
        {
            _animator.SetFloat(_speedHash, 0f);
            _animator.SetBool(_isMovingHash, false);
            _animator.SetBool(_isShootingHash, false);
        }

        protected override void OnDestroy()
        {
            ReleaseSlot();
            base.OnDestroy();
        }

        private void ReleaseSlot()
        {
            if (_unitSlots != null && !_isSlotReleased)
            {
                _unitSlots.ReleaseSlot(_reservedSlot);
                _isSlotReleased = true;
            }
        }

        private void SetSlotOccupied(bool isOccupied)
        {
            _unitSlots.SetSlotOccupied(_reservedSlot, isOccupied);
        }

        protected override void Die()
        {
            MarkAsDead();
            SetChargeBuff(false);
            _isInitialized = false;
            _navMeshAgent.isStopped = true;
            StopAnimation();
            _animator.SetTrigger(_deathHash);
            ReleaseSlot();
            OnDied?.Invoke(this);
            Destroy(gameObject, _deathAnimationDuration);
        }
    }
}
