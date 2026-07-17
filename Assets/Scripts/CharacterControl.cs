using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Renderer))]
public class CharacterControl : MonoBehaviour
{
    private enum CharacterState
    {
        Active,
        Captured,
        Thrown,
        Recovering
    }

    [Header("Movement")]
    [SerializeField] private CameraControl _cameraControl;
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _sprintSpeed = 7f;
    [SerializeField] private float _acceleration = 18f;
    [SerializeField] private float _rotationSharpness = 18f;

    [Header("Recovery")]
    [SerializeField] private float _minimumRecoveryDelay = 0.65f;
    [SerializeField] private float _maximumRecoveryDuration = 4f;
    [SerializeField] private float _groundCheckDistance = 1.2f;
    [SerializeField] private float _uprightDuration = 0.35f;
    [SerializeField] private LayerMask _groundLayers = ~0;

    [Header("Visual")]
    [SerializeField] private Color _characterColor = new(0.45f, 1f, 0.35f, 1f);

    private Rigidbody _rigidbody;
    private Collider _collider;
    private Renderer _renderer;
    private RigidbodyConstraints _defaultConstraints;
    private CharacterState _state;
    private Coroutine _recoveryRoutine;
    private Vector3 _horizontalVelocity;

    public bool CanBeCaptured => _state == CharacterState.Active;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _renderer = GetComponent<Renderer>();
        _defaultConstraints = _rigidbody.constraints;
        SetCharacterColor();
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    public bool TryBeginCapture()
    {
        if (!CanBeCaptured)
        {
            return false;
        }

        _state = CharacterState.Captured;
        _horizontalVelocity = Vector3.zero;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        return true;
    }

    public void SetCapturedPose(Vector3 position, Quaternion rotation)
    {
        _rigidbody.position = position;
        _rigidbody.rotation = rotation;
    }

    public void Throw(Vector3 velocity, Vector3 angularVelocity)
    {
        _state = CharacterState.Thrown;
        _rigidbody.isKinematic = false;
        _rigidbody.constraints = _defaultConstraints;
        _rigidbody.velocity = velocity;
        _rigidbody.angularVelocity = Vector3.zero;
        _recoveryRoutine = StartCoroutine(RecoverAfterThrow());
    }

    public void CancelCapture()
    {
        if (_state != CharacterState.Captured)
        {
            return;
        }

        _rigidbody.isKinematic = false;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.constraints = _defaultConstraints;
        _horizontalVelocity = Vector3.zero;
        _state = CharacterState.Active;
    }

    private void UpdateMovement()
    {
        if (_state != CharacterState.Active)
        {
            return;
        }

        Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 targetDirection = _cameraControl.GetMovementForward() * input.y +
                                  _cameraControl.GetMovementRight() * input.x;
        targetDirection = Vector3.ClampMagnitude(targetDirection, 1f);
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? _sprintSpeed : _moveSpeed;
        Vector3 targetVelocity = targetDirection * moveSpeed;

        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity,
            _acceleration * Time.fixedDeltaTime);

        Vector3 velocity = _rigidbody.velocity;
        velocity.x = _horizontalVelocity.x;
        velocity.z = _horizontalVelocity.z;
        _rigidbody.velocity = velocity;

        UpdateRotation(targetDirection);
    }

    private void UpdateRotation(Vector3 targetDirection)
    {
        Quaternion targetRotation = _cameraControl.IsFirstPerson
            ? _cameraControl.GetYawRotation()
            : GetThirdPersonRotation(targetDirection);

        _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation,
            1f - Mathf.Exp(-_rotationSharpness * Time.fixedDeltaTime)));
    }

    private Quaternion GetThirdPersonRotation(Vector3 targetDirection)
    {
        if (targetDirection.sqrMagnitude < 0.001f)
        {
            return _rigidbody.rotation;
        }

        return Quaternion.LookRotation(targetDirection, Vector3.up);
    }

    private IEnumerator RecoverAfterThrow()
    {
        yield return new WaitForSeconds(_minimumRecoveryDelay);

        float recoveryTimer = 0f;

        while (!IsGrounded() && recoveryTimer < _maximumRecoveryDuration)
        {
            recoveryTimer += Time.deltaTime;
            yield return null;
        }

        _state = CharacterState.Recovering;

        if (!_rigidbody.isKinematic)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _rigidbody.isKinematic = true;

        Quaternion startRotation = _rigidbody.rotation;
        Quaternion uprightRotation = Quaternion.Euler(0f, startRotation.eulerAngles.y, 0f);
        float uprightTimer = 0f;

        while (uprightTimer < _uprightDuration)
        {
            uprightTimer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, uprightTimer / _uprightDuration);
            _rigidbody.rotation = Quaternion.Slerp(startRotation, uprightRotation, progress);
            yield return null;
        }

        _rigidbody.rotation = uprightRotation;
        _rigidbody.constraints = _defaultConstraints;
        _rigidbody.isKinematic = false;
        _horizontalVelocity = Vector3.zero;
        _state = CharacterState.Active;
        _recoveryRoutine = null;
    }

    private bool IsGrounded()
    {
        Vector3 rayOrigin = _collider.bounds.center;
        return Physics.Raycast(rayOrigin, Vector3.down, _groundCheckDistance, _groundLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void SetCharacterColor()
    {
        MaterialPropertyBlock propertyBlock = new();
        _renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", _characterColor);
        propertyBlock.SetColor("_Color", _characterColor);
        _renderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDestroy()
    {
        if (_recoveryRoutine != null)
        {
            StopCoroutine(_recoveryRoutine);
        }
    }
}
