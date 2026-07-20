using System;
using UnityEngine;

namespace Cameras
{
    [Serializable]
    public class FirstPersonCamera
    {
        [SerializeField] private Transform _anchor;
        [SerializeField] private Vector3 _fallbackOffset = new(-0.2f, 2.7f, 0f);
        [SerializeField] private float _pitchMin = -50f;
        [SerializeField] private float _pitchMax = 50f;
        [SerializeField] private bool _rotateTarget = true;

        private Transform _cameraTransform;
        private Transform _target;

        public void Initialize(Transform cameraTransform, Transform target)
        {
            _cameraTransform = cameraTransform;
            _target = target;
            ResolveAnchor();
        }

        public void UpdateCamera(float yaw, float pitch)
        {
            if (_rotateTarget)
            {
                _target.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            Vector3 position = _anchor != null
                ? _anchor.position
                : _target.position + _fallbackOffset;
            _cameraTransform.SetPositionAndRotation(position,
                Quaternion.Euler(pitch, yaw, 0f));
        }

        public float ClampPitch(float pitch)
        {
            return Mathf.Clamp(pitch, _pitchMin, _pitchMax);
        }

        private void ResolveAnchor()
        {
            if (_anchor != null)
            {
                return;
            }

            Animator animator = _target.GetComponentInChildren<Animator>();
            _anchor = animator.GetBoneTransform(HumanBodyBones.Head);
        }
    }
}
