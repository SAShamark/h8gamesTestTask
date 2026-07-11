using System;
using UnityEngine;

namespace Game.Entities.Character
{
    [Serializable]
    public class CharacterAnimationControl
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDampTime = 0.12f;
        [SerializeField] private float _movingThreshold = 0.08f;

        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _isMovingHash = Animator.StringToHash("IsMoving");
        
        private MovementControl _movementControl;

        public void Init(MovementControl movementControl)
        {
            _movementControl = movementControl;
        }

        public void Tick(float deltaTime)
        {
            float speed = _movementControl.NormalizedSpeed;
            _animator.SetFloat(_speedHash, speed, _speedDampTime, deltaTime);
            _animator.SetBool(_isMovingHash, speed > _movingThreshold);
        }
    }
}