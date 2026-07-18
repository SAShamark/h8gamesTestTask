using System.Collections;
using UnityEngine;

namespace Tentacle
{
    [System.Serializable]
    public class TentacleLiftLogic
    {
        [SerializeField] private float _liftDuration = 0.75f;
        [SerializeField] private float _liftHeight = 4.25f;
        [SerializeField] private float _liftForwardOffset = 0.35f;
        [SerializeField] private float _liftArcHeight = 1.25f;
        [SerializeField, Range(0.35f, 1f)] private float _liftWrapTransitionPortion = 0.65f;
        [SerializeField] private float _tipRadius = 0.48f;
        [SerializeField] private float _tipSurfaceOffset = 0.42f;
        [SerializeField] private float _tipVerticalInset = 0.16f;
        [SerializeField] private float _tipWrapTurns = 1.65f;
        [SerializeField, Range(0.35f, 0.9f)] private float _bodyBonePortion = 0.46f;
        [SerializeField] private float _topShakeDuration = 3f;
        [SerializeField] private float _topShakeFrequency = 1.35f;
        [SerializeField] private float _topShakeSideAmplitude = 0.65f;
        [SerializeField] private float _topShakeForwardAmplitude = 0.25f;
        [SerializeField] private float _topShakeHeightAmplitude = 0.12f;
        [SerializeField] private float _topShakeRotationAngle = 12f;
        [SerializeField] private float _throwWindupDuration = 0.55f;
        [SerializeField] private float _throwReleaseDuration = 0.35f;
        [SerializeField] private float _throwDrawBackDistance = 1.15f;
        [SerializeField] private float _throwExtensionDistance = 0.9f;
        [SerializeField, Range(0f, 0.8f)] private float _throwUnwrapStart = 0.12f;
        [SerializeField] private float _throwUnwrapExpansion = 0.65f;
        [SerializeField] private float _throwUnwrapTrailDistance = 1.35f;
        [SerializeField] private float _throwFollowThroughDuration = 0.22f;
        [SerializeField] private float _throwFollowThroughDistance = 0.75f;
        [SerializeField] private float _throwForwardSpeed = 8.5f;
        [SerializeField] private float _throwUpSpeed = 2.5f;

        private Vector3[] _startBonePositions;
        private Quaternion[] _startBoneRotations;
        private Quaternion[] _boneRotationOffsets;
        private Vector3[] _targetBonePositions;
        private float _currentTipRadius;
        private float _currentTipBottomOffset;
        private float _currentTipTopOffset;
        private Vector3 _currentTargetCenterOffset;

        public IEnumerator LiftCapturedCharacter(
            Transform[] bones,
            ICapturableCharacter character,
            Transform target,
            Transform liftOrigin)
        {
            if (!HasValidInput(bones, character, target, liftOrigin))
            {
                yield break;
            }

            CaptureCurrentBonePose(bones);
            CaptureBoneRotationOffsets(bones);
            _targetBonePositions = new Vector3[bones.Length];

            Vector3 rootPosition = _startBonePositions[0];
            Vector3 startCharacterPosition = target.position;
            Quaternion startCharacterRotation = target.rotation;
            Vector3 liftDirection = GetFlatDirection(rootPosition, startCharacterPosition, liftOrigin);
            CaptureTargetWrapShape(target, liftDirection);
            Vector3 liftedPosition = rootPosition + Vector3.up * _liftHeight +
                                     liftDirection * _liftForwardOffset;
            Quaternion liftedRotation = Quaternion.Euler(0f, liftOrigin.eulerAngles.y, 0f);

            float timer = 0f;

            while (timer < _liftDuration)
            {
                timer += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(timer / _liftDuration);
                float movementProgress = GetSmootherStep(normalizedTime);
                float shapeProgress = GetSmootherStep(Mathf.Clamp01(normalizedTime /
                    Mathf.Max(0.01f, _liftWrapTransitionPortion)));

                Vector3 characterPosition = Vector3.Lerp(startCharacterPosition, liftedPosition,
                    movementProgress);
                Quaternion characterRotation = Quaternion.Slerp(startCharacterRotation,
                    liftedRotation, movementProgress);

                character.SetCapturedPose(characterPosition, characterRotation);
                BendToLiftedCharacter(bones, rootPosition, characterPosition, liftDirection,
                    shapeProgress, 1f);

                yield return null;
            }

            character.SetCapturedPose(liftedPosition, liftedRotation);
            BendToLiftedCharacter(bones, rootPosition, liftedPosition, liftDirection, 1f, 1f);

            yield return ShakeCapturedCharacterAtTop(bones, character, rootPosition, liftedPosition,
                liftedRotation, liftDirection);

            character.SetCapturedPose(liftedPosition, liftedRotation);
            BendToLiftedCharacter(bones, rootPosition, liftedPosition, liftDirection, 1f, 1f);

            yield return UnwindAndThrowCharacter(bones, character, rootPosition, liftedPosition,
                liftedRotation, liftDirection);
        }

