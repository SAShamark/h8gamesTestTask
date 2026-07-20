using System;
using Cameras;
using UnityEngine;

namespace Character
{
    [Serializable]
    public class MovementLogic
    {
        private static readonly int MovingHash = Animator.StringToHash("Moving");
        private static readonly int VelocityXHash = Animator.StringToHash("Velocity X");
        private static readonly int VelocityZHash = Animator.StringToHash("Velocity Z");

        [Header("Speed")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;

        [Header("Feel")]
        [SerializeField] private float _acceleration = 22f;
        [SerializeField] private float _rotationSharpness = 18f;
        [SerializeField] private float _gravity = -25f;

        private Transform _transform;
        private CharacterController _characterController;
        private CameraControl _cameraControl;
        private Animator _animator;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        public bool IsMovingOnGround => _characterController.isGrounded &&
                                        _horizontalVelocity.sqrMagnitude > 0.2f;

        public void Initialize(
            Transform characterTransform,
            CharacterController characterController,
            CameraControl cameraControl,
            Animator animator)
        {
            _transform = characterTransform;
            _characterController = characterController;
            _cameraControl = cameraControl;
            _animator = animator;
        }

        public void UpdateMovement()
        {
            Vector2 moveInput = ReadMoveInput();
            Vector3 moveDirection = GetMoveDirection(moveInput);
            float speed = Input.GetKey(KeyCode.LeftShift) ? _sprintSpeed : _walkSpeed;
            Vector3 targetVelocity = moveDirection * speed;

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity,
                _acceleration * Time.deltaTime);
            _verticalVelocity = GetVerticalVelocity();

            Vector3 velocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);

            UpdateRotation(moveDirection);
            UpdateAnimator();
        }

        public void StopMovement()
        {
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            UpdateAnimator();
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return Vector2.ClampMagnitude(input, 1f);
        }

        private Vector3 GetMoveDirection(Vector2 moveInput)
        {
            Vector3 direction = _cameraControl.GetMovementForward() * moveInput.y +
                                _cameraControl.GetMovementRight() * moveInput.x;
            return Vector3.ClampMagnitude(direction, 1f);
        }

        private float GetVerticalVelocity()
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                return -2f;
            }

            return _verticalVelocity + _gravity * Time.deltaTime;
        }

        private void UpdateRotation(Vector3 moveDirection)
        {
            Quaternion targetRotation = _cameraControl.IsFirstPerson
                ? _cameraControl.GetYawRotation()
                : GetMoveRotation(moveDirection);

            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation,
                1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime));
        }

        private Quaternion GetMoveRotation(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude < 0.001f)
            {
                return _transform.rotation;
            }

            return Quaternion.LookRotation(moveDirection, Vector3.up);
        }

        private void UpdateAnimator()
        {
            Vector3 localVelocity = _transform.InverseTransformDirection(_horizontalVelocity) / _walkSpeed;
            _animator.SetFloat(VelocityXHash, localVelocity.x);
            _animator.SetFloat(VelocityZHash, localVelocity.z);
            _animator.SetBool(MovingHash, _horizontalVelocity.sqrMagnitude > 0.01f);
        }
    }
}
