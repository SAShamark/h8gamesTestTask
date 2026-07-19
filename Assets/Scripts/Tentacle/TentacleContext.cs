using UnityEngine;

namespace Tentacle
{
    public sealed class TentacleContext
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");

        private readonly Transform _model;
        private readonly float _detectionRadius;
        private readonly float _detectionExitPadding;
        private readonly float _rotationSpeed;
        private readonly float _rotationSmoothTime;
        private float _rotationVelocity;

        public TentacleContext(Transform origin, Transform model, Transform target,
            Animator animator, TentacleSettings settings, float detectionRadius,
            float detectionExitPadding, float minimumAlertTime, float cooldownDuration,
            float rotationSpeed, float rotationSmoothTime)
        {
            Origin = origin;
            _model = model;
            Target = target;
            Animator = animator;
            Settings = settings;
            Pose = new TentaclePoseSolver(settings.Bones);
            _detectionRadius = detectionRadius;
            _detectionExitPadding = detectionExitPadding;
            MinimumAlertTime = minimumAlertTime;
            CooldownDuration = cooldownDuration;
            _rotationSpeed = rotationSpeed;
            _rotationSmoothTime = rotationSmoothTime;
        }

        public Transform Origin { get; }
        public Transform Target { get; }
        public Animator Animator { get; }
        public TentacleSettings Settings { get; }
        public TentaclePoseSolver Pose { get; }
        public float MinimumAlertTime { get; }
        public float CooldownDuration { get; }
        public ICapturableCharacter CapturedCharacter { get; set; }
        public Vector3 RootPosition { get; set; }
        public Vector3 LiftDirection { get; set; }
        public Vector3 LiftedPosition { get; set; }
        public Quaternion LiftedRotation { get; set; }

        public bool HasTarget => Target != null;

        public bool IsTargetInDetectionRadius(bool useExitPadding)
        {
            float radius = useExitPadding
                ? _detectionRadius + Mathf.Max(0f, _detectionExitPadding)
                : _detectionRadius;
            return GetFlatSqrDistance(Target.position) <= radius * radius;
        }

        public bool IsTargetInGrabRadius()
        {
            return GetFlatSqrDistance(Target.position) <= Settings.GrabRadius * Settings.GrabRadius;
        }

        public void RotateTowardsTarget()
        {
            Vector3 direction = Target.position - Origin.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float targetYaw = Quaternion.LookRotation(direction).eulerAngles.y;
            float yaw = Mathf.SmoothDampAngle(Origin.eulerAngles.y, targetYaw,
                ref _rotationVelocity, Mathf.Max(0.01f, _rotationSmoothTime), _rotationSpeed,
                Time.deltaTime);
            _model.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void SetAlert(bool isAlert)
        {
            Animator.SetBool(IsAlertHash, isAlert);
        }

        public void PlayAttack()
        {
            Animator.enabled = true;
            Animator.ResetTrigger(AttackHash);

            if (!string.IsNullOrEmpty(Settings.AttackStateName))
            {
                Animator.CrossFadeInFixedTime(Settings.AttackStateName,
                    Settings.AttackCrossFadeDuration);
                return;
            }

            Animator.SetTrigger(AttackHash);
        }

        public void ReturnToIdleAnimation()
        {
            Animator.enabled = true;
            Animator.ResetTrigger(AttackHash);
            Animator.SetBool(IsAlertHash, false);

            if (Animator.runtimeAnimatorController != null &&
                !string.IsNullOrEmpty(Settings.IdleStateName))
            {
                Animator.CrossFadeInFixedTime(Settings.IdleStateName,
                    Settings.RecoverToIdleDuration, 0, 0f);
            }
        }

        public bool TryCaptureCharacter()
        {
            ICapturableCharacter character = Target.GetComponentInParent<ICapturableCharacter>();
            if (character == null || !character.TryBeginCapture())
            {
                return false;
            }

            CapturedCharacter = character;
            character.SetCapturedPose(Target.position,
                Quaternion.Euler(0f, Target.eulerAngles.y, 0f));
            return true;
        }

        public void AbortCycle()
        {
            CapturedCharacter?.CancelCapture();
            CapturedCharacter = null;
            Pose.RestoreIdlePose();
            Pose.ClearRuntimePose();
            ReturnToIdleAnimation();
        }

        public Vector3 GetFlatDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Origin.forward;
                direction.y = 0f;
            }

            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }

        private float GetFlatSqrDistance(Vector3 position)
        {
            Vector3 offset = position - Origin.position;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }
    }
}
