using UnityEngine;

namespace Tentacle
{
    [RequireComponent(typeof(Animator))]
    public class TentacleControl : MonoBehaviour
    {
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");

        [SerializeField] private Transform _target;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _modelTransform;
        [SerializeField] private float _detectionRadius = 12f;
        [SerializeField] private float _detectionExitPadding = 1.25f;
        [SerializeField] private float _minimumAlertTimeBeforeGrab = 0.3f;
        [SerializeField] private float _postThrowCooldown = 0.65f;
        [SerializeField] private float _rotationSpeed = 540f;
        [SerializeField] private float _rotationSmoothTime = 0.16f;
        [SerializeField] private CaptureLogic _captureLogic = new();

        private bool _isAlert;
        private bool _wasCaptureBusy;
        private float _alertTimer;
        private float _postThrowCooldownTimer;
        private float _rotationVelocity;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_modelTransform == null)
            {
                _modelTransform = transform;
            }

            _captureLogic ??= new CaptureLogic();
        }

        private void Update()
        {
            if (_target == null)
            {
                SetAlert(false);
                _alertTimer = 0f;
                return;
            }

            Vector3 targetPosition = _target.position;

            if (_captureLogic.IsBusy)
            {
                _wasCaptureBusy = true;

                if (_captureLogic.CanCancelForDistance &&
                    !_captureLogic.IsInGrabRadius(transform.position, targetPosition))
                {
                    _captureLogic.StopGrab(this, _animator);
                }

                return;
            }

            if (_wasCaptureBusy)
            {
                _wasCaptureBusy = false;
                _postThrowCooldownTimer = _postThrowCooldown;
                _alertTimer = 0f;
                SetAlert(false);
                return;
            }

            if (_postThrowCooldownTimer > 0f)
            {
                _postThrowCooldownTimer -= Time.deltaTime;
                _alertTimer = 0f;
                SetAlert(false);
                return;
            }

            bool isTargetInRange = IsInDetectionRadius(targetPosition);
            SetAlert(isTargetInRange);

            if (isTargetInRange)
            {
                _alertTimer += Time.deltaTime;
                RotateTowards(targetPosition);
            }
            else
            {
                _alertTimer = 0f;
            }

            if (_alertTimer >= _minimumAlertTimeBeforeGrab &&
                _captureLogic.IsInGrabRadius(transform.position, targetPosition))
            {
                _captureLogic.TryStartGrab(this, _animator, _target);
            }
        }

        private bool IsInDetectionRadius(Vector3 position)
        {
            float radius = _isAlert
                ? _detectionRadius + Mathf.Max(0f, _detectionExitPadding)
                : _detectionRadius;
            return GetFlatSqrDistance(position) <= radius * radius;
        }

        private float GetFlatSqrDistance(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private void SetAlert(bool isAlert)
        {
            if (_isAlert == isAlert)
            {
                return;
            }

            _isAlert = isAlert;
            _animator.SetBool(IsAlertHash, _isAlert);
        }

        private void RotateTowards(Vector3 targetPosition)
        {
            Vector3 lookDirection = targetPosition - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float targetYaw = Quaternion.LookRotation(lookDirection).eulerAngles.y;
            float smoothTime = Mathf.Max(0.01f, _rotationSmoothTime);
            float yaw = Mathf.SmoothDampAngle(_modelTransform.eulerAngles.y, targetYaw,
                ref _rotationVelocity, smoothTime, _rotationSpeed, Time.deltaTime);
            _modelTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void OnDisable()
        {
            _captureLogic?.StopGrab(this, _animator);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        }
    }
}
