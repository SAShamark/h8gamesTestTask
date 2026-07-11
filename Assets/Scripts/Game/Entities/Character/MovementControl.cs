using System;
using UnityEngine;

namespace Game.Entities.Character
{
    [Serializable]
    public class MovementControl
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _acceleration = 18f;
        [SerializeField] private float _deceleration = 24f;
        [SerializeField] private float _rotationSpeed = 12f;
        [SerializeField] private float _inputDeadZone = 0.05f;

        private Transform _transform;
        private Vector3 _velocity;

        public Vector3 Velocity => _velocity;
        public float NormalizedSpeed => Mathf.InverseLerp(0f, _moveSpeed, _velocity.magnitude);

        public void Init(Transform transform)
        {
            _transform = transform;
        }

        public void Move(Vector2 input, float deltaTime)
        {
            Vector3 targetVelocity = Vector3.zero;

            if (input.sqrMagnitude >= _inputDeadZone * _inputDeadZone)
            {
                Vector3 inputDirection = new Vector3(input.x, 0f, input.y);
                if (inputDirection.sqrMagnitude > 1f)
                {
                    inputDirection.Normalize();
                }

                targetVelocity = inputDirection * _moveSpeed;
            }

            float velocityChangeSpeed = targetVelocity.sqrMagnitude > 0f ? _acceleration : _deceleration;
            _velocity = Vector3.MoveTowards(_velocity, targetVelocity, velocityChangeSpeed * deltaTime);

            _transform.position += _velocity * deltaTime;

            if (_velocity.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(_velocity.normalized);
            _transform.rotation = Quaternion.Slerp(
                _transform.rotation, targetRotation, _rotationSpeed * deltaTime);
        }
    }
}
