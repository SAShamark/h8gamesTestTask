using UnityEngine;

namespace Cameras
{
    public class CameraControl : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private float _mouseSensitivity = 0.6f;
        [SerializeField] private bool _lockCursor = true;

        [Header("Camera Modes")]
        [SerializeField] private FirstPersonCamera _firstPersonCamera = new();
        [SerializeField] private ThirdPersonCamera _thirdPersonCamera = new();
        [SerializeField] private RagdollCamera _ragdollCamera = new();
        [SerializeField] private CameraCollisionLogic _collisionLogic = new();

        private CameraViewMode _viewMode = CameraViewMode.ThirdPerson;
        private CameraViewMode _viewModeBeforeForce;
        private Transform _target;
        private float _yaw;
        private float _pitch;
        private bool _isViewModeForced;

        public bool IsFirstPerson => _viewMode == CameraViewMode.FirstPerson;

        public void Initialize(Transform target)
        {
            _target = target;
            _collisionLogic.Initialize(target);
            _firstPersonCamera.Initialize(transform, target);
            _thirdPersonCamera.Initialize(transform, target, _collisionLogic);
            _ragdollCamera.Initialize(transform, target, _collisionLogic);

            _yaw = transform.eulerAngles.y;
            _pitch = _thirdPersonCamera.DefaultPitch;
            SetCursorState();
        }

        private void Update()
        {
            UpdateViewMode();
            UpdateLookInput();
        }

        private void LateUpdate()
        {
            UpdateActiveCamera();
        }

        public Vector3 GetMovementForward()
        {
            return Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        }

        public Vector3 GetMovementRight()
        {
            return Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
        }

        public Quaternion GetYawRotation()
        {
            return Quaternion.Euler(0f, _yaw, 0f);
        }

        public void ForceRagdollView(Transform ragdollFocus, Vector3 throwVelocity)
        {
            SaveViewModeBeforeForce();
            _viewMode = CameraViewMode.Ragdoll;
            _pitch = _ragdollCamera.DefaultPitch;
            _yaw = _ragdollCamera.GetThrowYaw(throwVelocity);
            _ragdollCamera.SetFocus(ragdollFocus);
        }

        public void ForceCaptureView(Transform ragdollFocus)
        {
            SaveViewModeBeforeForce();
            _viewMode = CameraViewMode.Ragdoll;
            _pitch = _ragdollCamera.DefaultPitch;
            _yaw = _ragdollCamera.GetCurrentCameraYaw(ragdollFocus.position);
            _ragdollCamera.SetFocus(ragdollFocus);
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
            _ragdollCamera.ClearFocus();
        }

        public void SetExtraCollisionIgnoreRoot(Transform ignoreRoot)
        {
            _collisionLogic.SetExtraIgnoreRoot(ignoreRoot);
        }

        private void UpdateActiveCamera()
        {
            switch (_viewMode)
            {
                case CameraViewMode.FirstPerson:
                    _firstPersonCamera.UpdateCamera(_yaw, _pitch);
                    break;
                case CameraViewMode.ThirdPerson:
                    _thirdPersonCamera.UpdateCamera(_yaw, _pitch);
                    break;
                case CameraViewMode.Ragdoll:
                    _ragdollCamera.UpdateCamera(_yaw, _pitch);
                    break;
            }
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

        private void UpdateLookInput()
        {
            _yaw += Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            _pitch -= Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;
            _pitch = GetClampedPitch(_pitch);
        }

        private float GetClampedPitch(float pitch)
        {
            return _viewMode switch
            {
                CameraViewMode.FirstPerson => _firstPersonCamera.ClampPitch(pitch),
                CameraViewMode.Ragdoll => _ragdollCamera.ClampPitch(pitch),
                _ => _thirdPersonCamera.ClampPitch(pitch)
            };
        }

        private void SetFirstPersonView()
        {
            _viewMode = CameraViewMode.FirstPerson;
            _yaw = _target.eulerAngles.y;
            _pitch = _firstPersonCamera.ClampPitch(_pitch);
        }

        private void SetThirdPersonView()
        {
            _viewMode = CameraViewMode.ThirdPerson;
            _pitch = _thirdPersonCamera.ClampPitch(_pitch);
        }

        private void SaveViewModeBeforeForce()
        {
            if (_isViewModeForced)
            {
                return;
            }

            _viewModeBeforeForce = _viewMode;
            _isViewModeForced = true;
        }

        private void SetCursorState()
        {
            if (!_lockCursor)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}