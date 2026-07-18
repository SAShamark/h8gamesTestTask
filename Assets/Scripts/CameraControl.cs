using UnityEngine;

public class CameraControl : MonoBehaviour
{
    private const int CameraCollisionHitCapacity = 16;

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
    [SerializeField] private bool _rotateTargetWithFirstPersonCamera = true;

    [Header("Third Person")]
    [SerializeField] private float _thirdPersonDistance = 4.2f;
    [SerializeField] private float _thirdPersonHeight = 1.55f;
    [SerializeField] private float _thirdPersonPitch = 18f;
    [SerializeField] private float _thirdPersonPitchMin = -15f;
    [SerializeField] private float _thirdPersonPitchMax = 55f;
    [SerializeField] private float _thirdPersonPositionSmoothTime = 0.08f;
    [SerializeField] private float _thirdPersonRotationSharpness = 18f;

    [Header("Third Person Collision")]
    [SerializeField] private LayerMask _thirdPersonCollisionLayers = ~0;
    [SerializeField] private float _thirdPersonCollisionRadius = 0.28f;
    [SerializeField] private float _thirdPersonCollisionPadding = 0.12f;
    [SerializeField] private float _thirdPersonBlockedSmoothTime = 0.03f;
    [SerializeField] private float _thirdPersonReturnSmoothTime = 0.12f;

    private CameraViewMode _viewMode = CameraViewMode.ThirdPerson;
    private readonly RaycastHit[] _cameraCollisionHits = new RaycastHit[CameraCollisionHitCapacity];
    private Vector3 _positionVelocity;
    private float _yaw;
    private float _pitch;
    private float _targetYawOffset;
    private bool _isViewModeForced;
    private CameraViewMode _viewModeBeforeForce;

    public bool IsFirstPerson => _viewMode == CameraViewMode.FirstPerson;

    private void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = _thirdPersonPitch;
        _targetYawOffset = _target.eulerAngles.y - _yaw;

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

    public void ForceThirdPersonView()
    {
        if (!_isViewModeForced)
        {
            _viewModeBeforeForce = _viewMode;
            _isViewModeForced = true;
        }

        SetThirdPersonView();
    }

    public void RestoreForcedViewMode()
    {
        if (!_isViewModeForced)
        {
            return;
        }

        if (_viewModeBeforeForce == CameraViewMode.FirstPerson)
        {
            SetFirstPersonView();
        }
        else
        {
            SetThirdPersonView();
        }

        _isViewModeForced = false;
    }

    private void UpdateViewMode()
    {
        if (_isViewModeForced)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetFirstPersonView();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetThirdPersonView();
        }
    }

    private void SetFirstPersonView()
    {
        _viewMode = CameraViewMode.FirstPerson;
        _pitch = Mathf.Clamp(_pitch, _firstPersonPitchMin, _firstPersonPitchMax);
        _positionVelocity = Vector3.zero;
    }

    private void SetThirdPersonView()
    {
        _viewMode = CameraViewMode.ThirdPerson;
        _pitch = Mathf.Clamp(_pitch, _thirdPersonPitchMin, _thirdPersonPitchMax);
        _positionVelocity = Vector3.zero;
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
        RotateTargetWithFirstPersonCamera();
        transform.SetPositionAndRotation(_target.position + _firstPersonOffset,
            Quaternion.Euler(_pitch, _yaw, 0f));
    }

    private void RotateTargetWithFirstPersonCamera()
    {
        if (!_rotateTargetWithFirstPersonCamera)
        {
            return;
        }

        _target.rotation = Quaternion.Euler(0f, _yaw + _targetYawOffset, 0f);
    }

    private void UpdateThirdPersonCamera()
    {
        Vector3 focusPoint = _target.position + _thirdPersonOffset;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * _thirdPersonDistance +
                                  Vector3.up * _thirdPersonHeight;
        Vector3 targetPosition = GetThirdPersonCameraPosition(focusPoint, desiredPosition,
            out bool isBlocked);
        float smoothTime = isBlocked
            ? Mathf.Min(_thirdPersonBlockedSmoothTime, _thirdPersonPositionSmoothTime)
            : Mathf.Max(_thirdPersonReturnSmoothTime, _thirdPersonPositionSmoothTime);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition,
            ref _positionVelocity, smoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation,
            1f - Mathf.Exp(-_thirdPersonRotationSharpness * Time.deltaTime));
    }

    private Vector3 GetThirdPersonCameraPosition(
        Vector3 focusPoint,
        Vector3 desiredPosition,
        out bool isBlocked)
    {
        Vector3 cameraDirection = desiredPosition - focusPoint;
        float desiredDistance = cameraDirection.magnitude;

        if (desiredDistance <= 0.001f)
        {
            isBlocked = false;
            return desiredPosition;
        }

        cameraDirection /= desiredDistance;
        int hitsCount = Physics.SphereCastNonAlloc(focusPoint, _thirdPersonCollisionRadius,
            cameraDirection, _cameraCollisionHits, desiredDistance, _thirdPersonCollisionLayers,
            QueryTriggerInteraction.Ignore);
        float nearestDistance = desiredDistance;
        isBlocked = false;

        for (int i = 0; i < hitsCount; i++)
        {
            RaycastHit hit = _cameraCollisionHits[i];

            if (IsTargetCollider(hit.collider) || hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            isBlocked = true;
        }

        if (!isBlocked)
        {
            return desiredPosition;
        }

        float cameraDistance = Mathf.Max(0f, nearestDistance - _thirdPersonCollisionPadding);
        return focusPoint + cameraDirection * cameraDistance;
    }

    private bool IsTargetCollider(Collider hitCollider)
    {
        return hitCollider.transform == _target ||
               hitCollider.transform.IsChildOf(_target);
    }
}
