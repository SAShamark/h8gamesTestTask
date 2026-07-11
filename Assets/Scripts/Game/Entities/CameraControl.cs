using Unity.Cinemachine;
using UnityEngine;
using Game.Entities.Character;

namespace Game.Entities
{
    public class CameraControl : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CinemachineFollow _cinemachineFollow;
        [SerializeField] private Transform _lookAheadTarget;

        [Header("Lead")]
        [SerializeField] private float _leadDistanceByDirection = 1.25f;
        [SerializeField] private float _leadDistanceByVelocityTime = 0.28f;
        [SerializeField] private float _maxLeadDistance = 2.2f;

        [Header("Smooth")]
        [SerializeField] private float _moveToLeadSmoothTime = 0.08f;
        [SerializeField] private float _returnToPlayerSmoothTime = 0.28f;
        [SerializeField] private float _velocityDirectionSmoothTime = 0.08f;

        private Transform _playerTarget;
        private MovementControl _movementControl;
        private Vector3 _lookAheadTargetMoveVelocity;
        private Vector3 _smoothedPlayerVelocity;
        private Vector3 _velocitySmoothing;



        public void Init(Transform target, MovementControl movementControl)
        {
            _playerTarget = target;
            _movementControl = movementControl;
            _lookAheadTarget.position = target.position;
            _cinemachineCamera.Follow = _lookAheadTarget;
            _cinemachineCamera.LookAt = _lookAheadTarget;
        }

        private void LateUpdate()
        {
            MoveLookAheadTarget();
        }

        private void MoveLookAheadTarget()
        {
            _smoothedPlayerVelocity = Vector3.SmoothDamp(_smoothedPlayerVelocity, _movementControl.Velocity,
                ref _velocitySmoothing, _velocityDirectionSmoothTime);

            Vector3 targetPosition = _playerTarget.position + CalculateLeadOffset();
            float smoothTime = _movementControl.NormalizedSpeed > 0.05f
                ? _moveToLeadSmoothTime
                : _returnToPlayerSmoothTime;

            _lookAheadTarget.position = Vector3.SmoothDamp(_lookAheadTarget.position, targetPosition,
                ref _lookAheadTargetMoveVelocity, smoothTime);
        }

        private Vector3 CalculateLeadOffset()
        {
            Vector3 flatVelocity = Vector3.ProjectOnPlane(_smoothedPlayerVelocity, Vector3.up);
            float speedPercent = Mathf.Clamp01(_movementControl.NormalizedSpeed);

            Vector3 velocityLead = Vector3.ClampMagnitude(
                flatVelocity * _leadDistanceByVelocityTime,
                _maxLeadDistance);

            Vector3 directionLead = flatVelocity.sqrMagnitude > 0.0001f
                ? flatVelocity.normalized * (_leadDistanceByDirection * speedPercent)
                : Vector3.zero;

            return velocityLead + directionLead;
        }
    }
}
