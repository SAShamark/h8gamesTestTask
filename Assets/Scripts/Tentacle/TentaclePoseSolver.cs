using UnityEngine;

namespace Tentacle
{
    public sealed class TentaclePoseSolver
    {
        private readonly Transform[] _bones;
        private Vector3[] _startPositions;
        private Quaternion[] _startRotations;
        private Quaternion[] _rotationOffsets;
        private Vector3[] _targetPositions;
        private Vector3[] _wrappedPositions;
        private Vector3[] _idleLocalPositions;
        private Quaternion[] _idleLocalRotations;
        private Vector3[] _recoveryStartPositions;
        private Quaternion[] _recoveryStartRotations;
        private float _tipRadius;
        private float _tipGripHalfHeight;
        private bool _hasGripProfile;

        public TentaclePoseSolver(Transform[] bones)
        {
            _bones = bones;
        }

        public bool HasValidBones()
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

        public void CaptureIdlePose()
        {
            _idleLocalPositions = new Vector3[_bones.Length];
            _idleLocalRotations = new Quaternion[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                _idleLocalPositions[i] = _bones[i].localPosition;
                _idleLocalRotations[i] = _bones[i].localRotation;
            }
        }

        public void BeginGrab(Transform target, TentacleSettings settings)
        {
            CaptureCurrentPose();
            CaptureRotationOffsets();
            Vector3 gripCenter = GetGripCenter(target.position, target.rotation, settings.Lift);
            Vector3 direction = GetFlatDirection(_startPositions[0], gripCenter, target.forward);
            CaptureTargetWrapShape(target, direction, settings.Lift);
        }

        public void UpdateGrab(Transform target, float progress, TentacleSettings settings)
        {
            Vector3 root = _startPositions[0];
            Vector3 targetPosition = GetGripCenter(target.position, target.rotation,
                settings.Lift);
            Vector3 direction = GetFlatDirection(root, targetPosition, target.forward);
            float reach = SmootherStep(Mathf.Clamp01(progress / settings.ReachPhasePortion));
            float wrap = SmootherStep(Mathf.InverseLerp(settings.ReachPhasePortion, 1f, progress));
            Vector3 wrapEntry = targetPosition - direction * _tipRadius;
            Vector3 currentTarget = Vector3.Lerp(root, wrapEntry, reach);
            Vector3 side = GetSide(direction);
            Vector3 p1 = root + direction * settings.ArcForwardOffset +
                         side * settings.ArcSideOffset + Vector3.up * settings.ArcHeight;
            Vector3 p2 = currentTarget - direction * 1.5f -
                         side * settings.ArcSideOffset + Vector3.up * settings.ArcHeight;

            for (int i = 0; i < _bones.Length; i++)
            {
                float t = i / (float)(_bones.Length - 1);
                _targetPositions[i] = EvaluateBezier(root, p1, p2, currentTarget, t);
            }

            BuildLiftPose(root, targetPosition, target.rotation, direction, 1f, settings.Lift,
                _wrappedPositions);

            for (int i = 0; i < _bones.Length; i++)
            {
                _targetPositions[i] = Vector3.Lerp(_targetPositions[i], _wrappedPositions[i],
                    wrap);
            }

            ApplyWorldPose(reach, direction);
        }

        public void BeginLift(Transform target, Vector3 direction, TentacleLiftSettings settings)
        {
            CaptureCurrentPose();
            CaptureRotationOffsets();

            if (!_hasGripProfile)
            {
                CaptureTargetWrapShape(target, direction, settings);
            }
        }

        public void UpdateLift(Vector3 root, Vector3 characterPosition,
            Quaternion characterRotation, Vector3 direction, float progress, float wrapBlend,
            TentacleLiftSettings settings)
        {
            Vector3 gripCenter = GetGripCenter(characterPosition, characterRotation, settings);
            BuildLiftPose(root, gripCenter, characterRotation, direction, wrapBlend, settings,
                _targetPositions);

            ApplyWorldPose(progress, direction);
        }

        public void BeginRecovery()
        {
            _recoveryStartPositions = new Vector3[_bones.Length];
            _recoveryStartRotations = new Quaternion[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                _recoveryStartPositions[i] = _bones[i].localPosition;
                _recoveryStartRotations[i] = _bones[i].localRotation;
            }
        }

        public void UpdateRecovery(float progress)
        {
            float smoothProgress = SmootherStep(progress);
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = Vector3.Lerp(_recoveryStartPositions[i],
                    _idleLocalPositions[i], smoothProgress);
                _bones[i].localRotation = Quaternion.Slerp(_recoveryStartRotations[i],
                    _idleLocalRotations[i], smoothProgress);
            }
        }

