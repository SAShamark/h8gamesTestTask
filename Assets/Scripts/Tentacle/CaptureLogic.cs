using System.Collections;
using UnityEngine;

namespace Tentacle
{
    [System.Serializable]
    public class CaptureLogic
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");

        [SerializeField] private Transform[] _bones;
        [SerializeField] private float _grabRadius = 6f;
        [SerializeField] private string _attackStateName = "AttackC_Wall01";
        [SerializeField] private string _idleStateName = "IdleA";
        [SerializeField] private float _attackCrossFadeDuration = 0.18f;
        [SerializeField] private float _attackWindupDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _attackTakeoverNormalizedTime = 0.32f;
        [SerializeField] private float _attackTakeoverTimeout = 0.8f;
        [SerializeField] private float _grabDuration = 0.75f;
        [SerializeField] private float _grabVerticalOffset;
        [SerializeField] private float _arcHeight = 1.5f;
        [SerializeField] private float _arcForwardOffset = 2f;
        [SerializeField] private float _arcSideOffset = 0.75f;
        [SerializeField, Range(0.5f, 0.9f)] private float _bodyBonePortion = 0.72f;
        [SerializeField] private float _wrapRadius = 0.55f;
        [SerializeField] private float _wrapTurns = 1.15f;
        [SerializeField, Range(0.25f, 0.9f)] private float _reachPhasePortion = 0.65f;
        [SerializeField] private float _recoverToIdleDuration = 0.35f;
        [SerializeField] private TentacleLiftLogic _liftLogic = new();

        private Vector3[] _startBonePositions;
        private Quaternion[] _startBoneRotations;
        private Quaternion[] _boneRotationOffsets;
        private Vector3[] _targetBonePositions;
        private Vector3[] _idleLocalPositions;
        private Quaternion[] _idleLocalRotations;
        private Coroutine _grabRoutine;
        private CharacterControl _capturedCharacter;
        private bool _isGrabbing;
        private bool _isAttacking;
        private bool _hasCommittedCapture;

        public bool IsBusy => _isAttacking || _isGrabbing;
        public bool CanCancelForDistance => IsBusy && !_hasCommittedCapture;

        public bool IsInGrabRadius(Vector3 origin, Vector3 targetPosition)
        {
            Vector3 offset = targetPosition - origin;
            offset.y = 0f;
            return offset.sqrMagnitude <= _grabRadius * _grabRadius;
        }

        public void TryStartGrab(MonoBehaviour owner, Animator animator, Transform target)
        {
            if (IsBusy || owner == null || animator == null || target == null || !HasValidBones())
            {
                return;
            }

            CaptureIdleLocalPose();
            _liftLogic ??= new TentacleLiftLogic();
            _grabRoutine = owner.StartCoroutine(AttackThenGrab(animator, target));
        }

        public void StopGrab(MonoBehaviour owner, Animator animator)
        {
            if (_grabRoutine != null && owner != null)
            {
                owner.StopCoroutine(_grabRoutine);
            }

            _grabRoutine = null;
            _isAttacking = false;
            _isGrabbing = false;
            _hasCommittedCapture = false;

            if (_capturedCharacter != null)
            {
                _capturedCharacter.CancelCapture();
                _capturedCharacter = null;
            }

            _liftLogic?.Clear();

            _startBonePositions = null;
            _startBoneRotations = null;
            _boneRotationOffsets = null;
            _targetBonePositions = null;

            RestoreIdleLocalPose();

            if (animator != null)
            {
                ReturnToIdleAnimation(animator);
            }
        }

        private IEnumerator AttackThenGrab(Animator animator, Transform target)
        {
            _isAttacking = true;
            animator.enabled = true;
            PlayAttackAnimation(animator);
            yield return WaitForAttackTakeover(animator);

            _isAttacking = false;
            yield return GrabWithCurve(animator, target);
        }

