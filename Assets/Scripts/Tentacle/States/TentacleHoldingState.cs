using UnityEngine;

namespace Tentacle.States
{
    public sealed class TentacleHoldingState : TentacleStateBase
    {
        private float _timer;

        public TentacleHoldingState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            _timer = 0f;
        }

        public override void Update()
        {
            TentacleLiftSettings settings = Context.Settings.Lift;
            _timer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(_timer /
                                                 Mathf.Max(0.01f, settings.TopShakeDuration));
            float engage = TentaclePoseSolver.SmootherStep(Mathf.InverseLerp(0f, 0.16f,
                normalizedTime));
            float settle = 1f - TentaclePoseSolver.SmootherStep(Mathf.InverseLerp(0.72f, 1f,
                normalizedTime));
            float weight = engage * settle;
            float phase = _timer * settings.TopShakeFrequency * Mathf.PI * 2f;
            Vector3 side = Vector3.Cross(Vector3.up, Context.LiftDirection).normalized;
            Vector3 offset = side * (Mathf.Sin(phase) * settings.TopShakeSideAmplitude * weight) +
                             Context.LiftDirection * (Mathf.Sin(phase * 0.7f + 1.35f) *
                                                      settings.TopShakeForwardAmplitude * weight) + Vector3.up *
                             (Mathf.Sin(phase * 1.6f + 0.4f) * settings.TopShakeHeightAmplitude * weight);
            Quaternion rotation = Quaternion.AngleAxis(Mathf.Sin(phase) *
                                                       settings.TopShakeRotationAngle * weight, Context.LiftDirection) *
                                  Quaternion.AngleAxis(Mathf.Sin(phase * 0.7f + 1.35f) *
                                                       settings.TopShakeRotationAngle * 0.6f * weight, side) * Context.LiftedRotation;
            Vector3 position = Context.LiftedPosition + offset;

            Context.CapturedCharacter.SetCapturedPose(position, rotation);
            Context.Pose.UpdateLift(Context.RootPosition, position, rotation,
                Context.LiftDirection, 1f, 1f, settings);

            if (_timer >= settings.TopShakeDuration)
            {
                StateMachine.ChangeState(TentacleState.Throwing);
            }
        }
    }
}
