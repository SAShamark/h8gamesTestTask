using System;
using UnityEngine;

namespace Cameras
{
    [Serializable]
    public class CameraCollisionLogic
    {

        [SerializeField] private LayerMask _collisionLayers = ~0;
        [SerializeField] private float _collisionRadius = 0.28f;
        [SerializeField] private float _collisionPadding = 0.12f;

        private readonly RaycastHit[] _hits = new RaycastHit[HIT_CAPACITY];
        private Transform _target;
        private Transform _extraIgnoreRoot;
        private const int HIT_CAPACITY = 16;

        public void Initialize(Transform target)
        {
            _target = target;
        }

        public void SetExtraIgnoreRoot(Transform ignoreRoot)
        {
            _extraIgnoreRoot = ignoreRoot;
        }

        public Vector3 GetCameraPosition(Vector3 focusPoint, Vector3 desiredPosition)
        {
            Vector3 direction = desiredPosition - focusPoint;
            float desiredDistance = direction.magnitude;

            if (desiredDistance <= 0.001f)
            {
                return desiredPosition;
            }

            direction /= desiredDistance;
            int hitsCount = Physics.SphereCastNonAlloc(focusPoint, _collisionRadius,
                direction, _hits, desiredDistance, _collisionLayers,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = desiredDistance;

            for (int i = 0; i < hitsCount; i++)
            {
                RaycastHit hit = _hits[i];

                if (ShouldIgnore(hit.collider) || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
            }

            if (nearestDistance >= desiredDistance)
            {
                return desiredPosition;
            }

            float distance = Mathf.Max(0f, nearestDistance - _collisionPadding);
            return focusPoint + direction * distance;
        }

        private bool ShouldIgnore(Collider hitCollider)
        {
            Transform hitTransform = hitCollider.transform;
            return hitTransform == _target || hitTransform.IsChildOf(_target) ||
                   _extraIgnoreRoot != null && hitTransform.IsChildOf(_extraIgnoreRoot);
        }
    }
}