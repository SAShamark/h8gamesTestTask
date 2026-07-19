using UnityEngine;

namespace Tentacle.States
{
    public sealed class TentacleLiftingState : TentacleStateBase
    {
        private float _timer;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        public TentacleLiftingState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            _timer = 0f;
            _startPosition = Context.Target.position;
            _startRotation = Context.Target.rotation;
            Context.RootPosition = Context.Settings.Bones[0].position;
            Context.LiftDirection = Context.GetFlatDirection(Context.RootPosition, _startPosition);
            Context.LiftedPosition = Context.RootPosition + Vector3.up *
                Context.Settings.Lift.LiftHeight + Context.LiftDirection *
                Context.Settings.Lift.LiftForwardOffset;
            Context.LiftedRotation = Quaternion.Euler(0f, Context.Origin.eulerAngles.y, 0f);
            Context.Pose.BeginLift(Context.Target, Context.LiftDirection, Context.Settings.Lift);
        }

        public override void Update()
        {
            _timer += Time.deltaTime;
            TentacleLiftSettings settings = Context.Settings.Lift;
            float normalizedTime = Mathf.Clamp01(_timer / Mathf.Max(0.01f,
                settings.LiftDuration));
            float movement = TentaclePoseSolver.SmootherStep(normalizedTime);
            float shape = TentaclePoseSolver.SmootherStep(Mathf.Clamp01(normalizedTime /
                                                                        Mathf.Max(0.01f, settings.LiftWrapTransitionPortion)));
            Vector3 position = Vector3.Lerp(_startPosition, Context.LiftedPosition, movement);
            Quaternion rotation = Quaternion.Slerp(_startRotation, Context.LiftedRotation,
                movement);

            Context.CapturedCharacter.SetCapturedPose(position, rotation);
            Context.Pose.UpdateLift(Context.RootPosition, position, Context.LiftDirection, shape,
                1f, settings);

            if (normalizedTime >= 1f)
            {
                StateMachine.ChangeState(TentacleState.Holding);
            }
        }
    }
}