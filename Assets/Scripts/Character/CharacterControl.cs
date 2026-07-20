using Cameras;
using Character.Ragdoll;
using UnityEngine;

namespace Character
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControl : MonoBehaviour, ICapturableCharacter
    {
        public enum CharacterState
        {
            Active,
            Captured,
            Thrown,
            Recovering
        }

        [SerializeField] private Animator _animator;
        [SerializeField] private MovementLogic _movementLogic = new();
        [SerializeField] private CharacterEffectsLogic _effectsLogic = new();
        [SerializeField] private CharacterCaptureLogic _captureLogic = new();
        [SerializeField] private CharacterRagdollLogic _ragdollLogic = new();

        private CharacterController _characterController;
        private CameraControl _cameraControl;
        private CharacterState _state;
        private bool _isMovementLocked;

        public bool CanBeCaptured => _state == CharacterState.Active;

        public void Initialize(CameraControl cameraControl)
        {
            _cameraControl = cameraControl;
            _characterController = GetComponent<CharacterController>();
            _animator.applyRootMotion = false;
            EnsureAnimationEventReceiver();
            _movementLogic.Initialize(transform, _characterController, _cameraControl, _animator);
            _effectsLogic.Initialize();
            _ragdollLogic.Initialize(transform, _animator);
            _captureLogic.Initialize(this, transform, _characterController, _cameraControl,
                _ragdollLogic, _effectsLogic);
        }

        private void Update()
        {
            UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (_state != CharacterState.Active || _isMovementLocked)
            {
                return;
            }

            _movementLogic.UpdateMovement();
            _effectsLogic.UpdateStepDust(_movementLogic.IsMovingOnGround);
        }

        public bool TryBeginCapture()
        {
            if (!CanBeCaptured)
            {
                return false;
            }

            _movementLogic.StopMovement();
            _captureLogic.BeginCapture();
            _state = CharacterState.Captured;
            return true;
        }

        public void SetCapturedPose(Vector3 position, Quaternion rotation)
        {
            _captureLogic.SetCapturedPose(position, rotation);
        }

        public void SetMovementLocked(bool isLocked)
        {
            _isMovementLocked = isLocked;

            if (_isMovementLocked)
            {
                _movementLogic.StopMovement();
                _effectsLogic.StopStepDust();
            }
        }

        public void Throw(Vector3 velocity, Vector3 angularVelocity)
        {
            _state = CharacterState.Thrown;
            _captureLogic.Throw(velocity);
        }

        public void CancelCapture()
        {
            if (_state != CharacterState.Captured)
            {
                return;
            }

            _captureLogic.RestoreActiveState();
        }

        internal void SetState(CharacterState state)
        {
            _state = state;
        }

        private void EnsureAnimationEventReceiver()
        {
            if (_animator.GetComponent<CharacterAnimationEventReceiver>() != null)
            {
                return;
            }

            _animator.gameObject.AddComponent<CharacterAnimationEventReceiver>();
        }

        private void OnDestroy()
        {
            _captureLogic.Dispose();
        }
    }
}
