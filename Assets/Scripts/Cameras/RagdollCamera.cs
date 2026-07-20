using System;
using UnityEngine;

namespace Cameras
{
    [Serializable]
    public class RagdollCamera
    {
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _height = 2.2f;
        [SerializeField] private float _defaultPitch = 28f;
        [SerializeField] private float _pitchMin = 5f;
        [SerializeField] private float _pitchMax = 65f;
        [SerializeField] private float _focusHeight = 0.55f;
        [SerializeField] private float _positionSmoothTime = 0.12f;
        [SerializeField] private float _rotationSharpness = 14f;

        private Transform _cameraTransform;
        private Transform _target;
        private Transform _focus;
        private CameraCollisionLogic _collisionLogic;
        private Vector3 _positionVelocity;

        public float DefaultPitch => _defaultPitch;

        public void Initialize(
            Transform cameraTransform,
            Transform target,
            CameraCollisionLogic collisionLogic)
        {
            _cameraTransform = cameraTransform;
            _target = target;
            _collisionLogic = collisionLogic;
        }

        public void SetFocus(Transform focus)
        {
            _focus = focus;
            _positionVelocity = Vector3.zero;
        }

        public void ClearFocus()
        {
            _focus = null;
            _positionVelocity = Vector3.zero;
        }

        public void UpdateCamera(float yaw, float pitch)
        {
            Vector3 focusPoint = GetFocusPoint();
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focusPoint - orbitRotation * Vector3.forward * _distance +
                                      Vector3.up * _height;
            Vector3 targetPosition = _collisionLogic.GetCameraPosition(
                focusPoint, desiredPosition);

            _cameraTransform.position = Vector3.SmoothDamp(_cameraTransform.position,
                targetPosition, ref _positionVelocity, _positionSmoothTime);
            Quaternion lookRotation = Quaternion.LookRotation(
                focusPoint - _cameraTransform.position, Vector3.up);
            _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation,
                lookRotation, 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime));
        }

        public float ClampPitch(float pitch)
        {
            return Mathf.Clamp(pitch, _pitchMin, _pitchMax);
        }

        public float GetThrowYaw(Vector3 throwVelocity)
        {
            throwVelocity.y = 0f;

            if (throwVelocity.sqrMagnitude <= 0.001f)
            {
                return _target.eulerAngles.y;
            }

            return Quaternion.LookRotation(
                throwVelocity.normalized, Vector3.up).eulerAngles.y;
        }

        public float GetCurrentCameraYaw(Vector3 focusPosition)
        {
            Vector3 cameraDirection = _cameraTransform.position - focusPosition;
            cameraDirection.y = 0f;

            if (cameraDirection.sqrMagnitude <= 0.001f)
            {
                return _target.eulerAngles.y;
            }

            return Quaternion.LookRotation(
                -cameraDirection.normalized, Vector3.up).eulerAngles.y;
        }

        private Vector3 GetFocusPoint()
        {
            return _focus.position + Vector3.up * _focusHeight;
        }
    }
}
