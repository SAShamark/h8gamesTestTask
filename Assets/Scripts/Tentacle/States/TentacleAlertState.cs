using UnityEngine;

namespace Tentacle.States
{
    public sealed class TentacleAlertState : TentacleStateBase
    {
        private float _timer;

        public TentacleAlertState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            _timer = 0f;
            Context.SetAlert(true);
        }

        public override void Update()
        {
            if (!Context.HasTarget || !Context.IsTargetInDetectionRadius(true))
            {
                StateMachine.ChangeState(TentacleState.Idle);
                return;
            }

            _timer += Time.deltaTime;
            Context.RotateTowardsTarget();

            if (_timer >= Context.MinimumAlertTime && Context.IsTargetInGrabRadius() &&
                Context.Pose.HasValidBones())
            {
                StateMachine.ChangeState(TentacleState.Grabbing);
            }
        }
    }
}