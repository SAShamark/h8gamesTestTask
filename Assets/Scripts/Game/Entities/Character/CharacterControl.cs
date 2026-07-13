using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.Character
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterControl : MonoBehaviour, IInventoryOwner, IProjectileTarget
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private Health _health;
        [SerializeField] private MovementLogic _movementLogic;
        [SerializeField] private ShootingLogic _shootingLogic;
        [SerializeField] private CharacterAnimationControl _animationControl;
        [SerializeField] private float _aimHeight = 1f;
        [SerializeField] private float _healthRegenerationDelay = 3f;
        [SerializeField] private float _healthRegenerationPerSecond = 20f;

        private FloatingJoystick _joystick;
        private Rigidbody _rigidbody;
        private bool _isGameplayActive;
        private float _lastCombatActivityTime;

        public Inventory Inventory => _inventory;
        public Health Health => _health;
        public ShootingLogic ShootingLogic => _shootingLogic;
        public MovementLogic MovementLogic => _movementLogic;
        public bool IsAlive { get; private set; }
        public Vector3 AimPosition => transform.position + Vector3.up * _aimHeight;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Init(FloatingJoystick joystick)
        {
            _joystick = joystick;
            _health.Init();
            _inventory.Init();
            _movementLogic.Init(_rigidbody);
            _shootingLogic.Init(_rigidbody, _movementLogic);
            _animationControl.Init(_movementLogic);
            _isGameplayActive = true;
            IsAlive = true;
            _lastCombatActivityTime = Time.time;

            Subscribes();
        }

        private void Update()
        {
            if (!_isGameplayActive)
            {
                return;
            }

            _animationControl.SetShooting(_shootingLogic.IsShooting);
            _animationControl.Tick(Time.deltaTime);
            _inventory.Tick(transform.position);

            if (_shootingLogic.IsShooting)
            {
                _lastCombatActivityTime = Time.time;
            }

            RegenerateHealth();
        }

        private void FixedUpdate()
        {
            if (!_isGameplayActive)
            {
                return;
            }

            _movementLogic.Move(_joystick.Direction, Time.fixedDeltaTime);
            _shootingLogic.Tick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            Unsubscribes();
        }


        private void Subscribes()
        {
            _health.OnDeath += HandleDeath;
        }

        private void Unsubscribes()
        {
            _health.OnDeath -= HandleDeath;
        }

        public void PlayVictory()
        {
            StopGameplay();
            _animationControl.PlayVictory();
        }

        private void HandleDeath()
        {
            IsAlive = false;
            _inventory.DropAll(transform.position);
            StopGameplay();
            _animationControl.PlayDeath();
        }

        public void ApplyDamage(float damage)
        {
            _lastCombatActivityTime = Time.time;
            _health.ApplyDamage(damage);
        }

        private void RegenerateHealth()
        {
            if (Time.time < _lastCombatActivityTime + _healthRegenerationDelay ||
                _health.CurrentHealth >= _health.MaxHealth)
            {
                return;
            }

            _health.Heal(_healthRegenerationPerSecond * Time.deltaTime);
        }

        private void StopGameplay()
        {
            _isGameplayActive = false;
            _movementLogic.Stop();
            _shootingLogic.Stop();
        }
    }
}
