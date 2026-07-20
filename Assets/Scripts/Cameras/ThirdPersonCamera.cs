using System;
using UnityEngine;

namespace Cameras
{
    [Serializable]
    public class ThirdPersonCamera
    {
        [SerializeField] private Vector3 _targetOffset = new(0f, 1.5f, 0f);
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _height = 0.35f;
        [SerializeField] private float _defaultPitch = 30f;
        [SerializeField] private float _pitchMin = -15f;
        [SerializeField] private float _pitchMax = 55f;

        private Transform _cameraTransform;
        private Transform _target;
        private CameraCollisionLogic _collisionLogic;

        public float DefaultPitch => _defaultPitch;

        public void Initialize(Transform cameraTransform, Transform target, CameraCollisionLogic collisionLogic)
        {
            _cameraTransform = cameraTransform;
            _target = target;
            _collisionLogic = collisionLogic;
        }

        public void UpdateCamera(float yaw, float pitch)
        {
            Vector3 focusPoint = _target.position + _targetOffset;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * _distance + Vector3.up * _height;

            _cameraTransform.SetPositionAndRotation(
                _collisionLogic.GetCameraPosition(focusPoint, desiredPosition), rotation);
        }

        public float ClampPitch(float pitch)
        {
            return Mathf.Clamp(pitch, _pitchMin, _pitchMax);
        }
    }
}