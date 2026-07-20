using System;
using System.Collections;
using UnityEngine;

namespace Character.Ragdoll
{
    [Serializable]
    public class CharacterRagdollLogic
    {
        [Header("Throw")]
        [SerializeField] private float _minimumThrowTime = 0.65f;
        [SerializeField] private float _maximumThrowTime = 3f;
        [SerializeField] private float _settledVelocity = 0.75f;
        [SerializeField] private float _maximumThrowVelocity = 9f;
        [SerializeField] private float _groundCheckDistance = 0.75f;
        [SerializeField] private float _groundContactTolerance = 0.08f;
        [SerializeField] private LayerMask _groundLayers = ~0;

        [Header("Recovery")]
        [SerializeField] private float _groundedPauseBeforeRecovery = 0.18f;
        [SerializeField] private float _poseBlendDuration = 0.7f;
        [SerializeField] private float _animatedPoseSettleTime = 0.12f;

        [Header("Capture Stability")]
        [SerializeField] private float _captureDrag = 1.5f;
        [SerializeField] private float _captureAngularDrag = 4f;
        [SerializeField] private float _captureMaximumAngularVelocity = 4f;

        private CharacterRagdollRig _rig;
        private CharacterRagdollPhysics _physics;
        private CharacterRagdollRecovery _recovery;

        public Transform ModelTransform => _rig.ModelTransform;
        public Transform HipsTransform => _rig.HipsBody.transform;
        public Vector3 GroundContactPoint => _physics.GroundContactPoint;

        public void Initialize(Transform characterTransform, Animator animator)
        {
            _rig = new CharacterRagdollRig(characterTransform, animator);
            _physics = new CharacterRagdollPhysics(_rig, _minimumThrowTime,
                _maximumThrowTime, _settledVelocity, _maximumThrowVelocity,
                _groundCheckDistance, _groundContactTolerance, _groundLayers,
                _captureDrag, _captureAngularDrag, _captureMaximumAngularVelocity);
            _recovery = new CharacterRagdollRecovery(_rig, _physics,
                _groundedPauseBeforeRecovery, _poseBlendDuration,
                _animatedPoseSettleTime);
        }

        public void BeginCapture()
        {
            _physics.BeginCapture();
        }

        public void SetCapturedPose(Vector3 position, Quaternion rotation)
        {
            _physics.SetCapturedPose(position, rotation);
        }

        public void UpdateCapturedPose()
        {
            _physics.UpdateCapturedPose();
        }

        public void BeginThrow()
        {
            _physics.BeginThrow();
        }

        public void Throw(Vector3 velocity)
        {
            _physics.Throw(velocity);
        }

        public IEnumerator WaitForLanding()
        {
            return _physics.WaitForLanding();
        }

        public IEnumerator WaitForGroundContact()
        {
            return _physics.WaitForGroundContact();
        }

        public IEnumerator Recover()
        {
            return _recovery.Recover();
        }

        public void CancelRagdoll()
        {
            _physics.CancelCapture();
            _recovery.Cancel();
        }
    }
}
