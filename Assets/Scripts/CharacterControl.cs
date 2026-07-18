using UnityEngine;

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

    [Header("Links")]
    [SerializeField] private CameraControl _cameraControl;
    [SerializeField] private Animator _animator;

    [Header("Logic")]
    [SerializeField] private MovementLogic _movementLogic = new();
    [SerializeField] private CharacterCaptureLogic _captureLogic = new();
    [SerializeField] private CharacterRagdollLogic _ragdollLogic = new();

    private CharacterController _characterController;
    private CharacterState _state;

    public bool CanBeCaptured => _state == CharacterState.Active;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator.applyRootMotion = false;
        EnsureAnimationEventReceiver();
        _movementLogic.Initialize(transform, _characterController, _cameraControl, _animator);
        _ragdollLogic.Initialize(transform, _animator);
        _captureLogic.Initialize(this, transform, _characterController, _cameraControl,
            _ragdollLogic);
    }

    private void Update()
    {
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (_state != CharacterState.Active)
        {
            return;
        }

        _movementLogic.UpdateMovement();
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
