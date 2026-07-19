using UnityEngine;

namespace Tentacle.States
{
    public sealed class TentacleThrowingState : TentacleStateBase
    {
        private enum ThrowPhase
        {
            Windup,
            Release,
            FollowThrough
        }

        private ThrowPhase _phase;
        private float _timer;
        private Vector3 _side;
        private Vector3 _drawBackPosition;
        private Vector3 _releasePosition;
        private Vector3 _releaseVelocity;
        private Quaternion _releaseRotation;

        public TentacleThrowingState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            TentacleLiftSettings settings = Context.Settings.Lift;
            _phase = ThrowPhase.Windup;
            _timer = 0f;
            _side = Vector3.Cross(Vector3.up, Context.LiftDirection).normalized;
            _drawBackPosition = Context.LiftedPosition - Context.LiftDirection *
                settings.ThrowDrawBackDistance + Vector3.down * 0.2f;
            _releasePosition = Context.LiftedPosition + Context.LiftDirection *
                settings.ThrowExtensionDistance;
            _releaseVelocity = Context.LiftDirection * settings.ThrowForwardSpeed +
                               Vector3.up * settings.ThrowUpSpeed;
            _releaseRotation = Quaternion.AngleAxis(12f, _side) *
                               Quaternion.LookRotation(Context.LiftDirection, Vector3.up);
        }

        public override void Update()
        {
            switch (_phase)
            {
                case ThrowPhase.Windup:
                    UpdateWindup();
                    break;
                case ThrowPhase.Release:
                    UpdateRelease();
                    break;
                case ThrowPhase.FollowThrough:
                    UpdateFollowThrough();
                    break;
            }
        }

        private void UpdateWindup()
        {
            TentacleLiftSettings settings = Context.Settings.Lift;
            _timer += Time.deltaTime;
            float progress = Mathf.Clamp01(_timer / Mathf.Max(0.01f,
                settings.ThrowWindupDuration));
            float smooth = TentaclePoseSolver.SmootherStep(progress);
            Vector3 position = Vector3.Lerp(Context.LiftedPosition, _drawBackPosition, smooth);
            Quaternion rotation = Quaternion.AngleAxis(-28f * smooth, _side) *
                                  Context.LiftedRotation;
            Context.CapturedCharacter.SetCapturedPose(position, rotation);
            Context.Pose.UpdateLift(Context.RootPosition, position, rotation,
                Context.LiftDirection, 1f, 1f, settings);

            if (progress >= 1f)
            {
                _timer = 0f;
                _phase = ThrowPhase.Release;
            }
        }

        private void UpdateRelease()
        {
            TentacleLiftSettings settings = Context.Settings.Lift;
            _timer += Time.deltaTime;
            float duration = Mathf.Max(0.01f, settings.ThrowReleaseDuration);
            float progress = Mathf.Clamp01(_timer / duration);
            float unwrap = Mathf.InverseLerp(settings.ThrowUnwrapStart, 1f, progress);
            float wrapBlend = 1f - TentaclePoseSolver.SmootherStep(unwrap);
            Vector3 control = _releasePosition - _releaseVelocity * (duration / 3f);
            Vector3 position = TentaclePoseSolver.EvaluateBezier(_drawBackPosition,
                _drawBackPosition, control, _releasePosition, progress);
            Quaternion rotation = Quaternion.Slerp(Quaternion.AngleAxis(-28f, _side) *
                                                   Context.LiftedRotation, _releaseRotation,
                TentaclePoseSolver.SmootherStep(progress));
            Context.CapturedCharacter.SetCapturedPose(position, rotation);
            Context.Pose.UpdateLift(Context.RootPosition, position, rotation,
                Context.LiftDirection, 1f, wrapBlend, settings);

            if (progress < 1f)
            {
                return;
            }

            Context.CapturedCharacter.Throw(_releaseVelocity, Vector3.zero);
            Context.CapturedCharacter = null;
            _timer = 0f;
            _phase = ThrowPhase.FollowThrough;
        }

        private void UpdateFollowThrough()
        {
            TentacleLiftSettings settings = Context.Settings.Lift;
            _timer += Time.deltaTime;
            float progress = TentaclePoseSolver.SmootherStep(Mathf.Clamp01(_timer /
                                                                           Mathf.Max(0.01f, settings.ThrowFollowThroughDuration)));
            Vector3 target = _releasePosition + Context.LiftDirection *
                settings.ThrowFollowThroughDistance - Vector3.up * 0.35f;
            Vector3 position = Vector3.Lerp(_releasePosition, target, progress);
            Context.Pose.UpdateLift(Context.RootPosition, position, _releaseRotation,
                Context.LiftDirection, 1f, 0f, settings);

            if (progress >= 1f)
            {
                StateMachine.ChangeState(TentacleState.Recovering);
            }
        }
    }
}