        public void Clear()
        {
            _startBonePositions = null;
            _startBoneRotations = null;
            _boneRotationOffsets = null;
            _targetBonePositions = null;
            _currentTipRadius = _tipRadius;
            _currentTipBottomOffset = -_tipRadius;
            _currentTipTopOffset = _tipRadius;
            _currentTargetCenterOffset = Vector3.zero;
        }

        private void BendToLiftedCharacter(
            Transform[] bones,
            Vector3 rootPosition,
            Vector3 liftedPosition,
            Vector3 liftDirection,
            float progress,
            float wrapBlend)
        {
            Vector3 side = Vector3.Cross(Vector3.up, liftDirection);
            if (side.sqrMagnitude <= 0.001f)
            {
                side = Vector3.right;
            }

            side.Normalize();

            Vector3 lowerHandle = rootPosition + liftDirection * 0.75f + Vector3.up * _liftArcHeight;
            Vector3 upperHandle = liftedPosition - Vector3.up * _liftArcHeight -
                                  liftDirection * (_currentTipRadius + _liftForwardOffset);
            Vector3 tipCenter = liftedPosition + _currentTargetCenterOffset;
            wrapBlend = Mathf.Clamp01(wrapBlend);
            float effectiveBodyBonePortion = Mathf.Min(_bodyBonePortion, 0.46f);
            float effectiveWrapTurns = Mathf.Max(_tipWrapTurns, 1.65f);

            for (int i = 0; i < bones.Length; i++)
            {
                float t = i / (float)(bones.Length - 1);

                if (t <= effectiveBodyBonePortion)
                {
                    float bodyT = Mathf.InverseLerp(0f, effectiveBodyBonePortion, t);
                    _targetBonePositions[i] = EvaluateBezier(rootPosition, lowerHandle, upperHandle,
                        GetWrapCenter(tipCenter, 0f), bodyT);
                    continue;
                }

                float wrapT = Mathf.InverseLerp(effectiveBodyBonePortion, 1f, t);
                Vector3 wrappedPosition = EvaluateTipWrap(tipCenter, liftDirection, side, wrapT,
                    effectiveWrapTurns);
                float unwrapProgress = 1f - wrapBlend;
                float stagger = (1f - wrapT) * 0.22f;
                float localUnwrap = GetSmootherStep(Mathf.InverseLerp(stagger, 1f, unwrapProgress));
                Vector3 wrapCenter = GetWrapCenter(tipCenter, wrapT);
                Vector3 radialOffset = wrappedPosition - wrapCenter;
                float expansion = 1f + Mathf.Sin(localUnwrap * Mathf.PI) *
                                  Mathf.Max(0f, _throwUnwrapExpansion);
                Vector3 expandedPosition = wrapCenter + radialOffset * expansion;
                Vector3 openPosition = wrapCenter - liftDirection *
                    (_currentTipRadius + Mathf.Max(0f, _throwUnwrapTrailDistance) * wrapT) +
                    side * (Mathf.Sin(wrapT * Mathf.PI) * _currentTipRadius * 0.25f);
                float peelProgress = GetSmootherStep(Mathf.InverseLerp(0.32f, 1f, localUnwrap));
                _targetBonePositions[i] = Vector3.Lerp(expandedPosition, openPosition, peelProgress);
            }

            Vector3 stableUp = GetSafeStartUp(GetTargetBoneTangent(0), liftDirection);

            for (int i = 0; i < bones.Length; i++)
            {
                Vector3 tangent = GetTargetBoneTangent(i);

                if (tangent.sqrMagnitude <= 0.001f)
                {
                    tangent = Vector3.up;
                }

                Vector3 projectedUp = Vector3.ProjectOnPlane(stableUp, tangent);
                if (projectedUp.sqrMagnitude <= 0.001f)
                {
                    projectedUp = Vector3.ProjectOnPlane(liftDirection, tangent);
                }

                if (projectedUp.sqrMagnitude <= 0.001f)
                {
                    projectedUp = Vector3.right;
                }

                stableUp = projectedUp.normalized;

                Quaternion targetRotation = Quaternion.LookRotation(tangent.normalized, stableUp) *
                                            _boneRotationOffsets[i];

                bones[i].position = Vector3.Lerp(_startBonePositions[i], _targetBonePositions[i], progress);
                bones[i].rotation = Quaternion.Slerp(_startBoneRotations[i], targetRotation, progress);
            }
        }

