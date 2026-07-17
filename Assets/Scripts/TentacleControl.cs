using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TentacleControl : MonoBehaviour
{
    private enum TentacleState
    {
        Idle,
        Detecting,
        Reaching,
        Grabbing,
        Holding,
        Throwing,
        Recovering
    }

    private const int PosePointCount = 12;
    private const string RootBoneName = "Root_M";

    [Header("Detection")]
    [SerializeField] private float _detectionRadius = 12f;
    [SerializeField] private LayerMask _characterLayers = ~0;
    [SerializeField] private float _detectionDuration = 0.6f;

    [Header("Grab")]
    [SerializeField] private float _reachDuration = 1.4f;
    [SerializeField] private float _gripDuration = 0.4f;
    [SerializeField] private float _wrapRadius = 0.65f;
    [SerializeField] private Vector3 _characterGrabOffset;

    [Header("Hold")]
    [SerializeField] private float _liftDuration = 0.9f;
    [SerializeField] private float _holdDuration = 1.2f;
    [SerializeField] private float _holdDistance = 2.1f;
    [SerializeField] private float _holdHeight = 3.1f;
    [SerializeField] private float _holdSway = 0.08f;

    [Header("Throw")]
    [SerializeField] private float _throwWindupDuration = 0.35f;
    [SerializeField] private float _throwForce = 8f;
    [SerializeField] private float _throwUpwardForce = 4.5f;
    [SerializeField] private float _throwFollowThroughDuration = 0.4f;

    [Header("Recovery")]
    [SerializeField] private float _recoveryDuration = 0.75f;
    [SerializeField] private float _cycleCooldown = 1.5f;

    private readonly Collider[] _detectedColliders = new Collider[8];
    private readonly SplinePoint[] _posePoints = new SplinePoint[PosePointCount];

    private Animator _animator;
    private SplineComputer _spline;
    private Transform[] _bones;
    private Quaternion[] _boneRotationOffsets;
    private Vector3[] _startPositions;
    private Quaternion[] _startRotations;
    private Vector3[] _targetPositions;
    private Quaternion[] _targetRotations;
    private Vector3 _rootPosition;
    private Vector3 _currentDirection;
    private TentacleState _state;
    private float _nextDetectionTime;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.enabled = false;

        _spline = gameObject.AddComponent<SplineComputer>();
        _spline.space = SplineComputer.Space.Local;
        _spline.type = Spline.Type.CatmullRom;
        _spline.sampleRate = 16;

        CacheBoneChain();
        CacheBoneRotationOffsets();
        CreatePoseBuffers();

        _rootPosition = _bones[0].position;
        _currentDirection = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        BuildIdlePose(_currentDirection);
        EvaluateTargetPose();
        ApplyTargetPose();
        _state = TentacleState.Idle;
    }

    private void Update()
    {
        DetectCharacter();
    }

    private void DetectCharacter()
    {
        if (_state != TentacleState.Idle || Time.time < _nextDetectionTime)
        {
            return;
        }

        int detectedCount = Physics.OverlapSphereNonAlloc(_rootPosition, _detectionRadius,
            _detectedColliders, _characterLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < detectedCount; i++)
        {
            CharacterControl character = _detectedColliders[i].GetComponentInParent<CharacterControl>();

            if (character == null || !character.CanBeCaptured)
            {
                continue;
            }

            _state = TentacleState.Detecting;
            StartCoroutine(RunBehaviorCycle(character));
            return;
        }
    }

    private IEnumerator RunBehaviorCycle(CharacterControl character)
    {
        _currentDirection = GetHorizontalDirection(character.transform.position);
        yield return PlayDetection();

        if (!CanReachCharacter(character))
        {
            yield return RecoverToIdle();
            yield break;
        }

        yield return ReachCharacter(character);

        if (!character.TryBeginCapture())
        {
            yield return RecoverToIdle();
            yield break;
        }

        Vector3 grabCenter = GetCharacterGrabCenter(character);
        yield return TightenGrip(character, grabCenter);

        Vector3 holdCenter = _rootPosition + _currentDirection * _holdDistance +
                             Vector3.up * _holdHeight;
        yield return LiftCharacter(character, grabCenter, holdCenter);
        yield return HoldCharacter(character, holdCenter);
        yield return ThrowCharacter(character, holdCenter);
        yield return RecoverToIdle();
    }

    private IEnumerator PlayDetection()
    {
        BuildAlertPose(_currentDirection);
        yield return AnimateToBuiltPose(_detectionDuration);
    }

    private IEnumerator ReachCharacter(CharacterControl character)
    {
        _state = TentacleState.Reaching;
        CaptureCurrentBonePose();
        float timer = 0f;

        while (timer < _reachDuration)
        {
            timer += Time.deltaTime;
            Vector3 grabCenter = GetCharacterGrabCenter(character);
            _currentDirection = GetHorizontalDirection(grabCenter);
            BuildReachPose(grabCenter, _currentDirection);
            EvaluateTargetPose();
            ApplyPoseBlend(GetEasedProgress(timer, _reachDuration));
            yield return null;
        }
    }

    private IEnumerator TightenGrip(CharacterControl character, Vector3 grabCenter)
    {
        _state = TentacleState.Grabbing;
        CaptureCurrentBonePose();
        Quaternion startRotation = character.transform.rotation;
        float timer = 0f;

        while (timer < _gripDuration)
        {
            timer += Time.deltaTime;
            float progress = GetEasedProgress(timer, _gripDuration);
            float radius = Mathf.Lerp(_wrapRadius * 1.18f, _wrapRadius, progress);
            Quaternion characterRotation = Quaternion.Slerp(startRotation, GetHeldRotation(),
                progress * 0.35f);
            BuildWrapPose(grabCenter, radius, _currentDirection,
                characterRotation * Vector3.up);
            EvaluateTargetPose();
            ApplyPoseBlend(progress);
            character.SetCapturedPose(grabCenter - _characterGrabOffset,
                characterRotation);
            yield return null;
        }

        ApplyTargetPose();
    }

    private IEnumerator LiftCharacter(CharacterControl character, Vector3 grabCenter,
        Vector3 holdCenter)
    {
        _state = TentacleState.Holding;
        Quaternion startRotation = character.transform.rotation;
        Quaternion heldRotation = GetHeldRotation();
        float timer = 0f;

        while (timer < _liftDuration)
        {
            timer += Time.deltaTime;
            float progress = GetEasedProgress(timer, _liftDuration);
            Vector3 currentCenter = Vector3.Lerp(grabCenter, holdCenter, progress);
            Quaternion characterRotation = Quaternion.Slerp(startRotation, heldRotation, progress);
            BuildWrapPose(currentCenter, _wrapRadius, _currentDirection,
                characterRotation * Vector3.up);
            EvaluateTargetPose();
            ApplyTargetPose();
            character.SetCapturedPose(currentCenter - _characterGrabOffset,
                characterRotation);
            yield return null;
        }
    }

    private IEnumerator HoldCharacter(CharacterControl character, Vector3 holdCenter)
    {
        float timer = 0f;

        while (timer < _holdDuration)
        {
            timer += Time.deltaTime;
            float sway = Mathf.Sin(timer * Mathf.PI * 2f) * _holdSway;
            Vector3 currentCenter = holdCenter + Vector3.up * sway;
            Quaternion characterRotation = GetHeldRotation();
            BuildWrapPose(currentCenter, _wrapRadius, _currentDirection,
                characterRotation * Vector3.up);
            EvaluateTargetPose();
            ApplyTargetPose();
            character.SetCapturedPose(currentCenter - _characterGrabOffset, characterRotation);
            yield return null;
        }
    }

    private IEnumerator ThrowCharacter(CharacterControl character, Vector3 holdCenter)
    {
        _state = TentacleState.Throwing;
        Vector3 windupCenter = holdCenter - _currentDirection * 0.55f + Vector3.up * 0.3f;
        float timer = 0f;

        while (timer < _throwWindupDuration)
        {
            timer += Time.deltaTime;
            float progress = GetEasedProgress(timer, _throwWindupDuration);
            Vector3 currentCenter = Vector3.Lerp(holdCenter, windupCenter, progress);
            Quaternion characterRotation = GetHeldRotation();
            BuildWrapPose(currentCenter, _wrapRadius, _currentDirection,
                characterRotation * Vector3.up);
            EvaluateTargetPose();
            ApplyTargetPose();
            character.SetCapturedPose(currentCenter - _characterGrabOffset, characterRotation);
            yield return null;
        }

        Vector3 throwVelocity = _currentDirection * _throwForce +
                                Vector3.up * _throwUpwardForce;
        character.Throw(throwVelocity, Vector3.Cross(Vector3.up, _currentDirection) * 4f);

        BuildThrowPose(_currentDirection);
        yield return AnimateToBuiltPose(_throwFollowThroughDuration);
    }

    private IEnumerator RecoverToIdle()
    {
        _state = TentacleState.Recovering;
        BuildIdlePose(_currentDirection);
        yield return AnimateToBuiltPose(_recoveryDuration);
        _nextDetectionTime = Time.time + _cycleCooldown;
        _state = TentacleState.Idle;
    }

    private IEnumerator AnimateToBuiltPose(float duration)
    {
        CaptureCurrentBonePose();
        EvaluateTargetPose();
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            ApplyPoseBlend(GetEasedProgress(timer, duration));
            yield return null;
        }

        ApplyTargetPose();
    }

    private void BuildIdlePose(Vector3 direction)
    {
        Vector3 up = Vector3.up;
        Vector3 normal = GetPoseNormal(direction);
        SetPosePoint(0, _rootPosition, normal);
        SetPosePoint(1, _rootPosition + up * 0.35f, normal);
        SetPosePoint(2, _rootPosition + up * 0.75f, normal);
        SetPosePoint(3, _rootPosition + up * 1.15f, normal);
        SetPosePoint(4, _rootPosition + up * 1.55f, normal);
        SetPosePoint(5, _rootPosition + up * 1.95f, normal);
        SetPosePoint(6, _rootPosition + up * 2.35f, normal);
        SetPosePoint(7, _rootPosition + up * 2.72f, normal);
        SetPosePoint(8, _rootPosition + up * 3.02f + direction * 0.08f, normal);
        SetPosePoint(9, _rootPosition + up * 3.2f + direction * 0.3f, normal);
        SetPosePoint(10, _rootPosition + up * 3.12f + direction * 0.55f, normal);
        SetPosePoint(11, _rootPosition + up * 2.88f + direction * 0.58f, normal);
    }

    private void BuildAlertPose(Vector3 direction)
    {
        Vector3 up = Vector3.up;
        Vector3 normal = GetPoseNormal(direction);
        SetPosePoint(0, _rootPosition, normal);
        SetPosePoint(1, _rootPosition + up * 0.38f, normal);
        SetPosePoint(2, _rootPosition + up * 0.8f + direction * 0.08f, normal);
        SetPosePoint(3, _rootPosition + up * 1.2f + direction * 0.18f, normal);
        SetPosePoint(4, _rootPosition + up * 1.6f + direction * 0.12f, normal);
        SetPosePoint(5, _rootPosition + up * 2f - direction * 0.08f, normal);
        SetPosePoint(6, _rootPosition + up * 2.38f - direction * 0.16f, normal);
        SetPosePoint(7, _rootPosition + up * 2.72f - direction * 0.03f, normal);
        SetPosePoint(8, _rootPosition + up * 2.98f + direction * 0.2f, normal);
        SetPosePoint(9, _rootPosition + up * 3.08f + direction * 0.48f, normal);
        SetPosePoint(10, _rootPosition + up * 2.94f + direction * 0.68f, normal);
        SetPosePoint(11, _rootPosition + up * 2.7f + direction * 0.63f, normal);
    }

    private void BuildReachPose(Vector3 center, Vector3 direction)
    {
        Vector3 normal = GetPoseNormal(direction);
        Vector3 contactPoint = center - direction * (_wrapRadius + 0.08f);
        float archHeight = Mathf.Max(_rootPosition.y + 2.5f, center.y + 1.5f);
        Vector3 firstControlPoint = new(_rootPosition.x, archHeight, _rootPosition.z);
        Vector3 secondControlPoint = contactPoint - direction * 0.75f;
        secondControlPoint.y = archHeight;

        for (int i = 0; i < PosePointCount; i++)
        {
            float progress = (float)i / (PosePointCount - 1);
            Vector3 position = GetCubicBezierPoint(_rootPosition, firstControlPoint,
                secondControlPoint, contactPoint, progress);
            SetPosePoint(i, position, normal);
        }
    }

    private void BuildWrapPose(Vector3 center, float radius, Vector3 direction,
        Vector3 characterAxis)
    {
        Vector3 up = Vector3.up;
        Vector3 entryDirection = Vector3.ProjectOnPlane(-direction, characterAxis).normalized;
        Vector3 side = Vector3.Cross(characterAxis, entryDirection).normalized;
        Vector3 approach = center + entryDirection * (radius + 0.2f);
        Vector3 normal = GetPoseNormal(direction);

        SetPosePoint(0, _rootPosition, normal);
        SetPosePoint(1, _rootPosition + up * 0.4f, normal);
        SetPosePoint(2, Vector3.Lerp(_rootPosition, approach, 0.25f) + up * 1.4f, normal);
        SetPosePoint(3, Vector3.Lerp(_rootPosition, approach, 0.5f) + up * 1.35f, normal);
        SetPosePoint(4, Vector3.Lerp(_rootPosition, approach, 0.75f) + up * 0.75f, normal);
        SetPosePoint(5, approach, normal);
        SetPosePoint(6, center + entryDirection * radius, normal);
        SetPosePoint(7, center + side * radius, normal);
        SetPosePoint(8, center - entryDirection * radius, normal);
        SetPosePoint(9, center - side * radius, normal);
        SetPosePoint(10, center + entryDirection * radius, normal);
        SetPosePoint(11, center + side * radius * 0.65f - entryDirection * radius * 0.15f,
            normal);
    }

    private void BuildThrowPose(Vector3 direction)
    {
        Vector3 up = Vector3.up;
        Vector3 normal = GetPoseNormal(direction);
        SetPosePoint(0, _rootPosition, normal);
        SetPosePoint(1, _rootPosition + up * 0.35f, normal);
        SetPosePoint(2, _rootPosition + direction * 0.5f + up * 0.9f, normal);
        SetPosePoint(3, _rootPosition + direction * 1.1f + up * 1.55f, normal);
        SetPosePoint(4, _rootPosition + direction * 1.8f + up * 2.05f, normal);
        SetPosePoint(5, _rootPosition + direction * 2.55f + up * 2.25f, normal);
        SetPosePoint(6, _rootPosition + direction * 3.3f + up * 2.15f, normal);
        SetPosePoint(7, _rootPosition + direction * 4f + up * 1.9f, normal);
        SetPosePoint(8, _rootPosition + direction * 4.55f + up * 1.55f, normal);
        SetPosePoint(9, _rootPosition + direction * 4.9f + up * 1.15f, normal);
        SetPosePoint(10, _rootPosition + direction * 5.05f + up * 0.8f, normal);
        SetPosePoint(11, _rootPosition + direction * 5.15f + up * 0.62f, normal);
    }

    private void SetPosePoint(int index, Vector3 position, Vector3 normal)
    {
        SplinePoint point = new(position)
        {
            normal = normal,
            type = SplinePoint.Type.SmoothMirrored
        };
        _posePoints[index] = point;
    }

    private void EvaluateTargetPose()
    {
        _spline.SetPoints(_posePoints, SplineComputer.Space.World);

        for (int i = 0; i < _bones.Length; i++)
        {
            double percent = (double)i / (_bones.Length - 1);
            SplineSample sample = _spline.Evaluate(percent, SplineComputer.EvaluateMode.Calculate);
            _targetPositions[i] = sample.position;
            _targetRotations[i] = sample.rotation * _boneRotationOffsets[i];
        }
    }

    private void ApplyTargetPose()
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].SetPositionAndRotation(_targetPositions[i], _targetRotations[i]);
        }
    }

    private void CaptureCurrentBonePose()
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            _startPositions[i] = _bones[i].position;
            _startRotations[i] = _bones[i].rotation;
        }
    }

    private void ApplyPoseBlend(float progress)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].SetPositionAndRotation(
                Vector3.Lerp(_startPositions[i], _targetPositions[i], progress),
                Quaternion.Slerp(_startRotations[i], _targetRotations[i], progress));
        }
    }

    private void CacheBoneChain()
    {
        Transform currentBone = FindBone(RootBoneName);
        List<Transform> boneChain = new();

        while (currentBone != null)
        {
            boneChain.Add(currentBone);
            currentBone = GetNextBone(currentBone);
        }

        _bones = boneChain.ToArray();
    }

    private Transform FindBone(string boneName)
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i].name == boneName)
            {
                return childTransforms[i];
            }
        }

        throw new MissingReferenceException($"Tentacle bone '{boneName}' was not found.");
    }

    private static Transform GetNextBone(Transform bone)
    {
        for (int i = 0; i < bone.childCount; i++)
        {
            Transform child = bone.GetChild(i);

            if (child.name.StartsWith("RootPart") || child.name.StartsWith("Tail"))
            {
                return child;
            }
        }

        return null;
    }

    private void CacheBoneRotationOffsets()
    {
        _boneRotationOffsets = new Quaternion[_bones.Length];

        for (int i = 0; i < _bones.Length - 1; i++)
        {
            Vector3 direction = (_bones[i + 1].position - _bones[i].position).normalized;
            Vector3 up = Vector3.ProjectOnPlane(_bones[i].up, direction).normalized;

            if (up.sqrMagnitude < 0.001f)
            {
                up = Vector3.ProjectOnPlane(_bones[i].forward, direction).normalized;
            }

            Quaternion referenceRotation = Quaternion.LookRotation(direction, up);
            _boneRotationOffsets[i] = Quaternion.Inverse(referenceRotation) * _bones[i].rotation;
        }

        _boneRotationOffsets[^1] = _boneRotationOffsets[^2];
    }

    private void CreatePoseBuffers()
    {
        _startPositions = new Vector3[_bones.Length];
        _startRotations = new Quaternion[_bones.Length];
        _targetPositions = new Vector3[_bones.Length];
        _targetRotations = new Quaternion[_bones.Length];
    }

    private bool CanReachCharacter(CharacterControl character)
    {
        Vector3 offset = character.transform.position - _rootPosition;
        offset.y = 0f;
        return character.CanBeCaptured &&
               offset.sqrMagnitude <= _detectionRadius * _detectionRadius;
    }

    private Vector3 GetCharacterGrabCenter(CharacterControl character)
    {
        return character.transform.position + _characterGrabOffset;
    }

    private Vector3 GetHorizontalDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - _rootPosition;
        direction.y = 0f;
        return direction.normalized;
    }

    private Vector3 GetPoseNormal(Vector3 direction)
    {
        return Vector3.Cross(direction, Vector3.up).normalized;
    }

    private Quaternion GetHeldRotation()
    {
        Vector3 rotationAxis = GetPoseNormal(_currentDirection);
        return Quaternion.AngleAxis(65f, rotationAxis);
    }

    private static float GetEasedProgress(float timer, float duration)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / duration));
    }

    private static Vector3 GetCubicBezierPoint(Vector3 start, Vector3 firstControlPoint,
        Vector3 secondControlPoint, Vector3 end, float progress)
    {
        float inverseProgress = 1f - progress;
        return inverseProgress * inverseProgress * inverseProgress * start +
               3f * inverseProgress * inverseProgress * progress * firstControlPoint +
               3f * inverseProgress * progress * progress * secondControlPoint +
               progress * progress * progress * end;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}
