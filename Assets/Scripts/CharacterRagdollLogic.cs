using System.Collections;
using UnityEngine;

[System.Serializable]
public class CharacterRagdollLogic
{
    private const int GroundHitCapacity = 16;

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

    private Transform _characterTransform;
    private Transform _modelTransform;
    private Transform _modelParent;
    private Animator _animator;
    private Rigidbody _hipsBody;
    private Rigidbody[] _bodies;
    private Rigidbody[] _captureAnchorBodies;
    private Vector3[] _captureAnchorLocalPositions;
    private Quaternion[] _captureAnchorLocalRotations;
    private Collider[] _colliders;
    private SkinnedMeshRenderer[] _renderers;
    private bool[] _rendererUpdateWhenOffscreen;
    private Transform[] _bones;
    private Vector3[] _ragdollLocalPositions;
    private Quaternion[] _ragdollLocalRotations;
    private Vector3[] _animatedLocalPositions;
    private Quaternion[] _animatedLocalRotations;
    private Vector3 _modelLocalPosition;
    private Quaternion _modelLocalRotation;
    private Vector3 _hipsCharacterLocalPosition;
    private Quaternion _hipsCharacterLocalRotation;
    private Vector3 _capturedHipsPosition;
    private Quaternion _capturedHipsRotation;
    private Vector3 _groundContactPoint;
    private bool _isRagdollActive;
    private bool _isCaptured;
    private readonly RaycastHit[] _groundHits = new RaycastHit[GroundHitCapacity];

    public Transform ModelTransform => _modelTransform;
    public Transform HipsTransform => _hipsBody.transform;
    public Vector3 GroundContactPoint => _groundContactPoint;

    public void Initialize(Transform characterTransform, Animator animator)
    {
        _characterTransform = characterTransform;
        _animator = animator;
        _modelTransform = animator.transform;
        _modelParent = _modelTransform.parent;
        _modelLocalPosition = _modelTransform.localPosition;
        _modelLocalRotation = _modelTransform.localRotation;

        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        _hipsBody = hips.GetComponent<Rigidbody>();
        _bodies = _modelTransform.GetComponentsInChildren<Rigidbody>(true);
        _captureAnchorBodies = GetCaptureAnchorBodies(animator);
        _captureAnchorLocalPositions = new Vector3[_captureAnchorBodies.Length];
        _captureAnchorLocalRotations = new Quaternion[_captureAnchorBodies.Length];
        _colliders = GetRagdollColliders();
        _renderers = _modelTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        _rendererUpdateWhenOffscreen = new bool[_renderers.Length];
        _bones = new Transform[_bodies.Length];
        _ragdollLocalPositions = new Vector3[_bodies.Length];
        _ragdollLocalRotations = new Quaternion[_bodies.Length];
        _animatedLocalPositions = new Vector3[_bodies.Length];
        _animatedLocalRotations = new Quaternion[_bodies.Length];

        for (int i = 0; i < _bodies.Length; i++)
        {
            _bones[i] = _bodies[i].transform;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            _rendererUpdateWhenOffscreen[i] = _renderers[i].updateWhenOffscreen;
        }

        _hipsCharacterLocalPosition = _characterTransform.InverseTransformPoint(hips.position);
        _hipsCharacterLocalRotation = Quaternion.Inverse(_characterTransform.rotation) * hips.rotation;
        IgnoreSelfCollisions();
        DisableRagdoll();
    }

    public void BeginCapture()
    {
        ActivateRagdoll();
        CaptureAnchorPose();
        SetCaptureAnchorsKinematic(true);
        _capturedHipsPosition = _hipsBody.position;
        _capturedHipsRotation = _hipsBody.rotation;
        _isCaptured = true;
    }

    public void SetCapturedPose(Vector3 position, Quaternion rotation)
    {
        if (!_isRagdollActive)
        {
            return;
        }

        _capturedHipsPosition = position + rotation * _hipsCharacterLocalPosition;
        _capturedHipsRotation = rotation * _hipsCharacterLocalRotation;
    }

