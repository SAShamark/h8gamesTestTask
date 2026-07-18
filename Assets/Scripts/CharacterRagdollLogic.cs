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
    [SerializeField] private LayerMask _groundLayers = ~0;

    [Header("Recovery")]
    [SerializeField] private float _poseBlendDuration = 0.35f;

    private Transform _characterTransform;
    private Transform _modelTransform;
    private Transform _modelParent;
    private Animator _animator;
    private Rigidbody _hipsBody;
    private Rigidbody[] _bodies;
    private Collider[] _colliders;
    private Transform[] _bones;
    private Vector3[] _ragdollLocalPositions;
    private Quaternion[] _ragdollLocalRotations;
    private Vector3[] _animatedLocalPositions;
    private Quaternion[] _animatedLocalRotations;
    private Vector3 _modelLocalPosition;
    private Quaternion _modelLocalRotation;
    private Vector3 _hipsCharacterLocalPosition;
    private Quaternion _hipsCharacterLocalRotation;
    private bool _isRagdollActive;
    private readonly RaycastHit[] _groundHits = new RaycastHit[GroundHitCapacity];

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
        _colliders = GetRagdollColliders();
        _bones = new Transform[_bodies.Length];
        _ragdollLocalPositions = new Vector3[_bodies.Length];
        _ragdollLocalRotations = new Quaternion[_bodies.Length];
        _animatedLocalPositions = new Vector3[_bodies.Length];
        _animatedLocalRotations = new Quaternion[_bodies.Length];

        for (int i = 0; i < _bodies.Length; i++)
        {
            _bones[i] = _bodies[i].transform;
        }

        _hipsCharacterLocalPosition = _characterTransform.InverseTransformPoint(hips.position);
        _hipsCharacterLocalRotation = Quaternion.Inverse(_characterTransform.rotation) * hips.rotation;
        IgnoreSelfCollisions();
        DisableRagdoll();
    }

    public void BeginThrow()
    {
        Vector3 hipsPosition = _hipsBody.position;
        Quaternion hipsRotation = _hipsBody.rotation;

        _animator.enabled = false;
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

    public IEnumerator Recover()
    {
        FreezeRagdoll();
        SetCollidersEnabled(false);
        AlignCharacterToRagdoll();
        _modelTransform.SetParent(_modelParent, true);

        CaptureCurrentPose(_ragdollLocalPositions, _ragdollLocalRotations);
        _modelTransform.SetLocalPositionAndRotation(_modelLocalPosition, _modelLocalRotation);
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
    }

    public void CancelRagdoll()
    {
        if (!_isRagdollActive)
        {
            return;
        }

        FreezeRagdoll();
        AlignCharacterToRagdoll();
        _modelTransform.SetParent(_modelParent, true);
        _modelTransform.SetLocalPositionAndRotation(_modelLocalPosition, _modelLocalRotation);
        DisableRagdoll();
        _animator.enabled = true;
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
        FreezeRagdoll();
        SetCollidersEnabled(false);
        _isRagdollActive = false;
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

    private void AlignCharacterToRagdoll()
    {
        Vector3 hipsPosition = _hipsBody.position;
        Vector3 rootPosition = hipsPosition - GetHipsWorldOffset();
        if (TryGetGroundPoint(hipsPosition + Vector3.up, 4f, out Vector3 groundPoint))
        {
            rootPosition.y = groundPoint.y;
        }

        _characterTransform.SetPositionAndRotation(rootPosition,
            GetUprightRotation(_characterTransform.rotation));
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

            if (hit.collider.transform.IsChildOf(_modelTransform) ||
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

    private void CaptureAnimatedPose()
    {
        _animator.enabled = true;
        _animator.Update(0f);
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
