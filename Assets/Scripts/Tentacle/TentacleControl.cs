using UnityEngine;

namespace Tentacle
{
    [RequireComponent(typeof(Animator))]
    public class TentacleControl : MonoBehaviour
    {
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");

        [Header("References")]
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _model;
        [SerializeField] private Animator _animator;

        [Header("Detection")]
        [SerializeField] private float _alertRadius = 12f;
        [SerializeField] private float _actionRadius = 6f;

        [Header("Animation")]
        [SerializeField] private string _idleStateName = "IdleA";

        [Header("Rotation")]
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _rotationSmoothTime = 0.15f;

        [Header("Reach")]
        [SerializeField] private TentaclePoseSolver _poseSolver = new();

        [Header("Lift")]
        [SerializeField] private float _liftDuration = 1.1f;
        [SerializeField] private float _liftHeight = 2.5f;
        [SerializeField, Range(0f, 90f)] private float _liftTiltAngle = 80f;

        [Header("Throw")]
        [SerializeField, Range(0.5f, 1f)] private float _throwBackswingLiftStart = 0.78f;
        [SerializeField] private float _throwBackswingDuration = 0.55f;
        [SerializeField] private float _throwBackswingAngle = 55f;
        [SerializeField] private float _throwForwardAngle = 13f;
        [SerializeField] private float _throwHorizontalSpeed = 7f;
        [SerializeField] private float _throwUpwardSpeed = 2.5f;

        private ICapturableCharacter _capturableCharacter;
        private float _rotationVelocity;
        private float _liftProgress;
        private float _releaseTime;
        private Vector3 _captureStartPosition;
        private Vector3 _captureTargetPosition;
        private Vector3 _captureTargetCenter;
        private Vector3 _throwAxis;
        private Vector3 _throwDirection;
        private Quaternion _captureStartRotation;
        private Quaternion _captureTargetRotation;
        private bool _isAlert;
        private bool _isCapturing;
        private bool _isReleasing;
        private bool _isAnimatorLockedForProceduralMotion;
        private bool _hasThrown;

        public bool IsTargetInActionRadius { get; private set; }

        private void Awake()
        {
            _capturableCharacter = _target.GetComponent<ICapturableCharacter>();
        }

        private void Start()
        {
            _animator.Play(_idleStateName, 0, 0f);
            _animator.Update(0f);
            _poseSolver.Initialize(_model.Find("DeformationSystem/Root_M"),
                transform.position.y);
            UpdateDetection();
        }

        private void Update()
        {
            UpdateDetection();
            UpdateRotation();
        }

        private void LateUpdate()
        {
            bool shouldReach = IsTargetInActionRadius && !_hasThrown;
            LockAnimatorForProceduralMotion(shouldReach);
            StartCaptureAfterWrapping();
            UpdateLift();
            UpdateRelease();
            _poseSolver.UpdatePose(shouldReach, _target, Time.deltaTime);
            UnlockAnimatorAfterProceduralMotion();
            ResetCycleAfterRecovery();
        }

        private void StartCaptureAfterWrapping()
        {
            if (_isCapturing || !_poseSolver.IsWrapped ||
                !_capturableCharacter.TryBeginCapture())
            {
                return;
            }

            _isCapturing = true;
            _captureStartPosition = _target.position;
            _captureStartRotation = _target.rotation;

            Vector3 direction = Vector3.ProjectOnPlane(
                _target.position - transform.position, Vector3.up).normalized;
            _throwDirection = direction;
            _throwAxis = Vector3.Cross(Vector3.up, direction);
            _captureTargetRotation = Quaternion.AngleAxis(
                _liftTiltAngle, _throwAxis) * _captureStartRotation;

            Vector3 startCenter = _captureStartPosition +
                                  _captureStartRotation * Vector3.up *
                                  _poseSolver.TargetCenterOffset;
            _captureTargetCenter = new Vector3(
                transform.position.x,
                startCenter.y + _liftHeight,
                transform.position.z);
            _captureTargetPosition = _captureTargetCenter -
                                     _captureTargetRotation * Vector3.up *
                                     _poseSolver.TargetCenterOffset;
        }

        private void UpdateLift()
        {
            if (!_isCapturing || _liftProgress >= 1f)
            {
                return;
            }

            _liftProgress = Mathf.MoveTowards(
                _liftProgress, 1f, Time.deltaTime / _liftDuration);
            GetLiftPose(out Vector3 position, out Quaternion rotation);
            _capturableCharacter.SetCapturedPose(position, rotation);
        }