        public void RestoreIdlePose()
        {
            if (_idleLocalPositions == null || _idleLocalPositions.Length != _bones.Length)
            {
                return;
            }

            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = _idleLocalPositions[i];
                _bones[i].localRotation = _idleLocalRotations[i];
            }
        }

        public void ClearRuntimePose()
        {
            _startPositions = null;
            _startRotations = null;
            _rotationOffsets = null;
            _targetPositions = null;
            _wrappedPositions = null;
            _recoveryStartPositions = null;
            _recoveryStartRotations = null;
            _hasGripProfile = false;
        }

        public static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
            float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 +
                   3f * u * t * t * p2 + t * t * t * p3;
        }

        public static float SmootherStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }

        private void CaptureCurrentPose()
        {
            _startPositions = new Vector3[_bones.Length];
            _startRotations = new Quaternion[_bones.Length];
            _targetPositions = new Vector3[_bones.Length];
            _wrappedPositions = new Vector3[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                _startPositions[i] = _bones[i].position;
                _startRotations[i] = _bones[i].rotation;
            }
        }

        private void CaptureRotationOffsets()
        {
            _rotationOffsets = new Quaternion[_bones.Length];
            for (int i = 0; i < _bones.Length; i++)
            {
                Vector3 tangent = GetTangent(_startPositions, i);
                if (tangent.sqrMagnitude <= 0.001f)
                {
                    _rotationOffsets[i] = Quaternion.identity;
                    continue;
                }

                Vector3 up = Vector3.ProjectOnPlane(_startRotations[i] * Vector3.up, tangent);
                if (up.sqrMagnitude <= 0.001f)
                {
                    up = Vector3.ProjectOnPlane(Vector3.up, tangent);
                }

                Quaternion reference = Quaternion.LookRotation(tangent.normalized,
                    up.sqrMagnitude > 0.001f ? up.normalized : Vector3.right);
                _rotationOffsets[i] = Quaternion.Inverse(reference) * _startRotations[i];
            }
        }

        private void ApplyWorldPose(float progress, Vector3 fallbackDirection)
        {
            Vector3 firstTangent = GetTangent(_targetPositions, 0);
            Vector3 stableUp = Vector3.ProjectOnPlane(_startRotations[0] * Vector3.up,
                firstTangent);
            if (stableUp.sqrMagnitude <= 0.001f)
            {
                stableUp = Vector3.up;
            }

            for (int i = 0; i < _bones.Length; i++)
            {
                Vector3 tangent = GetTangent(_targetPositions, i);
                if (tangent.sqrMagnitude <= 0.001f)
                {
                    tangent = fallbackDirection;
                }

                Vector3 projectedUp = Vector3.ProjectOnPlane(stableUp, tangent);
                if (projectedUp.sqrMagnitude <= 0.001f)
                {
                    projectedUp = Vector3.ProjectOnPlane(Vector3.up, tangent);
                }

                stableUp = projectedUp.sqrMagnitude > 0.001f ? projectedUp.normalized : Vector3.right;
                Quaternion rotation = Quaternion.LookRotation(tangent.normalized, stableUp) *
                                      _rotationOffsets[i];
                _bones[i].position = Vector3.Lerp(_startPositions[i], _targetPositions[i], progress);
                _bones[i].rotation = Quaternion.Slerp(_startRotations[i], rotation, progress);
            }
        }

        private void CaptureTargetWrapShape(Transform target, Vector3 direction,
            TentacleLiftSettings settings)
        {
            Collider targetCollider = target.GetComponentInParent<Collider>();
            if (targetCollider == null)
            {
                _tipRadius = settings.TipRadius *
                             Mathf.Clamp(settings.TipGripCompression, 0.75f, 1f);
                _tipGripHalfHeight = Mathf.Max(0.08f, settings.TipRadius * settings.TipGripPitch);
                _hasGripProfile = true;
                return;
            }

            Vector3 side = GetSide(direction);
            Bounds bounds = targetCollider.bounds;
            float boundsRadius = Mathf.Max(GetBoundsRadius(bounds, direction),
                GetBoundsRadius(bounds, side));
            float rawRadius = Mathf.Max(settings.TipRadius, boundsRadius) +
                              Mathf.Max(settings.TipSurfaceOffset, 0f);
            _tipRadius = settings.TipMaxGripRadius > 0f
                ? Mathf.Min(rawRadius, settings.TipMaxGripRadius)
                : rawRadius;
            _tipRadius *= Mathf.Clamp(settings.TipGripCompression, 0.75f, 1f);
            float availableGripHeight = Mathf.Max(0.08f, bounds.extents.y -
                settings.TipVerticalInset);
            _tipGripHalfHeight = Mathf.Max(0.08f, Mathf.Min(availableGripHeight,
                _tipRadius * settings.TipGripPitch));
            _hasGripProfile = true;
        }

        private void BuildLiftPose(Vector3 root, Vector3 gripCenter,
            Quaternion characterRotation, Vector3 direction, float wrapBlend,
            TentacleLiftSettings settings, Vector3[] positions)
        {
            Vector3 gripUp = characterRotation * Vector3.up;
            Vector3 gripForward = Vector3.ProjectOnPlane(direction, gripUp);
            if (gripForward.sqrMagnitude <= 0.001f)
            {
                gripForward = characterRotation * Vector3.forward;
            }

            gripForward.Normalize();
            Vector3 gripSide = Vector3.Cross(gripUp, gripForward).normalized;
            Vector3 lowerHandle = root + direction * 0.75f + Vector3.up * settings.LiftArcHeight;
            Vector3 upperHandle = gripCenter - gripUp * settings.LiftArcHeight -
                                  gripForward * (_tipRadius + settings.LiftForwardOffset);
            float bodyPortion = Mathf.Min(settings.BodyBonePortion, 0.46f);
            float turns = Mathf.Max(settings.TipWrapTurns, 1.65f);
            wrapBlend = Mathf.Clamp01(wrapBlend);

            for (int i = 0; i < _bones.Length; i++)
            {
                float t = i / (float)(_bones.Length - 1);
                if (t <= bodyPortion)
                {
                    float bodyT = Mathf.InverseLerp(0f, bodyPortion, t);
                    positions[i] = EvaluateBezier(root, lowerHandle, upperHandle,
                        GetGripEntry(gripCenter, gripForward), bodyT);
                    continue;
                }

                float wrapT = Mathf.InverseLerp(bodyPortion, 1f, t);
                Vector3 wrapCenter = GetRingCenter(gripCenter, gripUp, wrapT, settings);
                float angle = (180f - 360f * turns * wrapT) * Mathf.Deg2Rad;
                Vector3 wrapped = wrapCenter + gripForward * Mathf.Cos(angle) * _tipRadius +
                                  gripSide * Mathf.Sin(angle) * _tipRadius;
                float unwrap = 1f - wrapBlend;
                float localUnwrap = SmootherStep(Mathf.InverseLerp((1f - wrapT) * 0.22f, 1f,
                    unwrap));
                float expansion = 1f + Mathf.Sin(localUnwrap * Mathf.PI) *
                                  Mathf.Max(0f, settings.ThrowUnwrapExpansion);
                Vector3 expanded = wrapCenter + (wrapped - wrapCenter) * expansion;
                Vector3 open = wrapCenter - gripForward * (_tipRadius +
                    Mathf.Max(0f, settings.ThrowUnwrapTrailDistance) * wrapT) +
                    gripSide * (Mathf.Sin(wrapT * Mathf.PI) * _tipRadius * 0.25f);
                float peel = SmootherStep(Mathf.InverseLerp(0.32f, 1f, localUnwrap));
                positions[i] = Vector3.Lerp(expanded, open, peel);
            }
        }

        private static Vector3 GetGripCenter(Vector3 characterPosition,
            Quaternion characterRotation, TentacleLiftSettings settings)
        {
            return characterPosition + characterRotation * Vector3.up *
                settings.TipGripVerticalOffset;
        }

        private Vector3 GetGripEntry(Vector3 center, Vector3 direction)
        {
            return center - direction * _tipRadius;
        }

        private Vector3 GetRingCenter(Vector3 center, Vector3 gripUp, float t,
            TentacleLiftSettings settings)
        {
            float verticalWave = Mathf.Sin(t * Mathf.PI * 2f) * _tipGripHalfHeight *
                                 Mathf.Clamp01(settings.TipGripPitch);
            return center + gripUp * verticalWave;
        }

        private static Vector3 GetFlatDirection(Vector3 from, Vector3 to, Vector3 fallback)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = fallback;
                direction.y = 0f;
            }

            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }

        private static Vector3 GetSide(Vector3 direction)
        {
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            return side.sqrMagnitude > 0.001f ? side.normalized : Vector3.right;
        }

        private static Vector3 GetTangent(Vector3[] positions, int index)
        {
            return index < positions.Length - 1
                ? positions[index + 1] - positions[index]
                : positions[index] - positions[index - 1];
        }

        private static float GetBoundsRadius(Bounds bounds, Vector3 axis)
        {
            axis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
            return bounds.extents.x * axis.x + bounds.extents.y * axis.y +
                   bounds.extents.z * axis.z;
        }
    }
}
