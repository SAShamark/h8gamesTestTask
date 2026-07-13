using System;
using UnityEngine;

namespace Game.Entities.Character
{
    [Serializable]
    public class MovementLogic
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _acceleration = 18f;
        [SerializeField] private float _deceleration = 24f;
        [SerializeField] private float _rotationSpeed = 12f;
        [SerializeField] private float _inputDeadZone = 0.05f;
        [SerializeField] private float _stationarySpeed = 0.05f;

        private Rigidbody _rigidbody;
        private Vector3 _velocity;

        public Vector3 Velocity => _velocity;
        public float NormalizedSpeed => Mathf.InverseLerp(0f, _moveSpeed, _velocity.magnitude);
        public bool IsMoving => _velocity.sqrMagnitude > _stationarySpeed * _stationarySpeed;

        public void Init(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
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

            Vector3 rigidbodyVelocity = _rigidbody.linearVelocity;
            rigidbodyVelocity.x = _velocity.x;
            rigidbodyVelocity.z = _velocity.z;
            _rigidbody.linearVelocity = rigidbodyVelocity;

            if (!IsMoving)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(_velocity.normalized);
            Quaternion rotation = Quaternion.Slerp(
                _rigidbody.rotation, targetRotation, _rotationSpeed * deltaTime);
            _rigidbody.MoveRotation(rotation);
        }

        public void Stop()
        {
            _velocity = Vector3.zero;
            Vector3 rigidbodyVelocity = _rigidbody.linearVelocity;
            rigidbodyVelocity.x = 0f;
            rigidbodyVelocity.z = 0f;
            _rigidbody.linearVelocity = rigidbodyVelocity;
        }
    }
}