        private IEnumerator GrabWithCurve(Animator animator, Transform target)
        {
            _isGrabbing = true;
            animator.Update(0f);
            animator.enabled = false;
            CaptureCurrentBonePose();
            CaptureBoneRotationOffsets();
            _targetBonePositions = new Vector3[_bones.Length];

            float timer = 0f;

            while (timer < _grabDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / _grabDuration);
                BendTentacleToTarget(target, progress);

                yield return null;
            }

            BendTentacleToTarget(target, 1f);
            if (!CaptureCharacter(target))
            {
                _isGrabbing = false;
                ReturnToIdleAnimation(animator);
                _grabRoutine = null;
                yield break;
            }

            _hasCommittedCapture = true;

            yield return _liftLogic.LiftCapturedCharacter(_bones, _capturedCharacter, target,
                animator.transform);

            _capturedCharacter = null;
            yield return RecoverBonesToIdlePose();
            _isGrabbing = false;
            _hasCommittedCapture = false;
            ReturnToIdleAnimation(animator);
            _grabRoutine = null;
        }

        private void BendTentacleToTarget(Transform target, float progress)
        {
            Vector3 root = _startBonePositions[0];
            Vector3 targetPosition = GetGrabCenter(target);

            Vector3 direction = targetPosition - root;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = target.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            Vector3 side = Vector3.Cross(Vector3.up, direction);
            if (side.sqrMagnitude <= 0.001f)
            {
                side = Vector3.right;
            }

            side.Normalize();

            float reachProgress = GetSmootherStep(Mathf.Clamp01(progress / _reachPhasePortion));
            float wrapProgress = GetSmootherStep(Mathf.InverseLerp(_reachPhasePortion, 1f,
                progress));

            Vector3 wrapEntryPoint = targetPosition - direction * _wrapRadius;
            Vector3 currentTarget = Vector3.Lerp(root, wrapEntryPoint, reachProgress);
            Vector3 p0 = root;
            Vector3 p1 = root + direction * _arcForwardOffset + side * _arcSideOffset +
                         Vector3.up * _arcHeight;
            Vector3 p2 = currentTarget - direction * 1.5f - side * _arcSideOffset +
                         Vector3.up * _arcHeight;
            Vector3 p3 = currentTarget;

            for (int i = 0; i < _bones.Length; i++)
            {
                float t = i / (float)(_bones.Length - 1);

                if (t <= _bodyBonePortion)
                {
                    float wrappedBodyT = Mathf.InverseLerp(0f, _bodyBonePortion, t);
                    float bodyT = Mathf.Lerp(t, wrappedBodyT, wrapProgress);

                    _targetBonePositions[i] = EvaluateBezier(p0, p1, p2, p3, bodyT);
                    continue;
                }

                float wrapT = Mathf.InverseLerp(_bodyBonePortion, 1f, t);
                Vector3 straightPosition = EvaluateBezier(p0, p1, p2, p3, t);
                float boneWrapProgress = GetSmootherStep(Mathf.Clamp01(wrapProgress * 1.25f -
                                                                        wrapT * 0.25f));
                Vector3 wrappedPosition = EvaluateWrapPoint(targetPosition, direction, side, wrapT,
                    boneWrapProgress);
                _targetBonePositions[i] = Vector3.Lerp(straightPosition, wrappedPosition,
                    boneWrapProgress);
            }

            Vector3 stableUp = GetSafeStartUp(GetTargetBoneTangent(0), direction);

            for (int i = 0; i < _bones.Length; i++)
            {
                Vector3 tangent = GetTargetBoneTangent(i);

                if (tangent.sqrMagnitude <= 0.001f)
                {
                    tangent = direction;
                }

                Vector3 projectedUp = Vector3.ProjectOnPlane(stableUp, tangent);
                if (projectedUp.sqrMagnitude <= 0.001f)
                {
                    projectedUp = Vector3.ProjectOnPlane(Vector3.up, tangent);
                }

                if (projectedUp.sqrMagnitude <= 0.001f)
                {
                    projectedUp = Vector3.Cross(tangent, Vector3.right);
                }

                stableUp = projectedUp.normalized;

                Quaternion rotation = Quaternion.LookRotation(tangent.normalized, stableUp) *
                                      _boneRotationOffsets[i];

                _bones[i].position = Vector3.Lerp(_startBonePositions[i], _targetBonePositions[i],
                    reachProgress);
                _bones[i].rotation = Quaternion.Slerp(_startBoneRotations[i], rotation,
                    reachProgress);
            }
        }

        private void PlayAttackAnimation(Animator animator)
        {
            animator.ResetTrigger(AttackHash);

            if (!string.IsNullOrEmpty(_attackStateName))
            {
                animator.CrossFadeInFixedTime(_attackStateName, _attackCrossFadeDuration);
                return;
            }

            animator.SetTrigger(AttackHash);
        }

        private IEnumerator WaitForAttackTakeover(Animator animator)
        {
            float timer = 0f;
            float minimumDuration = Mathf.Max(0f, _attackWindupDuration);
            float timeout = Mathf.Max(minimumDuration, _attackTakeoverTimeout);

            while (timer < timeout)
            {
                timer += Time.deltaTime;
                AnimatorStateInfo stateInfo = animator.IsInTransition(0)
                    ? animator.GetNextAnimatorStateInfo(0)
                    : animator.GetCurrentAnimatorStateInfo(0);

                bool reachedAttackPose = stateInfo.IsName(_attackStateName) &&
                                         stateInfo.normalizedTime >= _attackTakeoverNormalizedTime;
                if (timer >= minimumDuration && reachedAttackPose)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private void ReturnToIdleAnimation(Animator animator)
        {
            animator.enabled = true;
            animator.ResetTrigger(AttackHash);
            animator.SetBool(IsAlertHash, false);

            if (!string.IsNullOrEmpty(_idleStateName))
            {
                animator.CrossFadeInFixedTime(_idleStateName, _recoverToIdleDuration, 0, 0f);
            }
        }

        private bool CaptureCharacter(Transform target)
        {
            CharacterControl character = target.GetComponentInParent<CharacterControl>();

            if (character == null || !character.TryBeginCapture())
            {
                return false;
            }

            _capturedCharacter = character;
            Vector3 capturePosition = target.position;
            Quaternion captureRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            character.SetCapturedPose(capturePosition, captureRotation);
            return true;
        }

        private void CaptureCurrentBonePose()
        {
            _startBonePositions = new Vector3[_bones.Length];
            _startBoneRotations = new Quaternion[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                _startBonePositions[i] = _bones[i].position;
                _startBoneRotations[i] = _bones[i].rotation;
                _idleLocalPositions[i] = _bones[i].localPosition;
                _idleLocalRotations[i] = _bones[i].localRotation;
            }
        }

        private void CaptureIdleLocalPose()
        {
            _idleLocalPositions = new Vector3[_bones.Length];
            _idleLocalRotations = new Quaternion[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                _idleLocalPositions[i] = _bones[i].localPosition;
                _idleLocalRotations[i] = _bones[i].localRotation;
            }
        }

        private IEnumerator RecoverBonesToIdlePose()
        {
            if (_idleLocalPositions == null || _idleLocalRotations == null ||
                _idleLocalPositions.Length != _bones.Length || _recoverToIdleDuration <= 0f)
            {
                RestoreIdleLocalPose();
                yield break;
            }

            Vector3[] recoverStartPositions = new Vector3[_bones.Length];
            Quaternion[] recoverStartRotations = new Quaternion[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                recoverStartPositions[i] = _bones[i].localPosition;
                recoverStartRotations[i] = _bones[i].localRotation;
            }

            float timer = 0f;

            while (timer < _recoverToIdleDuration)
            {
                timer += Time.deltaTime;
                float progress = GetSmootherStep(Mathf.Clamp01(timer / _recoverToIdleDuration));

                for (int i = 0; i < _bones.Length; i++)
                {
                    _bones[i].localPosition = Vector3.Lerp(recoverStartPositions[i],
                        _idleLocalPositions[i], progress);
                    _bones[i].localRotation = Quaternion.Slerp(recoverStartRotations[i],
                        _idleLocalRotations[i], progress);
                }

                yield return null;
            }

            RestoreIdleLocalPose();
        }

        private void RestoreIdleLocalPose()
        {
            if (_idleLocalPositions == null || _idleLocalRotations == null ||
                _idleLocalPositions.Length != _bones.Length)
            {
                return;
            }

            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = _idleLocalPositions[i];
                _bones[i].localRotation = _idleLocalRotations[i];
            }
        }

        private void CaptureBoneRotationOffsets()
        {
            _boneRotationOffsets = new Quaternion[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                Vector3 tangent = GetStartBoneTangent(i);

                if (tangent.sqrMagnitude <= 0.001f)
                {
                    _boneRotationOffsets[i] = Quaternion.identity;
                    continue;
                }

                Vector3 up = Vector3.ProjectOnPlane(_startBoneRotations[i] * Vector3.up, tangent);

                if (up.sqrMagnitude <= 0.001f)
                {
                    up = Vector3.ProjectOnPlane(Vector3.up, tangent);
                }

                if (up.sqrMagnitude <= 0.001f)
                {
                    up = Vector3.right;
                }

                Quaternion referenceRotation = Quaternion.LookRotation(tangent.normalized,
                    up.normalized);
                _boneRotationOffsets[i] = Quaternion.Inverse(referenceRotation) *
                                          _startBoneRotations[i];
            }
        }

        private Vector3 GetStartBoneTangent(int index)
        {
            if (index < _startBonePositions.Length - 1)
            {
                return _startBonePositions[index + 1] - _startBonePositions[index];
            }

            return _startBonePositions[index] - _startBonePositions[index - 1];
        }

        private Vector3 GetTargetBoneTangent(int index)
        {
            if (index < _targetBonePositions.Length - 1)
            {
                return _targetBonePositions[index + 1] - _targetBonePositions[index];
            }

            return _targetBonePositions[index] - _targetBonePositions[index - 1];
        }

        private Vector3 GetSafeStartUp(Vector3 tangent, Vector3 fallbackDirection)
        {
            Vector3 up = Vector3.ProjectOnPlane(_startBoneRotations[0] * Vector3.up, tangent);

            if (up.sqrMagnitude <= 0.001f)
            {
                up = Vector3.ProjectOnPlane(Vector3.up, tangent);
            }

            if (up.sqrMagnitude <= 0.001f)
            {
                up = Vector3.Cross(fallbackDirection, Vector3.right);
            }

            return up.sqrMagnitude > 0.001f ? up.normalized : Vector3.up;
        }

        private bool HasValidBones()
        {
            if (_bones == null || _bones.Length < 2)
            {
                return false;
            }

            for (int i = 0; i < _bones.Length; i++)
            {
                if (_bones[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
            float t)
        {
            float u = 1f - t;

            return u * u * u * p0 +
                   3f * u * u * t * p1 +
                   3f * u * t * t * p2 +
                   t * t * t * p3;
        }

        private static float GetSmootherStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }

        private Vector3 EvaluateWrapPoint(Vector3 center, Vector3 direction, Vector3 side,
            float wrapT, float wrapProgress)
        {
            float angle = (180f - 360f * _wrapTurns * wrapProgress * wrapT) * Mathf.Deg2Rad;

            return center +
                   direction * Mathf.Cos(angle) * _wrapRadius +
                   side * Mathf.Sin(angle) * _wrapRadius;
        }

        private Vector3 GetGrabCenter(Transform target)
        {
            Collider targetCollider = target.GetComponentInParent<Collider>();

            if (targetCollider != null)
            {
                return targetCollider.bounds.center + Vector3.up * _grabVerticalOffset;
            }

            return target.position + Vector3.up * _grabVerticalOffset;
        }

    }
}