        private void CaptureCurrentBonePose(Transform[] bones)
        {
            _startBonePositions = new Vector3[bones.Length];
            _startBoneRotations = new Quaternion[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                _startBonePositions[i] = bones[i].position;
                _startBoneRotations[i] = bones[i].rotation;
            }
        }

        private void CaptureBoneRotationOffsets(Transform[] bones)
        {
            _boneRotationOffsets = new Quaternion[bones.Length];

            for (int i = 0; i < bones.Length; i++)
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

                Quaternion referenceRotation = Quaternion.LookRotation(tangent.normalized, up.normalized);
                _boneRotationOffsets[i] = Quaternion.Inverse(referenceRotation) * _startBoneRotations[i];
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
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, tangent);

            if (up.sqrMagnitude <= 0.001f)
            {
                up = Vector3.ProjectOnPlane(fallbackDirection, tangent);
            }

            return up.sqrMagnitude > 0.001f ? up.normalized : Vector3.up;
        }

        private Vector3 EvaluateTipWrap(
            Vector3 center,
            Vector3 direction,
            Vector3 side,
            float t,
            float turns)
        {
            float angle = (180f - 360f * turns * t) * Mathf.Deg2Rad;

            return GetWrapCenter(center, t) +
                   direction * Mathf.Cos(angle) * _currentTipRadius +
                   side * Mathf.Sin(angle) * _currentTipRadius;
        }

        private Vector3 GetWrapCenter(Vector3 center, float t)
        {
            return center + Vector3.up * Mathf.Lerp(_currentTipBottomOffset, _currentTipTopOffset, t);
        }

        private void CaptureTargetWrapShape(Transform target, Vector3 direction)
        {
            Collider targetCollider = target.GetComponentInParent<Collider>();

            if (targetCollider == null)
            {
                _currentTipRadius = _tipRadius;
                _currentTipBottomOffset = -_tipRadius;
                _currentTipTopOffset = _tipRadius;
                _currentTargetCenterOffset = Vector3.zero;
                return;
            }

            Vector3 side = Vector3.Cross(Vector3.up, direction);
            if (side.sqrMagnitude <= 0.001f)
            {
                side = Vector3.right;
            }

            side.Normalize();

            Bounds bounds = targetCollider.bounds;
            _currentTargetCenterOffset = bounds.center - target.position;
            float directionRadius = GetBoundsRadiusOnAxis(bounds, direction);
            float sideRadius = GetBoundsRadiusOnAxis(bounds, side);
            _currentTipRadius = Mathf.Max(_tipRadius, directionRadius, sideRadius) +
                                Mathf.Max(_tipSurfaceOffset, 0.42f);

            float halfHeight = Mathf.Max(0.5f, bounds.extents.y - _tipVerticalInset);
            halfHeight = Mathf.Max(halfHeight, _currentTipRadius * 0.7f);
            _currentTipBottomOffset = -halfHeight;
            _currentTipTopOffset = halfHeight;
        }

        private IEnumerator ShakeCapturedCharacterAtTop(
            Transform[] bones,
            ICapturableCharacter character,
            Vector3 rootPosition,
            Vector3 liftedPosition,
            Quaternion liftedRotation,
            Vector3 liftDirection)
        {
            if (_topShakeDuration <= 0f)
            {
                yield break;
            }

            Vector3 side = Vector3.Cross(Vector3.up, liftDirection);
            if (side.sqrMagnitude <= 0.001f)
            {
                side = Vector3.right;
            }

            side.Normalize();

            float timer = 0f;

            while (timer < _topShakeDuration)
            {
                timer += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(timer / _topShakeDuration);
                float engage = GetSmootherStep(Mathf.InverseLerp(0f, 0.16f, normalizedTime));
                float settle = 1f - GetSmootherStep(Mathf.InverseLerp(0.72f, 1f,
                    normalizedTime));
                float shakeWeight = engage * settle;
                float phase = timer * _topShakeFrequency * Mathf.PI * 2f;

                Vector3 shakeOffset =
                    side * (Mathf.Sin(phase) * _topShakeSideAmplitude * shakeWeight) +
                    liftDirection * (Mathf.Sin(phase * 0.7f + 1.35f) *
                                     _topShakeForwardAmplitude * shakeWeight) +
                    Vector3.up * (Mathf.Sin(phase * 1.6f + 0.4f) *
                                  _topShakeHeightAmplitude * shakeWeight);

                Quaternion shakeRotation =
                    Quaternion.AngleAxis(Mathf.Sin(phase) * _topShakeRotationAngle * shakeWeight,
                        liftDirection) *
                    Quaternion.AngleAxis(Mathf.Sin(phase * 0.7f + 1.35f) *
                                         _topShakeRotationAngle * 0.6f * shakeWeight, side) *
                    liftedRotation;

                Vector3 shakenPosition = liftedPosition + shakeOffset;
                character.SetCapturedPose(shakenPosition, shakeRotation);
                BendToLiftedCharacter(bones, rootPosition, shakenPosition, liftDirection, 1f, 1f);

                yield return null;
            }
        }