    public void UpdateCapturedPose()
    {
        if (!_isCaptured)
        {
            return;
        }

        for (int i = 0; i < _captureAnchorBodies.Length; i++)
        {
            Rigidbody body = _captureAnchorBodies[i];
            Vector3 position = _capturedHipsPosition +
                               _capturedHipsRotation * _captureAnchorLocalPositions[i];
            Quaternion rotation = _capturedHipsRotation * _captureAnchorLocalRotations[i];
            body.MovePosition(position);
            body.MoveRotation(rotation);
        }
    }

    public void BeginThrow()
    {
        _isCaptured = false;

        if (!_isRagdollActive)
        {
            ActivateRagdoll();
        }

        SetBodiesKinematic(false);
    }

    private void ActivateRagdoll()
    {
        Vector3 hipsPosition = _hipsBody.position;
        Quaternion hipsRotation = _hipsBody.rotation;

        _animator.enabled = false;
        SetRenderersUpdateWhenOffscreen(true);
        _modelTransform.SetParent(null, true);
        EnableRagdoll();

        _hipsBody.position = hipsPosition;
        _hipsBody.rotation = hipsRotation;
        _isRagdollActive = true;
    }

    public void Throw(Vector3 velocity)
    {
        velocity = Vector3.ClampMagnitude(velocity, _maximumThrowVelocity);
        SetBodiesKinematic(false);

        for (int i = 0; i < _bodies.Length; i++)
        {
            _bodies[i].velocity = velocity;
            _bodies[i].angularVelocity = Vector3.zero;
            _bodies[i].WakeUp();
        }
    }

    public IEnumerator WaitForLanding()
    {
        float timer = 0f;

        while (timer < _maximumThrowTime)
        {
            timer += Time.deltaTime;
            FollowHips();

            if (timer >= _minimumThrowTime && IsSettledOnGround())
            {
                yield break;
            }

            yield return null;
        }
    }

    public IEnumerator WaitForGroundContact()
    {
        float timer = 0f;

        while (timer < _maximumThrowTime)
        {
            timer += Time.deltaTime;
            FollowHips();

            if (IsTouchingGround())
            {
                yield break;
            }

            yield return null;
        }
    }

    public IEnumerator Recover()
    {
        yield return WaitBeforeRecovery();

        FreezeRagdoll();
        CaptureWorldPose(_ragdollLocalPositions, _ragdollLocalRotations);
        AlignCharacterToRagdoll();
        SetCollidersEnabled(false);
        _modelTransform.SetParent(_modelParent, true);
        _modelTransform.SetLocalPositionAndRotation(_modelLocalPosition, _modelLocalRotation);
        RestoreWorldPose(_ragdollLocalPositions, _ragdollLocalRotations);
        CaptureCurrentPose(_ragdollLocalPositions, _ragdollLocalRotations);
        CaptureAnimatedPose();
        RestorePose(_ragdollLocalPositions, _ragdollLocalRotations);

        float timer = 0f;

        while (timer < _poseBlendDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(timer / _poseBlendDuration));
            BlendToAnimatedPose(progress);
            yield return null;
        }

