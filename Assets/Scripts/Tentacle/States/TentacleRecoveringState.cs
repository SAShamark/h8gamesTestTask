using UnityEngine;

namespace Tentacle.States
{
    public sealed class TentacleRecoveringState : TentacleStateBase
    {
        private float _timer;

        public TentacleRecoveringState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            _timer = 0f;
            Context.Pose.BeginRecovery();
        }

        public override void Update()
        {
            _timer += Time.deltaTime;
            float progress = Mathf.Clamp01(_timer /
                                           Mathf.Max(0.01f, Context.Settings.RecoverToIdleDuration));
            Context.Pose.UpdateRecovery(progress);

            if (progress < 1f)
            {
                return;
            }

            Context.Pose.RestoreIdlePose();
            Context.Pose.ClearRuntimePose();
            Context.ReturnToIdleAnimation();
            StateMachine.ChangeState(TentacleState.Cooldown);
        }
    }
}