        private void GetLiftPose(out Vector3 position, out Quaternion rotation)
        {
            float progress = Mathf.SmoothStep(0f, 1f, _liftProgress);
            position = Vector3.Lerp(
                _captureStartPosition, _captureTargetPosition, progress);
            rotation = Quaternion.Slerp(
                _captureStartRotation, _captureTargetRotation, progress);
        }

        private void UpdateRelease()
        {
            if (!_isCapturing || _liftProgress < _throwBackswingLiftStart)
            {
                return;
            }

            if (!_isReleasing)
            {
                UpdateThrowBackswing();
                return;
            }

            float releaseProgress = _poseSolver.ReleaseProgress;
            float forwardProgress = releaseProgress * releaseProgress;
            float releaseAngle = Mathf.Lerp(
                -_throwBackswingAngle, _throwForwardAngle, forwardProgress);
            SetReleasePose(releaseAngle);

            if (!_poseSolver.IsUnwrapped)
            {
                return;
            }

            ThrowCharacter();
        }

        private void UpdateThrowBackswing()
        {
            _releaseTime = Mathf.MoveTowards(
                _releaseTime, _throwBackswingDuration, Time.deltaTime);
            float progress = _releaseTime / _throwBackswingDuration;
            float angle = -Mathf.Sin(progress * Mathf.PI * 0.5f) *
                          _throwBackswingAngle;
            SetReleasePose(angle);

            if (_releaseTime < _throwBackswingDuration)
            {
                return;
            }

            _isReleasing = true;
            _poseSolver.BeginRelease();
        }

        private void SetReleasePose(float angle)
        {
            Quaternion releaseRotation = Quaternion.AngleAxis(angle, _throwAxis);
            Vector3 pivot = transform.position;
            GetLiftPose(out Vector3 liftPosition, out Quaternion liftRotation);
            Vector3 liftCenter = liftPosition + liftRotation * Vector3.up *
                                 _poseSolver.TargetCenterOffset;
            Vector3 center = pivot +
                             releaseRotation * (liftCenter - pivot);
            Quaternion rotation = releaseRotation * liftRotation;
            Vector3 position = center - rotation * Vector3.up *
                               _poseSolver.TargetCenterOffset;
            _capturableCharacter.SetCapturedPose(position, rotation);
        }

        private void ThrowCharacter()
        {
            Vector3 velocity = _throwDirection * _throwHorizontalSpeed +
                               Vector3.up * _throwUpwardSpeed;
            _capturableCharacter.Throw(velocity, Vector3.zero);
            _poseSolver.BeginFollowThrough(_throwAxis);
            _hasThrown = true;
            _isCapturing = false;
            _isReleasing = false;
        }

        private void LockAnimatorForProceduralMotion(bool shouldReach)
        {
            if (!shouldReach || _isAnimatorLockedForProceduralMotion)
            {
                return;
            }

            _isAnimatorLockedForProceduralMotion = true;
            _animator.enabled = false;
        }

        private void UnlockAnimatorAfterProceduralMotion()
        {
            if (!_isAnimatorLockedForProceduralMotion ||
                !_poseSolver.IsIdle)
            {
                return;
            }

            _isAnimatorLockedForProceduralMotion = false;
            _animator.enabled = true;
            UpdateDetection();
        }

        private void ResetCycleAfterRecovery()
        {
            if (!_hasThrown || !_poseSolver.IsIdle ||
                !_capturableCharacter.CanBeCaptured)
            {
                return;
            }

            _liftProgress = 0f;
            _releaseTime = 0f;
            _hasThrown = false;
        }

        private void UpdateDetection()
        {
            float sqrDistance = GetFlatSqrDistanceToTarget();
            IsTargetInActionRadius = sqrDistance <= _actionRadius * _actionRadius;

            if (_isAnimatorLockedForProceduralMotion)
            {
                return;
            }

            SetAlert(sqrDistance <= _alertRadius * _alertRadius);
        }

        private void SetAlert(bool isAlert)
        {
            if (_isAlert == isAlert)
            {
                return;
            }

            _isAlert = isAlert;
            _animator.SetBool(IsAlertHash, isAlert);
        }

        private void UpdateRotation()
        {
            if (!_isAlert || _isCapturing ||
                _isAnimatorLockedForProceduralMotion)
            {
                return;
            }

            Vector3 direction = _target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float targetYaw = Quaternion.LookRotation(direction).eulerAngles.y;
            float yaw = Mathf.SmoothDampAngle(_model.eulerAngles.y, targetYaw,
                ref _rotationVelocity, _rotationSmoothTime, _rotationSpeed, Time.deltaTime);
            _model.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private float GetFlatSqrDistanceToTarget()
        {
            Vector3 offset = _target.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _alertRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _actionRadius);
        }
    }
}