        RestorePose(_animatedLocalPositions, _animatedLocalRotations);
        DisableRagdoll();
        _animator.enabled = true;
        RestoreRendererUpdateWhenOffscreen();
    }

    public void CancelRagdoll()
    {
        if (!_isRagdollActive)
        {
            return;
        }

        FreezeRagdoll();
        _isCaptured = false;
        AlignCharacterToRagdoll();
        SetCollidersEnabled(false);
        _modelTransform.SetParent(_modelParent, true);
        _modelTransform.SetLocalPositionAndRotation(_modelLocalPosition, _modelLocalRotation);
        DisableRagdoll();
        _animator.enabled = true;
        RestoreRendererUpdateWhenOffscreen();
    }

    private Collider[] GetRagdollColliders()
    {
        Collider[] allColliders = _modelTransform.GetComponentsInChildren<Collider>(true);
        int ragdollColliderCount = 0;

        for (int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i].GetComponent<Rigidbody>() != null)
            {
                ragdollColliderCount++;
            }
        }

        Collider[] ragdollColliders = new Collider[ragdollColliderCount];
        int index = 0;

        for (int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i].GetComponent<Rigidbody>() == null)
            {
                continue;
            }

            ragdollColliders[index] = allColliders[i];
            index++;
        }

        return ragdollColliders;
    }

    private static Rigidbody[] GetCaptureAnchorBodies(Animator animator)
    {
        HumanBodyBones[] anchorBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightUpperArm
        };
        Rigidbody[] anchors = new Rigidbody[anchorBones.Length];
        int anchorsCount = 0;

        for (int i = 0; i < anchorBones.Length; i++)
        {
            Transform bone = animator.GetBoneTransform(anchorBones[i]);

            if (bone == null || !bone.TryGetComponent(out Rigidbody body))
            {
                continue;
            }

            anchors[anchorsCount] = body;
            anchorsCount++;
        }

        if (anchorsCount == anchors.Length)
        {
            return anchors;
        }

        Rigidbody[] result = new Rigidbody[anchorsCount];
        System.Array.Copy(anchors, result, anchorsCount);
        return result;
    }

    private void CaptureAnchorPose()
    {
        Quaternion inverseHipsRotation = Quaternion.Inverse(_hipsBody.rotation);

        for (int i = 0; i < _captureAnchorBodies.Length; i++)
        {
            Rigidbody body = _captureAnchorBodies[i];
            _captureAnchorLocalPositions[i] = inverseHipsRotation *
                                              (body.position - _hipsBody.position);
            _captureAnchorLocalRotations[i] = inverseHipsRotation * body.rotation;
        }
    }

    private void SetCaptureAnchorsKinematic(bool isKinematic)
    {
        for (int i = 0; i < _captureAnchorBodies.Length; i++)
        {
            Rigidbody body = _captureAnchorBodies[i];
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = isKinematic;
        }
    }

    private void EnableRagdoll()
    {
        SetCollidersEnabled(true);
        SetBodiesKinematic(false);
    }

    private void SetBodiesKinematic(bool isKinematic)
    {
        for (int i = 0; i < _bodies.Length; i++)
        {
            if (_bodies[i] == null)
            {
                continue;
            }

            if (!_bodies[i].isKinematic)
            {
                _bodies[i].velocity = Vector3.zero;
                _bodies[i].angularVelocity = Vector3.zero;
            }

            _bodies[i].isKinematic = isKinematic;
        }
    }

    private void FreezeRagdoll()
    {
        for (int i = 0; i < _bodies.Length; i++)
        {
            if (_bodies[i] == null)
            {
                continue;
            }

            if (!_bodies[i].isKinematic)
            {
                _bodies[i].velocity = Vector3.zero;
                _bodies[i].angularVelocity = Vector3.zero;
            }

            _bodies[i].isKinematic = true;
        }
    }

    private void DisableRagdoll()
    {
        _isCaptured = false;
        FreezeRagdoll();
        SetCollidersEnabled(false);
        _isRagdollActive = false;
    }

    private IEnumerator WaitBeforeRecovery()
    {
        float timer = 0f;

        while (timer < _groundedPauseBeforeRecovery)
        {
            timer += Time.deltaTime;
            FollowHips();
            yield return null;
        }
    }

    private void FollowHips()
    {
        _characterTransform.position = _hipsBody.position - GetHipsWorldOffset();
    }

    private bool IsSettledOnGround()
    {
        bool isSlowEnough = _hipsBody.velocity.sqrMagnitude <=
                            _settledVelocity * _settledVelocity;

        return isSlowEnough && TryGetGroundPoint(_hipsBody.position,
            _groundCheckDistance, out _);
    }

    private bool IsTouchingGround()
    {
        bool hasGroundContact = false;
        Vector3 highestGroundPoint = default;

        for (int i = 0; i < _colliders.Length; i++)
        {
            Bounds bounds = _colliders[i].bounds;
            float checkDistance = bounds.extents.y + _groundContactTolerance;

            if (!TryGetGroundPoint(bounds.center, checkDistance,
                    out Vector3 groundPoint))
            {
                continue;
            }

            if (!hasGroundContact || groundPoint.y > highestGroundPoint.y)
            {
                highestGroundPoint = groundPoint;
            }

            hasGroundContact = true;
        }

        if (hasGroundContact)
        {
            _groundContactPoint = highestGroundPoint;
        }

        return hasGroundContact;
    }

    private void AlignCharacterToRagdoll()
    {
        Vector3 hipsPosition = _hipsBody.position;
        Vector3 rootPosition = hipsPosition - GetHipsWorldOffset();

        if (TryGetHighestGroundPointUnderRagdoll(out Vector3 groundPoint))
        {
            rootPosition.y = groundPoint.y;
        }

        _characterTransform.SetPositionAndRotation(rootPosition,
            GetUprightRotation(_characterTransform.rotation));
    }

    private bool TryGetHighestGroundPointUnderRagdoll(out Vector3 groundPoint)
    {
        bool hasGround = false;
        groundPoint = default;

        for (int i = 0; i < _colliders.Length; i++)
        {
            Bounds bounds = _colliders[i].bounds;
            Vector3 origin = bounds.center + Vector3.up * 0.25f;
            float distance = bounds.extents.y + 1.5f;

            if (!TryGetGroundPoint(origin, distance, out Vector3 hitPoint))
            {
                continue;
            }

            if (hasGround && hitPoint.y <= groundPoint.y)
            {
                continue;
            }

            hasGround = true;
            groundPoint = hitPoint;
        }

        return hasGround;
    }

    private Vector3 GetHipsWorldOffset()
    {
        return GetUprightRotation(_characterTransform.rotation) * _hipsCharacterLocalPosition;
    }

    private static Quaternion GetUprightRotation(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private bool TryGetGroundPoint(Vector3 origin, float distance, out Vector3 groundPoint)
    {
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _groundHits, distance,
            _groundLayers, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        groundPoint = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _groundHits[i];

            if (hit.collider == null ||
                hit.collider.transform.IsChildOf(_modelTransform) ||
                hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            groundPoint = hit.point;
        }

        return nearestDistance < float.MaxValue;
    }

    private void IgnoreSelfCollisions()
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            for (int j = i + 1; j < _colliders.Length; j++)
            {
                Physics.IgnoreCollision(_colliders[i], _colliders[j]);
            }
        }
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] == null)
            {
                continue;
            }

            _colliders[i].enabled = isEnabled;
        }
    }

    private void SetRenderersUpdateWhenOffscreen(bool updateWhenOffscreen)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].updateWhenOffscreen = updateWhenOffscreen;
        }
    }

    private void RestoreRendererUpdateWhenOffscreen()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].updateWhenOffscreen = _rendererUpdateWhenOffscreen[i];
        }
    }

    private void CaptureAnimatedPose()
    {
        _animator.enabled = true;
        _animator.Update(Mathf.Max(0f, _animatedPoseSettleTime));
        CaptureCurrentPose(_animatedLocalPositions, _animatedLocalRotations);
        _animator.enabled = false;
    }

    private void CaptureCurrentPose(Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            positions[i] = _bones[i].localPosition;
            rotations[i] = _bones[i].localRotation;
        }
    }

    private void CaptureWorldPose(Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            positions[i] = _bones[i].position;
            rotations[i] = _bones[i].rotation;
        }
    }

    private void RestoreWorldPose(Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].SetPositionAndRotation(positions[i], rotations[i]);
        }
    }

    private void RestorePose(Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
        }
    }

    private void BlendToAnimatedPose(float progress)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].localPosition = Vector3.Lerp(_ragdollLocalPositions[i],
                _animatedLocalPositions[i], progress);
            _bones[i].localRotation = Quaternion.Slerp(_ragdollLocalRotations[i],
                _animatedLocalRotations[i], progress);
        }
    }
}