        private IEnumerator UnwindAndThrowCharacter(
            Transform[] bones,
            ICapturableCharacter character,
            Vector3 rootPosition,
            Vector3 liftedPosition,
            Quaternion liftedRotation,
            Vector3 liftDirection)
        {
            Vector3 side = Vector3.Cross(Vector3.up, liftDirection);
            if (side.sqrMagnitude <= 0.001f)
            {
                side = Vector3.right;
            }

            side.Normalize();

            Vector3 drawBackPosition = liftedPosition - liftDirection * _throwDrawBackDistance +
                                       Vector3.down * 0.2f;
            Vector3 releasePosition = liftedPosition + liftDirection * _throwExtensionDistance;
            Vector3 releaseVelocity = liftDirection * _throwForwardSpeed +
                                      Vector3.up * _throwUpSpeed;
            Quaternion releaseRotation = Quaternion.AngleAxis(12f, side) *
                                         Quaternion.LookRotation(liftDirection, Vector3.up);

            float timer = 0f;
            while (timer < _throwWindupDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / _throwWindupDuration);
                float smoothProgress = GetSmootherStep(progress);

                Vector3 windupPosition = Vector3.Lerp(liftedPosition, drawBackPosition,
                    smoothProgress);
                Quaternion windupRotation = Quaternion.AngleAxis(-28f * smoothProgress, side) *
                                            liftedRotation;

                character.SetCapturedPose(windupPosition, windupRotation);
                BendToLiftedCharacter(bones, rootPosition, windupPosition, liftDirection, 1f, 1f);

                yield return null;
            }

            timer = 0f;
            while (timer < _throwReleaseDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / _throwReleaseDuration);
                float unwrapProgress = Mathf.InverseLerp(_throwUnwrapStart, 1f, progress);
                float wrapBlend = 1f - GetSmootherStep(unwrapProgress);

                Vector3 releaseControl = releasePosition - releaseVelocity *
                                         (_throwReleaseDuration / 3f);
                Vector3 throwPosePosition = EvaluateBezier(drawBackPosition, drawBackPosition,
                    releaseControl, releasePosition, progress);
                Quaternion throwPoseRotation = Quaternion.Slerp(
                    Quaternion.AngleAxis(-28f, side) * liftedRotation,
                    releaseRotation,
                    GetSmootherStep(progress));

                character.SetCapturedPose(throwPosePosition, throwPoseRotation);
                BendToLiftedCharacter(bones, rootPosition, throwPosePosition, liftDirection, 1f,
                    wrapBlend);

                yield return null;
            }

            character.SetCapturedPose(releasePosition, releaseRotation);
            BendToLiftedCharacter(bones, rootPosition, releasePosition, liftDirection, 1f, 0f);
            ThrowCharacter(character, releaseVelocity);

            timer = 0f;
            Vector3 followThroughPosition = releasePosition +
                                            liftDirection * _throwFollowThroughDistance -
                                            Vector3.up * 0.35f;
            while (timer < _throwFollowThroughDuration)
            {
                timer += Time.deltaTime;
                float progress = GetSmootherStep(Mathf.Clamp01(timer / _throwFollowThroughDuration));
                Vector3 currentPosition = Vector3.Lerp(releasePosition, followThroughPosition,
                    progress);
                BendToLiftedCharacter(bones, rootPosition, currentPosition, liftDirection, 1f, 0f);
                yield return null;
            }
        }

        private void ThrowCharacter(
            ICapturableCharacter character,
            Vector3 releaseVelocity)
        {
            character.Throw(releaseVelocity, Vector3.zero);
        }

        private static bool HasValidInput(
            Transform[] bones,
            ICapturableCharacter character,
            Transform target,
            Transform liftOrigin)
        {
            if (bones == null || bones.Length < 2 || character == null || target == null ||
                liftOrigin == null)
            {
                return false;
            }

            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 GetFlatDirection(
            Vector3 rootPosition,
            Vector3 targetPosition,
            Transform fallbackTransform)
        {
            Vector3 direction = targetPosition - rootPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = fallbackTransform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector3.forward;
            }

            return direction.normalized;
        }

        private static float GetBoundsRadiusOnAxis(Bounds bounds, Vector3 axis)
        {
            axis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
            return bounds.extents.x * axis.x +
                   bounds.extents.y * axis.y +
                   bounds.extents.z * axis.z;
        }

        private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
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
    }
}
