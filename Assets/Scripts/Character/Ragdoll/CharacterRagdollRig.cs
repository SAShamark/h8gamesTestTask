using UnityEngine;

namespace Character.Ragdoll
{
    internal sealed class CharacterRagdollRig
    {
        private readonly float[] _bodyDrag;
        private readonly float[] _bodyAngularDrag;
        private readonly float[] _bodyMaximumAngularVelocity;
        private readonly SkinnedMeshRenderer[] _renderers;
        private readonly bool[] _rendererUpdateWhenOffscreen;
        private readonly Vector3 _hipsCharacterLocalPosition;
        private readonly Quaternion _hipsCharacterLocalRotation;

        public CharacterRagdollRig(Transform characterTransform, Animator animator)
        {
            CharacterTransform = characterTransform;
            Animator = animator;
            ModelTransform = animator.transform;
            ModelParent = ModelTransform.parent;
            ModelLocalPosition = ModelTransform.localPosition;
            ModelLocalRotation = ModelTransform.localRotation;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            HipsBody = hips.GetComponent<Rigidbody>();
            Bodies = ModelTransform.GetComponentsInChildren<Rigidbody>(true);
            Bones = new Transform[Bodies.Length];
            _bodyDrag = new float[Bodies.Length];
            _bodyAngularDrag = new float[Bodies.Length];
            _bodyMaximumAngularVelocity = new float[Bodies.Length];

            for (int i = 0; i < Bodies.Length; i++)
            {
                Rigidbody body = Bodies[i];
                Bones[i] = body.transform;
                _bodyDrag[i] = body.drag;
                _bodyAngularDrag[i] = body.angularDrag;
                _bodyMaximumAngularVelocity[i] = body.maxAngularVelocity;
            }

            Colliders = GetRagdollColliders();
            _renderers = ModelTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _rendererUpdateWhenOffscreen = new bool[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                _rendererUpdateWhenOffscreen[i] = _renderers[i].updateWhenOffscreen;
            }

            _hipsCharacterLocalPosition = CharacterTransform.InverseTransformPoint(hips.position);
            _hipsCharacterLocalRotation = Quaternion.Inverse(CharacterTransform.rotation) *
                                          hips.rotation;
            IgnoreSelfCollisions();
            Disable();
        }

        public Transform CharacterTransform { get; }
        public Transform ModelTransform { get; }
        public Transform ModelParent { get; }
        public Vector3 ModelLocalPosition { get; }
        public Quaternion ModelLocalRotation { get; }
        public Animator Animator { get; }
        public Rigidbody HipsBody { get; }
        public Rigidbody[] Bodies { get; }
        public Collider[] Colliders { get; }
        public Transform[] Bones { get; }
        public bool IsActive { get; private set; }

        public void Activate()
        {
            Vector3 hipsPosition = HipsBody.position;
            Quaternion hipsRotation = HipsBody.rotation;

            Animator.enabled = false;
            SetRenderersUpdateWhenOffscreen(true);
            ModelTransform.SetParent(null, true);
            SetCollidersEnabled(true);
            SetBodiesKinematic(false);
            HipsBody.position = hipsPosition;
            HipsBody.rotation = hipsRotation;
            IsActive = true;
        }

        public void Disable()
        {
            RestoreBodyDynamics();
            Freeze();
            SetCollidersEnabled(false);
            IsActive = false;
        }

        public void ApplyCaptureStability(float drag, float angularDrag,
            float maximumAngularVelocity)
        {
            for (int i = 0; i < Bodies.Length; i++)
            {
                Rigidbody body = Bodies[i];
                body.drag = Mathf.Max(_bodyDrag[i], drag);
                body.angularDrag = Mathf.Max(_bodyAngularDrag[i], angularDrag);
                body.maxAngularVelocity = Mathf.Min(_bodyMaximumAngularVelocity[i],
                    maximumAngularVelocity);
            }
        }

        public void RestoreBodyDynamics()
        {
            for (int i = 0; i < Bodies.Length; i++)
            {
                Rigidbody body = Bodies[i];
                body.drag = _bodyDrag[i];
                body.angularDrag = _bodyAngularDrag[i];
                body.maxAngularVelocity = _bodyMaximumAngularVelocity[i];
            }
        }

        public void SetHipsKinematic(bool isKinematic)
        {
            StopBody(HipsBody);
            HipsBody.isKinematic = isKinematic;
        }

        public void SetBodiesKinematic(bool isKinematic)
        {
            for (int i = 0; i < Bodies.Length; i++)
            {
                StopBody(Bodies[i]);
                Bodies[i].isKinematic = isKinematic;
            }
        }

        public void Freeze()
        {
            SetBodiesKinematic(true);
        }

        public void FollowHips()
        {
            CharacterTransform.position = HipsBody.position - GetHipsWorldOffset();
        }

        public void AlignCharacterToRagdoll(Vector3? groundPoint = null)
        {
            Vector3 rootPosition = HipsBody.position - GetHipsWorldOffset();

            if (groundPoint.HasValue)
            {
                rootPosition.y = groundPoint.Value.y;
            }

            CharacterTransform.SetPositionAndRotation(rootPosition,
                GetUprightRotation(CharacterTransform.rotation));
        }

        public Vector3 GetCapturedHipsPosition(Vector3 position, Quaternion rotation)
        {
            return position + rotation * _hipsCharacterLocalPosition;
        }

        public Quaternion GetCapturedHipsRotation(Quaternion rotation)
        {
            return rotation * _hipsCharacterLocalRotation;
        }

        public void SetCollidersEnabled(bool isEnabled)
        {
            for (int i = 0; i < Colliders.Length; i++)
            {
                Colliders[i].enabled = isEnabled;
            }
        }

        public void RestoreModelParent()
        {
            ModelTransform.SetParent(ModelParent, true);
            ModelTransform.SetLocalPositionAndRotation(ModelLocalPosition,
                ModelLocalRotation);
        }

        public void RestoreRendererSettings()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].updateWhenOffscreen = _rendererUpdateWhenOffscreen[i];
            }
        }

        private Collider[] GetRagdollColliders()
        {
            Collider[] allColliders = ModelTransform.GetComponentsInChildren<Collider>(true);
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

                ragdollColliders[index++] = allColliders[i];
            }

            return ragdollColliders;
        }

        private void IgnoreSelfCollisions()
        {
            for (int i = 0; i < Colliders.Length; i++)
            {
                for (int j = i + 1; j < Colliders.Length; j++)
                {
                    Physics.IgnoreCollision(Colliders[i], Colliders[j]);
                }
            }
        }

        private void SetRenderersUpdateWhenOffscreen(bool updateWhenOffscreen)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].updateWhenOffscreen = updateWhenOffscreen;
            }
        }

        private Vector3 GetHipsWorldOffset()
        {
            return GetUprightRotation(CharacterTransform.rotation) *
                   _hipsCharacterLocalPosition;
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

        private static void StopBody(Rigidbody body)
        {
            if (body.isKinematic)
            {
                return;
            }

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
