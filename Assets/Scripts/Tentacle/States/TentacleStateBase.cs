namespace Tentacle.States
{
    public abstract class TentacleStateBase
    {
        protected readonly TentacleStateMachine StateMachine;

        protected TentacleStateBase(TentacleContext context, TentacleStateMachine stateMachine)
        {
            Context = context;
            StateMachine = stateMachine;
        }

        public TentacleContext Context { get; }

        public virtual void Enter()
        {
        }

        public abstract void Update();

        public virtual void Exit()
        {
        }

        protected void AbortToCooldown()
        {
            Context.AbortCycle();
            StateMachine.ChangeState(TentacleState.Cooldown);
        }
    }
}
