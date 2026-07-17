using UnityEngine;

public class CameraControl : MonoBehaviour
{
    private enum CameraViewMode
    {
        FirstPerson,
        ThirdPerson
    }

    [Header("Target")]
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _firstPersonOffset = new(0f, 0.65f, 0f);
    [SerializeField] private Vector3 _thirdPersonOffset = new(0f, 1.25f, 0f);

    [Header("Input")]
    [SerializeField] private float _mouseSensitivity = 2.5f;
    [SerializeField] private bool _lockCursor = true;

    [Header("First Person")]
    [SerializeField] private float _firstPersonPitchMin = -75f;
    [SerializeField] private float _firstPersonPitchMax = 75f;

    [Header("Third Person")]
    [SerializeField] private float _thirdPersonDistance = 4.2f;
    [SerializeField] private float _thirdPersonHeight = 1.55f;
    [SerializeField] private float _thirdPersonPitch = 18f;
    [SerializeField] private float _thirdPersonPitchMin = -15f;
    [SerializeField] private float _thirdPersonPitchMax = 55f;
    [SerializeField] private float _thirdPersonPositionSmoothTime = 0.08f;
    [SerializeField] private float _thirdPersonRotationSharpness = 18f;

    private CameraViewMode _viewMode = CameraViewMode.ThirdPerson;
    private Vector3 _positionVelocity;
    private float _yaw;
    private float _pitch;

    public bool IsFirstPerson => _viewMode == CameraViewMode.FirstPerson;

    private void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = _thirdPersonPitch;

        if (_lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        UpdateViewMode();
        UpdateLookInput();
    }

    private void LateUpdate()
    {
        if (IsFirstPerson)
        {
            UpdateFirstPersonCamera();
        }
        else
        {
            UpdateThirdPersonCamera();
        }
    }

    public Vector3 GetMovementForward()
    {
        Vector3 forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    public Vector3 GetMovementRight()
    {
        Vector3 right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
        right.y = 0f;
        return right.normalized;
    }

    public Quaternion GetYawRotation()
    {
        return Quaternion.Euler(0f, _yaw, 0f);
    }

    private void UpdateViewMode()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _viewMode = CameraViewMode.FirstPerson;
            _pitch = Mathf.Clamp(_pitch, _firstPersonPitchMin, _firstPersonPitchMax);
            _positionVelocity = Vector3.zero;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _viewMode = CameraViewMode.ThirdPerson;
            _pitch = Mathf.Clamp(_pitch, _thirdPersonPitchMin, _thirdPersonPitchMax);
            _positionVelocity = Vector3.zero;
        }
    }

    private void UpdateLookInput()
    {
        _yaw += Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
        _pitch -= Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

        if (IsFirstPerson)
        {
            _pitch = Mathf.Clamp(_pitch, _firstPersonPitchMin, _firstPersonPitchMax);
            return;
        }

        _pitch = Mathf.Clamp(_pitch, _thirdPersonPitchMin, _thirdPersonPitchMax);
    }

    private void UpdateFirstPersonCamera()
    {
        transform.SetPositionAndRotation(_target.position + _firstPersonOffset,
            Quaternion.Euler(_pitch, _yaw, 0f));
    }

    private void UpdateThirdPersonCamera()
    {
        Vector3 focusPoint = _target.position + _thirdPersonOffset;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 targetPosition = focusPoint - rotation * Vector3.forward * _thirdPersonDistance +
                                 Vector3.up * _thirdPersonHeight;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition,
            ref _positionVelocity, _thirdPersonPositionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation,
            1f - Mathf.Exp(-_thirdPersonRotationSharpness * Time.deltaTime));
    }
}
