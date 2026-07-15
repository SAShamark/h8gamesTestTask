using Game.Entities.Units.Character.Parts;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Entities
{
    public class CameraControl : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CinemachineFollow _cinemachineFollow;
        [SerializeField] private CinemachineBasicMultiChannelPerlin _cinemachineNoise;
        [SerializeField] private Transform _lookAheadTarget;

        [Header("Lead")]
        [SerializeField] private float _leadDistanceByDirection = 1.25f;
        [SerializeField] private float _leadDistanceByVelocityTime = 0.28f;
        [SerializeField] private float _maxLeadDistance = 2.2f;

        [Header("Smooth")]
        [SerializeField] private float _moveToLeadSmoothTime = 0.08f;
        [SerializeField] private float _returnToPlayerSmoothTime = 0.28f;
        [SerializeField] private float _velocityDirectionSmoothTime = 0.08f;

        [Header("Damage Shake")]
        [SerializeField] private float _damageShakeDuration = 0.16f;
        [SerializeField] private float _damageShakeStrength = 0.45f;
        [SerializeField] private float _damageShakeFrequency = 2.5f;

        private Transform _playerTarget;
        private MovementLogic _movementLogic;
        private Vector3 _lookAheadTargetMoveVelocity;
        private Vector3 _lookAheadTargetPosition;
        private Vector3 _smoothedPlayerVelocity;
        private Vector3 _velocitySmoothing;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeStrength;

        public void Init(Transform target, MovementLogic movementLogic)
        {
            _playerTarget = target;
            _movementLogic = movementLogic;
            _lookAheadTarget.position = target.position;
            _lookAheadTargetPosition = target.position;
            _cinemachineCamera.Follow = _lookAheadTarget;
            _cinemachineCamera.LookAt = _lookAheadTarget;
            _cinemachineNoise.AmplitudeGain = 0f;
            _cinemachineNoise.FrequencyGain = _damageShakeFrequency;
        }

        public void ShakeOnDamage()
        {
            Shake(_damageShakeStrength, _damageShakeDuration);
        }

        public void Shake(float strength, float duration)
        {
            _shakeStrength = strength;
            _shakeDuration = duration;
            _shakeTimer = duration;
            _cinemachineNoise.AmplitudeGain = strength;
        }

        private void LateUpdate()
        {
            MoveLookAheadTarget();
            UpdateShake();
        }

        private void MoveLookAheadTarget()
        {
            _smoothedPlayerVelocity = Vector3.SmoothDamp(_smoothedPlayerVelocity, _movementLogic.Velocity,
                ref _velocitySmoothing, _velocityDirectionSmoothTime);

            Vector3 targetPosition = _playerTarget.position + CalculateLeadOffset();
            float smoothTime = _movementLogic.NormalizedSpeed > 0.05f
                ? _moveToLeadSmoothTime
                : _returnToPlayerSmoothTime;

            _lookAheadTargetPosition = Vector3.SmoothDamp(_lookAheadTargetPosition, targetPosition,
                ref _lookAheadTargetMoveVelocity, smoothTime);
            _lookAheadTarget.position = _lookAheadTargetPosition;
        }

        private Vector3 CalculateLeadOffset()
        {
            Vector3 flatVelocity = Vector3.ProjectOnPlane(_smoothedPlayerVelocity, Vector3.up);
            float speedPercent = Mathf.Clamp01(_movementLogic.NormalizedSpeed);

            Vector3 velocityLead = Vector3.ClampMagnitude(
                flatVelocity * _leadDistanceByVelocityTime,
                _maxLeadDistance);

            Vector3 directionLead = flatVelocity.sqrMagnitude > 0.0001f
                ? flatVelocity.normalized * (_leadDistanceByDirection * speedPercent)
                : Vector3.zero;

            return velocityLead + directionLead;
        }

        private void UpdateShake()
        {
            if (_shakeTimer <= 0f)
            {
                _cinemachineNoise.AmplitudeGain = 0f;
                return;
            }

            _shakeTimer -= Time.deltaTime;
            float fade = Mathf.Clamp01(_shakeTimer / _shakeDuration);
            _cinemachineNoise.AmplitudeGain = _shakeStrength * fade * fade;
        }
    }
}
