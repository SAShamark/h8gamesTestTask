using UnityEngine;

namespace Tentacle.States
{
    public sealed class TentacleCooldownState : TentacleStateBase
    {
        private float _timer;

        public TentacleCooldownState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            _timer = Context.CooldownDuration;
            Context.SetAlert(false);
        }

        public override void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                StateMachine.ChangeState(TentacleState.Idle);
            }
        }
    }
}