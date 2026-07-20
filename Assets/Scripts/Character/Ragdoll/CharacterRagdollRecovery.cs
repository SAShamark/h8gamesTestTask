using System.Collections;
using UnityEngine;

namespace Character.Ragdoll
{
    internal sealed class CharacterRagdollRecovery
    {
        private readonly CharacterRagdollRig _rig;
        private readonly CharacterRagdollPhysics _physics;
        private readonly float _groundedPauseBeforeRecovery;
        private readonly float _poseBlendDuration;
        private readonly float _animatedPoseSettleTime;
        private readonly Vector3[] _ragdollLocalPositions;
        private readonly Quaternion[] _ragdollLocalRotations;
        private readonly Vector3[] _animatedLocalPositions;
        private readonly Quaternion[] _animatedLocalRotations;

        public CharacterRagdollRecovery(CharacterRagdollRig rig,
            CharacterRagdollPhysics physics,
            float groundedPauseBeforeRecovery, float poseBlendDuration,
            float animatedPoseSettleTime)
        {
            _rig = rig;
            _physics = physics;
            _groundedPauseBeforeRecovery = groundedPauseBeforeRecovery;
            _poseBlendDuration = poseBlendDuration;
            _animatedPoseSettleTime = animatedPoseSettleTime;
            _ragdollLocalPositions = new Vector3[rig.Bones.Length];
            _ragdollLocalRotations = new Quaternion[rig.Bones.Length];
            _animatedLocalPositions = new Vector3[rig.Bones.Length];
            _animatedLocalRotations = new Quaternion[rig.Bones.Length];
        }

        public IEnumerator Recover()
        {
            yield return WaitBeforeRecovery();

            _rig.Freeze();
            CaptureWorldPose(_ragdollLocalPositions, _ragdollLocalRotations);
            _rig.AlignCharacterToRagdoll(GetHighestGroundPoint());
            _rig.SetCollidersEnabled(false);
            _rig.RestoreModelParent();
            RestoreWorldPose(_ragdollLocalPositions, _ragdollLocalRotations);
            CaptureLocalPose(_ragdollLocalPositions, _ragdollLocalRotations);
            CaptureAnimatedPose();
            RestoreLocalPose(_ragdollLocalPositions, _ragdollLocalRotations);

            float timer = 0f;

            while (timer < _poseBlendDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(timer / _poseBlendDuration));
                BlendToAnimatedPose(progress);
                yield return null;
            }

            RestoreLocalPose(_animatedLocalPositions, _animatedLocalRotations);
            FinishRecovery();
        }

        public void Cancel()
        {
            if (!_rig.IsActive)
            {
                return;
            }

            _rig.Freeze();
            _rig.AlignCharacterToRagdoll(GetHighestGroundPoint());
            _rig.SetCollidersEnabled(false);
            _rig.RestoreModelParent();
            FinishRecovery();
        }

        private IEnumerator WaitBeforeRecovery()
        {
            float timer = 0f;

            while (timer < _groundedPauseBeforeRecovery)
            {
                timer += Time.deltaTime;
                _rig.FollowHips();
                yield return null;
            }
        }

        private Vector3? GetHighestGroundPoint()
        {
            return _physics.TryGetHighestGroundPointUnderRagdoll(
                out Vector3 groundPoint) ? groundPoint : null;
        }

        private void FinishRecovery()
        {
            _rig.Disable();
            _rig.Animator.enabled = true;
            _rig.RestoreRendererSettings();
        }

        private void CaptureAnimatedPose()
        {
            _rig.Animator.enabled = true;
            _rig.Animator.Update(Mathf.Max(0f, _animatedPoseSettleTime));
            CaptureLocalPose(_animatedLocalPositions, _animatedLocalRotations);
            _rig.Animator.enabled = false;
        }

        private void CaptureLocalPose(Vector3[] positions, Quaternion[] rotations)
        {
            for (int i = 0; i < _rig.Bones.Length; i++)
            {
                positions[i] = _rig.Bones[i].localPosition;
                rotations[i] = _rig.Bones[i].localRotation;
            }
        }

        private void CaptureWorldPose(Vector3[] positions, Quaternion[] rotations)
        {
            for (int i = 0; i < _rig.Bones.Length; i++)
            {
                positions[i] = _rig.Bones[i].position;
                rotations[i] = _rig.Bones[i].rotation;
            }
        }

        private void RestoreWorldPose(Vector3[] positions, Quaternion[] rotations)
        {
            for (int i = 0; i < _rig.Bones.Length; i++)
            {
                _rig.Bones[i].SetPositionAndRotation(positions[i], rotations[i]);
            }
        }

        private void RestoreLocalPose(Vector3[] positions, Quaternion[] rotations)
        {
            for (int i = 0; i < _rig.Bones.Length; i++)
            {
                _rig.Bones[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
            }
        }

        private void BlendToAnimatedPose(float progress)
        {
            for (int i = 0; i < _rig.Bones.Length; i++)
            {
                _rig.Bones[i].localPosition = Vector3.Lerp(
                    _ragdollLocalPositions[i], _animatedLocalPositions[i], progress);
                _rig.Bones[i].localRotation = Quaternion.Slerp(
                    _ragdollLocalRotations[i], _animatedLocalRotations[i], progress);
            }
        }
    }
}
