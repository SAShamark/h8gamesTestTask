using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tentacle
{
    [Serializable]
    public class TentaclePoseSolver
    {
        private const int CurveSamplesCount = 64;
        private const int ArcHeightSearchIterations = 12;

        [SerializeField] private float _reachDuration = 0.42f;
        [SerializeField] private float _targetHeight = 1f;
        [SerializeField] private float _targetSurfaceOffset = 0.35f;
        [SerializeField] private float _minimumArcHeight = 0.3f;
        [SerializeField] private float _maximumStretch = 1.08f;

        [Header("Windup")]
        [SerializeField] private float _windupDuration = 0.245f;
        [SerializeField] private float _windupDistance = 2.4f;
        [SerializeField] private float _windupHeight = 1f;
        [SerializeField, Range(0.55f, 0.85f)] private float _windupCurlStart = 0.7f;
        [SerializeField] private float _windupCurlRadius = 0.45f;
        [SerializeField] private float _windupCurlTurns = 0.75f;

        [Header("Wrap")]
        [SerializeField] private float _wrapDuration = 0.455f;
        [SerializeField, Range(0.5f, 0.85f)] private float _wrapStart = 0.66f;
        [SerializeField] private float _wrapRadius = 0.35f;
        [SerializeField] private float _wrapTurns = 1.05f;
        [SerializeField] private float _wrapVerticalOffset = 0.05f;
        [SerializeField] private float _releaseDuration = 0.28f;

        [Header("Follow Through")]
        [SerializeField] private float _followThroughDuration = 0.2f;
        [SerializeField] private float _followThroughAngle = 12f;

        [Header("Retraction")]
        [SerializeField] private float _retractDuration = 0.45f;
        [SerializeField] private float _minimumHeightOffset = 0.03f;

        private Transform[] _bones;
        private float[] _boneDistanceRatios;
        private Vector3[] _baseLocalPositions;
        private Quaternion[] _baseLocalRotations;
        private Vector3[] _sourceLocalPositions;
        private Quaternion[] _sourceLocalRotations;
        private Vector3[] _animatedPositions;
        private Quaternion[] _animatedRotations;
        private Vector3[] _windupPositions;
        private Vector3[] _reachPositions;
        private Vector3[] _targetPositions;
        private Quaternion[] _targetRotations;
        private Vector3[] _followThroughStartPositions;
        private Quaternion[] _followThroughStartRotations;
        private Vector3[] _retractStartLocalPositions;
        private Quaternion[] _retractStartLocalRotations;
        private Quaternion[] _rotationOffsets;
        private Vector3[] _curveSamples;
        private float[] _curveDistances;

        private float _chainLength;
        private float _windupWeight;
        private float _reachWeight;
        private float _wrapWeight;
        private float _followThroughProgress;
        private float _retractProgress;
        private float _minimumWorldHeight;
        private Vector3 _followThroughAxis;
        private Vector3 _rotationUp;
        private Vector3 _wrapCenter;
        private Vector3 _wrapApproach;
        private Vector3 _wrapSide;
        private bool _hasActivePose;
        private bool _isReleasing;
        private bool _isFollowingThrough;
        private bool _isRetracting;

        public bool IsWrapped => _wrapWeight >= 1f;
        public bool IsUnwrapped => _isReleasing && _wrapWeight <= 0f;
        public bool IsFollowingThrough => _isFollowingThrough;
        public bool IsIdle => !_isRetracting && _windupWeight <= 0f;
        public float ReleaseProgress => 1f - _wrapWeight;
        public float TargetCenterOffset => _targetHeight + _wrapVerticalOffset;

        public void BeginRelease()
        {
            _isReleasing = true;
        }

        public void BeginFollowThrough(Vector3 axis)
        {
            _isReleasing = false;
            _isFollowingThrough = true;
            _isRetracting = true;
            _followThroughProgress = 0f;
            _retractProgress = 0f;
            _followThroughAxis = axis.normalized;

            CaptureFollowThroughStartPose();
        }

        public void Initialize(Transform rootBone, float groundHeight)
        {
            _bones = BuildBoneChain(rootBone);
            _minimumWorldHeight = groundHeight + _minimumHeightOffset;
            CreateBuffers();
            CaptureBaseLocalPose();
        }

        public void UpdatePose(bool shouldReach, Transform target, float deltaTime)
        {
            if (_isRetracting)
            {
                UpdateRetraction(deltaTime);
                return;
            }

            PrepareProceduralSourcePose(shouldReach);
            UpdatePhaseWeights(shouldReach, deltaTime);

            if (_windupWeight <= 0f)
            {
                _hasActivePose = false;
                return;
            }

            CacheAnimatedPose();

            if (!_hasActivePose)
            {
                CaptureRotationProfile();
                _hasActivePose = true;
            }

            BuildTargetPose(target);
            ApplyTargetPose();
        }

        private void UpdateRetraction(float deltaTime)
        {
            if (_isFollowingThrough)
            {
                UpdateFollowThrough(deltaTime);
                return;
            }

            _retractProgress = Mathf.MoveTowards(
                _retractProgress, 1f, deltaTime / _retractDuration);
            float blend = Mathf.SmoothStep(0f, 1f, _retractProgress);

            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = Vector3.Lerp(
                    _retractStartLocalPositions[i],
                    _baseLocalPositions[i], blend);
                _bones[i].localRotation = Quaternion.Slerp(
                    _retractStartLocalRotations[i],
                    _baseLocalRotations[i], blend);

                if (i > 0 && _bones[i].position.y < _minimumWorldHeight)
                {
                    Vector3 position = _bones[i].position;
                    position.y = _minimumWorldHeight;
                    _bones[i].position = position;
                }
            }

            if (_retractProgress < 1f)
            {
                return;
            }

            _windupWeight = 0f;
            _reachWeight = 0f;
            _wrapWeight = 0f;
            RestoreBaseLocalPose();
            _hasActivePose = false;
            _isRetracting = false;
        }

        private void UpdateFollowThrough(float deltaTime)
        {
            _followThroughProgress = Mathf.MoveTowards(_followThroughProgress, 1f, deltaTime / _followThroughDuration);
            float progress = Mathf.Sin(_followThroughProgress * Mathf.PI * 0.5f);
            Quaternion rotation = Quaternion.AngleAxis(_followThroughAngle * progress, _followThroughAxis);
            Vector3 pivot = _followThroughStartPositions[0];

            for (int i = 0; i < _bones.Length; i++)
            {
                Vector3 position = pivot + rotation *
                    (_followThroughStartPositions[i] - pivot);

                if (i > 0)
                {
                    position.y = Mathf.Max(position.y, _minimumWorldHeight);
                }

                _bones[i].position = position;
                _bones[i].rotation = rotation * _followThroughStartRotations[i];
            }

            if (_followThroughProgress < 1f)
            {
                return;
            }

            CaptureRetractionStartLocalPose();
            _isFollowingThrough = false;
            _retractProgress = 0f;
        }

        private void CaptureFollowThroughStartPose()
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _followThroughStartPositions[i] = _bones[i].position;
                _followThroughStartRotations[i] = _bones[i].rotation;
            }
        }

        private void CaptureRetractionStartLocalPose()
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _retractStartLocalPositions[i] = _bones[i].localPosition;
                _retractStartLocalRotations[i] = _bones[i].localRotation;
            }
        }

        private void PrepareProceduralSourcePose(bool shouldReach)
        {
            if (!_hasActivePose)
            {
                if (shouldReach)
                {
                    CaptureSourceLocalPose();
                }

                return;
            }

            RestoreSourceLocalPose();
        }

        private void UpdatePhaseWeights(bool shouldReach, float deltaTime)
        {
            if (_isReleasing)
            {
                _wrapWeight = Mathf.MoveTowards(
                    _wrapWeight, 0f, deltaTime / _releaseDuration);
                return;
            }

            if (shouldReach)
            {
                _windupWeight = Mathf.MoveTowards(
                    _windupWeight, 1f, deltaTime / _windupDuration);

                if (_windupWeight >= 1f)
                {
                    _reachWeight = Mathf.MoveTowards(
                        _reachWeight, 1f, deltaTime / _reachDuration);
                }

                if (_reachWeight >= 1f)
                {
                    _wrapWeight = Mathf.MoveTowards(_wrapWeight, 1f, deltaTime / _wrapDuration);
                }

                return;
            }

            _wrapWeight = Mathf.MoveTowards(_wrapWeight, 0f, deltaTime / _wrapDuration);

            if (_wrapWeight <= 0f)
            {
                _reachWeight = Mathf.MoveTowards(_reachWeight, 0f, deltaTime / _reachDuration);
            }

            if (_reachWeight <= 0f)
            {
                _windupWeight = Mathf.MoveTowards(
                    _windupWeight, 0f, deltaTime / _windupDuration);
            }
        }

        private void CacheAnimatedPose()
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _animatedPositions[i] = _bones[i].position;
                _animatedRotations[i] = _bones[i].rotation;
            }
        }

        private void CaptureBaseLocalPose()
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _baseLocalPositions[i] = _bones[i].localPosition;
                _baseLocalRotations[i] = _bones[i].localRotation;
            }
        }

        private void RestoreBaseLocalPose()
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].SetLocalPositionAndRotation(
                    _baseLocalPositions[i], _baseLocalRotations[i]);
            }
        }

        private void CaptureSourceLocalPose()
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _sourceLocalPositions[i] = _bones[i].localPosition;
                _sourceLocalRotations[i] = _bones[i].localRotation;
            }
        }

        private void RestoreSourceLocalPose()
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].SetLocalPositionAndRotation(
                    _sourceLocalPositions[i], _sourceLocalRotations[i]);
            }
        }

        private void BuildTargetPose(Transform target)
        {
            Vector3 start = _animatedPositions[0];
            Vector3 targetPosition = GetTargetPosition(start, target);
            float maximumReach = _chainLength * _maximumStretch;
            Vector3 end = ClampToReach(start, targetPosition, maximumReach);
            float desiredCurveLength = Mathf.Max(_chainLength, Vector3.Distance(start, end));
            float arcHeight = FindArcHeight(start, end, desiredCurveLength);

            BuildCurveSamples(start, end, arcHeight);
            PlaceBonesOnCurve(_reachPositions, _bones.Length - 1);

            BuildWindupPose(start, target);

            if (_reachWeight < 1f)
            {
                BlendWindupAndReach();
                CalculateBoneRotations();
                return;
            }

            BuildWrappedPose(start, target);
            CalculateBoneRotations();
        }

        private void BuildWindupPose(Vector3 start, Transform target)
        {
            Vector3 targetDirection = Vector3.ProjectOnPlane(
                target.position - start, target.up).normalized;
            Vector3 awayFromTarget = -targetDirection;
            int curlStartIndex = Mathf.RoundToInt(
                (_bones.Length - 1) * _windupCurlStart);
            Vector3 bodyEnd = start + awayFromTarget * _windupDistance +
                              target.up * _windupHeight;
            float bodyDistance = Vector3.Distance(start, bodyEnd);
            Vector3 firstControl = start + awayFromTarget * (_windupDistance * 0.12f) +
                                   target.up * (_windupHeight * 1.4f);
            Vector3 secondControl = bodyEnd - awayFromTarget * (bodyDistance * 0.22f);

            BuildCurveSamples(start, firstControl, secondControl, bodyEnd);
            PlaceBonesOnCurve(_windupPositions, curlStartIndex);

            Vector3 curlCenter = bodyEnd + target.up * _windupCurlRadius;

            for (int i = curlStartIndex; i < _bones.Length; i++)
            {
                float progress = Mathf.InverseLerp(
                    curlStartIndex, _bones.Length - 1, i);
                float angle = -Mathf.PI * 0.5f -
                              Mathf.PI * 2f * _windupCurlTurns * progress;
                _windupPositions[i] = curlCenter +
                                      targetDirection * (Mathf.Cos(angle) * _windupCurlRadius) +
                                      target.up * (Mathf.Sin(angle) * _windupCurlRadius);
            }
        }

        private void BlendWindupAndReach()
        {
            float blend = Mathf.SmoothStep(0f, 1f, _reachWeight);

            for (int i = 0; i < _bones.Length; i++)
            {
                _targetPositions[i] = Vector3.Lerp(
                    _windupPositions[i], _reachPositions[i], blend);
            }
        }

        private void BuildWrappedPose(Vector3 start, Transform target)
        {
            _wrapCenter = target.position + target.up * (_targetHeight + _wrapVerticalOffset);
            _wrapApproach = Vector3.ProjectOnPlane(start - _wrapCenter, target.up).normalized;
            _wrapSide = Vector3.Cross(target.up, _wrapApproach).normalized;
            Vector3 entry = _wrapCenter + _wrapApproach * _wrapRadius;
            int wrapStartIndex = Mathf.RoundToInt((_bones.Length - 1) * _wrapStart);
            float wrapProgress = Mathf.SmoothStep(0f, 1f, _wrapWeight);
            Vector3 bodyEnd = GetApproachPosition(wrapStartIndex, wrapProgress, entry);
            float bodyLength = _chainLength * _boneDistanceRatios[wrapStartIndex];
            float arcHeight = FindArcHeight(start, bodyEnd,
                Mathf.Max(bodyLength, Vector3.Distance(start, bodyEnd)));
            float nextProgress = Mathf.Min(wrapProgress + 0.05f, 1f);
            Vector3 approachTangent = wrapProgress < 1f
                ? (GetApproachPosition(wrapStartIndex, nextProgress, entry) - bodyEnd).normalized
                : -_wrapSide;
            float tangentBlend = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.75f, 1f, wrapProgress));
            Vector3 bodyEndTangent = Vector3.Slerp(
                approachTangent, -_wrapSide, tangentBlend).normalized;
            float bodyDistance = Vector3.Distance(start, bodyEnd);
            Vector3 firstControl = start + Vector3.up * arcHeight +
                                   (bodyEnd - start).normalized * bodyDistance * 0.15f;
            Vector3 secondControl = bodyEnd - bodyEndTangent * bodyDistance * 0.16f;

            BuildCurveSamples(start, firstControl, secondControl, bodyEnd);
            PlaceBonesOnCurve(_targetPositions, wrapStartIndex);

            for (int i = wrapStartIndex; i < _bones.Length; i++)
            {
                float boneProgress = Mathf.InverseLerp(
                    wrapStartIndex, _bones.Length - 1, i);
                float pathProgress = boneProgress + wrapProgress;

                _targetPositions[i] = pathProgress <= 1f
                    ? GetApproachPosition(wrapStartIndex, pathProgress, entry)
                    : GetRingPosition(pathProgress - 1f);
            }

            if (_wrapWeight <= 0f)
            {
                for (int i = 0; i < _bones.Length; i++)
                {
                    _targetPositions[i] = _reachPositions[i];
                }
            }
        }

        private Vector3 GetApproachPosition(int wrapStartIndex, float progress, Vector3 entry)
        {
            float bonePosition = Mathf.Lerp(wrapStartIndex, _bones.Length - 1, progress);
            int firstIndex = Mathf.FloorToInt(bonePosition);
            int secondIndex = Mathf.Min(firstIndex + 1, _bones.Length - 1);
            Vector3 position = Vector3.Lerp(_reachPositions[firstIndex],
                _reachPositions[secondIndex], bonePosition - firstIndex);
            Vector3 entryCorrection = entry - _reachPositions[^1];
            return position + entryCorrection * Mathf.SmoothStep(0f, 1f, progress);
        }

        private Vector3 GetRingPosition(float progress)
        {
            float angle = -Mathf.PI * 2f * _wrapTurns * progress;
            return _wrapCenter +
                   _wrapApproach * (Mathf.Cos(angle) * _wrapRadius) +
                   _wrapSide * (Mathf.Sin(angle) * _wrapRadius);
        }

        private Vector3 GetTargetPosition(Vector3 start, Transform target)
        {
            Vector3 targetPosition = target.position + target.up * _targetHeight;
            Vector3 direction = targetPosition - start;
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            return targetPosition - flatDirection * _targetSurfaceOffset;
        }

        private static Vector3 ClampToReach(Vector3 start, Vector3 target, float maximumDistance)
        {
            Vector3 offset = target - start;

            if (offset.sqrMagnitude <= maximumDistance * maximumDistance)
            {
                return target;
            }

            return start + offset.normalized * maximumDistance;
        }

        private float FindArcHeight(Vector3 start, Vector3 end, float desiredLength)
        {
            float minimumHeight = _minimumArcHeight;
            float maximumHeight = _chainLength;

            for (int i = 0; i < ArcHeightSearchIterations; i++)
            {
                float height = (minimumHeight + maximumHeight) * 0.5f;
                float curveLength = CalculateCurveLength(start, end, height);

                if (curveLength < desiredLength)
                {
                    minimumHeight = height;
                }
                else
                {
                    maximumHeight = height;
                }
            }

            return (minimumHeight + maximumHeight) * 0.5f;
        }

        private float CalculateCurveLength(Vector3 start, Vector3 end, float arcHeight)
        {
            GetControlPoints(start, end, arcHeight, out Vector3 firstControl, out Vector3 secondControl);
            Vector3 previousPoint = start;
            float length = 0f;

            for (int i = 1; i < CurveSamplesCount; i++)
            {
                float time = i / (CurveSamplesCount - 1f);
                Vector3 point = EvaluateBezier(start, firstControl, secondControl, end, time);
                length += Vector3.Distance(previousPoint, point);
                previousPoint = point;
            }

            return length;
        }

        private void BuildCurveSamples(Vector3 start, Vector3 end, float arcHeight)
        {
            GetControlPoints(start, end, arcHeight, out Vector3 firstControl, out Vector3 secondControl);
            BuildCurveSamples(start, firstControl, secondControl, end);
        }

        private void BuildCurveSamples(Vector3 start, Vector3 firstControl,
            Vector3 secondControl, Vector3 end)
        {
            _curveSamples[0] = start;
            _curveDistances[0] = 0f;

            for (int i = 1; i < CurveSamplesCount; i++)
            {
                float time = i / (CurveSamplesCount - 1f);
                _curveSamples[i] = EvaluateBezier(start, firstControl, secondControl, end, time);
                _curveDistances[i] = _curveDistances[i - 1] +
                    Vector3.Distance(_curveSamples[i - 1], _curveSamples[i]);
            }
        }

        private static void GetControlPoints(Vector3 start, Vector3 end, float arcHeight,
            out Vector3 firstControl, out Vector3 secondControl)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            firstControl = start + Vector3.up * arcHeight + direction * distance * 0.15f;
            secondControl = end + Vector3.up * arcHeight * 0.35f - direction * distance * 0.12f;
        }

        private void PlaceBonesOnCurve(Vector3[] positions, int lastBoneIndex)
        {
            float curveLength = _curveDistances[CurveSamplesCount - 1];
            int sampleIndex = 1;
            float lastBoneDistance = _boneDistanceRatios[lastBoneIndex];

            for (int boneIndex = 0; boneIndex <= lastBoneIndex; boneIndex++)
            {
                float targetDistance = curveLength *
                    (_boneDistanceRatios[boneIndex] / lastBoneDistance);

                while (sampleIndex < CurveSamplesCount - 1 &&
                       _curveDistances[sampleIndex] < targetDistance)
                {
                    sampleIndex++;
                }

                float segmentLength = _curveDistances[sampleIndex] - _curveDistances[sampleIndex - 1];
                float segmentProgress = segmentLength > 0f
                    ? (targetDistance - _curveDistances[sampleIndex - 1]) / segmentLength
                    : 0f;

                positions[boneIndex] = Vector3.Lerp(
                    _curveSamples[sampleIndex - 1], _curveSamples[sampleIndex], segmentProgress);
            }
        }

        private void CaptureRotationProfile()
        {
            Vector3 transportedUp = Vector3.zero;

            for (int i = 0; i < _bones.Length - 1; i++)
            {
                Vector3 tangent = (_animatedPositions[i + 1] - _animatedPositions[i]).normalized;

                if (i == 0)
                {
                    transportedUp = GetPerpendicularUp(_animatedRotations[i] * Vector3.up, tangent);
                    _rotationUp = transportedUp;
                }
                else
                {
                    transportedUp = GetPerpendicularUp(transportedUp, tangent);
                }

                Quaternion referenceRotation = Quaternion.LookRotation(tangent, transportedUp);
                _rotationOffsets[i] = Quaternion.Inverse(referenceRotation) * _animatedRotations[i];
            }

            _rotationOffsets[^1] = _rotationOffsets[^2];
        }

        private void CalculateBoneRotations()
        {
            Vector3 transportedUp = _rotationUp;

            for (int i = 0; i < _bones.Length - 1; i++)
            {
                Vector3 tangent = (_targetPositions[i + 1] - _targetPositions[i]).normalized;
                transportedUp = GetPerpendicularUp(transportedUp, tangent);
                _targetRotations[i] = Quaternion.LookRotation(tangent, transportedUp) *
                    _rotationOffsets[i];
            }

            _targetRotations[^1] = _targetRotations[^2];
        }

        private static Vector3 GetPerpendicularUp(Vector3 up, Vector3 tangent)
        {
            Vector3 perpendicularUp = Vector3.ProjectOnPlane(up, tangent);

            if (perpendicularUp.sqrMagnitude <= 0.001f)
            {
                perpendicularUp = Vector3.ProjectOnPlane(Vector3.up, tangent);
            }

            if (perpendicularUp.sqrMagnitude <= 0.001f)
            {
                perpendicularUp = Vector3.ProjectOnPlane(Vector3.right, tangent);
            }

            return perpendicularUp.normalized;
        }

        private void ApplyTargetPose()
        {
            float blend = Mathf.SmoothStep(0f, 1f, _windupWeight);

            for (int i = 0; i < _bones.Length; i++)
            {
                Vector3 targetPosition = _targetPositions[i];

                if (i > 0)
                {
                    targetPosition.y = Mathf.Max(
                        targetPosition.y, _minimumWorldHeight);
                }

                _bones[i].position = Vector3.Lerp(
                    _animatedPositions[i], targetPosition, blend);
                _bones[i].rotation = Quaternion.Slerp(_animatedRotations[i], _targetRotations[i], blend);
            }
        }

        private void CreateBuffers()
        {
            int bonesCount = _bones.Length;
            _boneDistanceRatios = new float[bonesCount];
            _baseLocalPositions = new Vector3[bonesCount];
            _baseLocalRotations = new Quaternion[bonesCount];
            _sourceLocalPositions = new Vector3[bonesCount];
            _sourceLocalRotations = new Quaternion[bonesCount];
            _animatedPositions = new Vector3[bonesCount];
            _animatedRotations = new Quaternion[bonesCount];
            _windupPositions = new Vector3[bonesCount];
            _reachPositions = new Vector3[bonesCount];
            _targetPositions = new Vector3[bonesCount];
            _targetRotations = new Quaternion[bonesCount];
            _followThroughStartPositions = new Vector3[bonesCount];
            _followThroughStartRotations = new Quaternion[bonesCount];
            _retractStartLocalPositions = new Vector3[bonesCount];
            _retractStartLocalRotations = new Quaternion[bonesCount];
            _rotationOffsets = new Quaternion[bonesCount];
            _curveSamples = new Vector3[CurveSamplesCount];
            _curveDistances = new float[CurveSamplesCount];

            for (int i = 1; i < bonesCount; i++)
            {
                _chainLength += Vector3.Distance(_bones[i - 1].position, _bones[i].position);
                _boneDistanceRatios[i] = _chainLength;
            }

            for (int i = 1; i < bonesCount; i++)
            {
                _boneDistanceRatios[i] /= _chainLength;
            }
        }

        private static Transform[] BuildBoneChain(Transform rootBone)
        {
            List<Transform> bones = new();
            Transform currentBone = rootBone;

            while (currentBone != null)
            {
                bones.Add(currentBone);
                currentBone = currentBone.childCount > 0 ? currentBone.GetChild(0) : null;
            }

            return bones.ToArray();
        }

        private static Vector3 EvaluateBezier(Vector3 start, Vector3 firstControl,
            Vector3 secondControl, Vector3 end, float time)
        {
            float inverseTime = 1f - time;
            return inverseTime * inverseTime * inverseTime * start +
                   3f * inverseTime * inverseTime * time * firstControl +
                   3f * inverseTime * time * time * secondControl +
                   time * time * time * end;
        }
    }
}
