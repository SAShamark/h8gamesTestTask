using System.Collections;
using UnityEngine;

namespace Character.Ragdoll
{
    internal sealed class CharacterRagdollPhysics
    {
        private const int GROUND_HIT_CAPACITY = 16;

        private readonly CharacterRagdollRig _rig;
        private readonly float _minimumThrowTime;
        private readonly float _maximumThrowTime;
        private readonly float _settledVelocity;
        private readonly float _maximumThrowVelocity;
        private readonly float _groundCheckDistance;
        private readonly float _groundContactTolerance;
        private readonly LayerMask _groundLayers;
        private readonly float _captureDrag;
        private readonly float _captureAngularDrag;
        private readonly float _captureMaximumAngularVelocity;
        private readonly RaycastHit[] _groundHits = new RaycastHit[GROUND_HIT_CAPACITY];

        private Vector3 _capturedHipsPosition;
        private Quaternion _capturedHipsRotation;
        private bool _isCaptured;

        public CharacterRagdollPhysics(CharacterRagdollRig rig,
            float minimumThrowTime, float maximumThrowTime, float settledVelocity,
            float maximumThrowVelocity, float groundCheckDistance,
            float groundContactTolerance, LayerMask groundLayers, float captureDrag,
            float captureAngularDrag, float captureMaximumAngularVelocity)
        {
            _rig = rig;
            _minimumThrowTime = minimumThrowTime;
            _maximumThrowTime = maximumThrowTime;
            _settledVelocity = settledVelocity;
            _maximumThrowVelocity = maximumThrowVelocity;
            _groundCheckDistance = groundCheckDistance;
            _groundContactTolerance = groundContactTolerance;
            _groundLayers = groundLayers;
            _captureDrag = captureDrag;
            _captureAngularDrag = captureAngularDrag;
            _captureMaximumAngularVelocity = captureMaximumAngularVelocity;
        }

        public Vector3 GroundContactPoint { get; private set; }

        public void BeginCapture()
        {
            _rig.Activate();
            _rig.ApplyCaptureStability(_captureDrag, _captureAngularDrag,
                _captureMaximumAngularVelocity);
            _rig.SetHipsKinematic(true);
            _capturedHipsPosition = _rig.HipsBody.position;
            _capturedHipsRotation = _rig.HipsBody.rotation;
            _isCaptured = true;
        }

        public void SetCapturedPose(Vector3 position, Quaternion rotation)
        {
            if (!_rig.IsActive)
            {
                return;
            }

            _capturedHipsPosition = _rig.GetCapturedHipsPosition(position, rotation);
            _capturedHipsRotation = _rig.GetCapturedHipsRotation(rotation);
        }

        public void UpdateCapturedPose()
        {
            if (!_isCaptured)
            {
                return;
            }

            _rig.HipsBody.MovePosition(_capturedHipsPosition);
            _rig.HipsBody.MoveRotation(_capturedHipsRotation);
        }

        public void BeginThrow()
        {
            _isCaptured = false;

            if (!_rig.IsActive)
            {
                _rig.Activate();
            }

            _rig.RestoreBodyDynamics();
            _rig.SetBodiesKinematic(false);
        }

        public void Throw(Vector3 velocity)
        {
            velocity = Vector3.ClampMagnitude(velocity, _maximumThrowVelocity);
            _rig.SetBodiesKinematic(false);

            for (int i = 0; i < _rig.Bodies.Length; i++)
            {
                Rigidbody body = _rig.Bodies[i];
                body.velocity = velocity;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
        }

        public IEnumerator WaitForLanding()
        {
            float timer = 0f;

            while (timer < _maximumThrowTime)
            {
                timer += Time.deltaTime;
                _rig.FollowHips();

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
                _rig.FollowHips();

                if (IsTouchingGround())
                {
                    yield break;
                }

                yield return null;
            }
        }

        public void CancelCapture()
        {
            _isCaptured = false;
        }

        public bool TryGetHighestGroundPointUnderRagdoll(out Vector3 groundPoint)
        {
            bool hasGround = false;
            groundPoint = default;

            for (int i = 0; i < _rig.Colliders.Length; i++)
            {
                Bounds bounds = _rig.Colliders[i].bounds;
                Vector3 origin = bounds.center + Vector3.up * 0.25f;
                float distance = bounds.extents.y + 1.5f;

                if (!TryGetGroundPoint(origin, distance, out Vector3 hitPoint) ||
                    hasGround && hitPoint.y <= groundPoint.y)
                {
                    continue;
                }

                hasGround = true;
                groundPoint = hitPoint;
            }

            return hasGround;
        }

        private bool IsSettledOnGround()
        {
            bool isSlowEnough = _rig.HipsBody.velocity.sqrMagnitude <=
                                _settledVelocity * _settledVelocity;
            return isSlowEnough && TryGetGroundPoint(_rig.HipsBody.position,
                _groundCheckDistance, out _);
        }

        private bool IsTouchingGround()
        {
            bool hasGroundContact = false;
            Vector3 highestGroundPoint = default;

            for (int i = 0; i < _rig.Colliders.Length; i++)
            {
                Bounds bounds = _rig.Colliders[i].bounds;
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
                GroundContactPoint = highestGroundPoint;
            }

            return hasGroundContact;
        }

        private bool TryGetGroundPoint(Vector3 origin, float distance,
            out Vector3 groundPoint)
        {
            int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down,
                _groundHits, distance, _groundLayers, QueryTriggerInteraction.Ignore);
            float nearestDistance = float.MaxValue;
            groundPoint = default;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _groundHits[i];

                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(_rig.ModelTransform) ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                groundPoint = hit.point;
            }

            return nearestDistance < float.MaxValue;
        }
    }
}
