namespace Tentacle.States
{
    public sealed class TentacleIdleState : TentacleStateBase
    {
        public TentacleIdleState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            Context.SetAlert(false);
        }

        public override void Update()
        {
            if (Context.HasTarget && Context.IsTargetInDetectionRadius(false))
            {
                StateMachine.ChangeState(TentacleState.Alert);
            }
        }
    }
}