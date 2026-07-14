using System;
using UI.Managers;
using UI.Popups;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.Character
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterControl : MonoBehaviour, IInventoryOwner, IProjectileTarget
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private CharacterHealth _health;
        [SerializeField] private MovementLogic _movementLogic;
        [SerializeField] private ShootingLogic _shootingLogic;
        [SerializeField] private CharacterAnimationControl _animationControl;
        [SerializeField] private ProjectileHitFeedback _hitFeedback;
        [SerializeField] private float _aimHeight = 1f;

        private FloatingJoystick _joystick;
        private CameraControl _cameraControl;
        private Rigidbody _rigidbody;
        private bool _isGameplayActive;

        public Inventory Inventory => _inventory;
        public Health Health => _health;
        public ShootingLogic ShootingLogic => _shootingLogic;
        public MovementLogic MovementLogic => _movementLogic;
        public bool IsAlive { get; private set; }
        public int LifeVersion { get; private set; }
        public Vector3 AimPosition => transform.position + Vector3.up * _aimHeight;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Init(FloatingJoystick joystick, CameraControl cameraControl)
        {
            _joystick = joystick;
            _cameraControl = cameraControl;
            _health.Init();
            _inventory.Init();
            _movementLogic.Init(_rigidbody);
            _shootingLogic.Init(_rigidbody, _movementLogic);
            _animationControl.Init(_movementLogic);
            _isGameplayActive = true;
            IsAlive = true;

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
                _health.RegisterCombatActivity();
            }

            _health.Tick(Time.deltaTime);
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

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();

            _health.Init();
            _movementLogic.Stop();
            _animationControl.ResetToIdle();
            IsAlive = true;
            _isGameplayActive = true;
        }

        private void HandleDeath()
        {
            IsAlive = false;
            LifeVersion++;
            _inventory.DropAll(transform.position);
            StopGameplay();
            _animationControl.PlayDeath();
        }

        public void ApplyDamage(float damage)
        {
            _health.ApplyDamage(damage);
        }

        public void PlayHitFeedback(Vector3 hitPosition)
        {
            _hitFeedback.Play(hitPosition);
            _cameraControl.ShakeOnDamage();
        }

        private void StopGameplay()
        {
            _isGameplayActive = false;
            _movementLogic.Stop();
            _shootingLogic.Stop();
        }
    }
}
