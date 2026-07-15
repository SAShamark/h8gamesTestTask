using System;
using UnityEngine;

namespace Game.Entities.Units.Character.Parts
{
    [Serializable]
    public class CharacterAnimationControl
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDampTime = 0.12f;
        [SerializeField] private float _movingThreshold = 0.08f;

        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _isMovingHash = Animator.StringToHash("IsMoving");
        private readonly int _isShootingHash = Animator.StringToHash("IsShooting");
        private readonly int _deathHash = Animator.StringToHash("Died");
        private readonly int _victoryHash = Animator.StringToHash("Victory");
        
        private MovementLogic _movementLogic;

        public void Init(MovementLogic movementLogic)
        {
            _movementLogic = movementLogic;
        }

        public void Tick(float deltaTime)
        {
            float speed = _movementLogic.NormalizedSpeed;
            _animator.SetFloat(_speedHash, speed, _speedDampTime, deltaTime);
            _animator.SetBool(_isMovingHash, speed > _movingThreshold);
        }

        public void SetShooting(bool isShooting)
        {
            _animator.SetBool(_isShootingHash, isShooting);
        }

        public void PlayDeath()
        {
            StopLocomotion();
            _animator.SetTrigger(_deathHash);
        }

        public void PlayVictory()
        {
            StopLocomotion();
            _animator.SetTrigger(_victoryHash);
        }

        public void ResetToIdle()
        {
            _animator.Rebind();
            _animator.Update(0f);
            StopLocomotion();
        }

        private void StopLocomotion()
        {
            _animator.SetFloat(_speedHash, 0f);
            _animator.SetBool(_isMovingHash, false);
            _animator.SetBool(_isShootingHash, false);
        }
    }
}
