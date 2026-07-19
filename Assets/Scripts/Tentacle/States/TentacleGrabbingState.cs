using UnityEngine;

namespace Tentacle.States
{
    public sealed class TentacleGrabbingState : TentacleStateBase
    {
        private float _attackTimer;
        private float _grabTimer;
        private bool _isReaching;

        public TentacleGrabbingState(TentacleContext context, TentacleStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            _attackTimer = 0f;
            _grabTimer = 0f;
            _isReaching = false;
            Context.Pose.CaptureIdlePose();
            Context.PlayAttack();
        }

        public override void Update()
        {
            if (!Context.HasTarget || !Context.IsTargetInGrabRadius())
            {
                AbortToCooldown();
                return;
            }

            Context.RotateTowardsTarget();

            if (!_isReaching)
            {
                UpdateAttackWindup();
                return;
            }

            UpdateGrabMovement();
        }

        private void UpdateAttackWindup()
        {
            _attackTimer += Time.deltaTime;
            AnimatorStateInfo stateInfo = Context.Animator.IsInTransition(0)
                ? Context.Animator.GetNextAnimatorStateInfo(0)
                : Context.Animator.GetCurrentAnimatorStateInfo(0);
            bool reachedPose = stateInfo.IsName(Context.Settings.AttackStateName) &&
                               stateInfo.normalizedTime >=
                               Context.Settings.AttackTakeoverNormalizedTime;
            bool minimumTimePassed = _attackTimer >= Context.Settings.AttackWindupDuration;
            bool timedOut = _attackTimer >= Context.Settings.AttackTakeoverTimeout;

            if (!timedOut && (!minimumTimePassed || !reachedPose))
            {
                return;
            }

            Context.Animator.Update(0f);
            Context.Animator.enabled = false;
            Context.Pose.BeginGrab();
            _isReaching = true;
        }

        private void UpdateGrabMovement()
        {
            _grabTimer += Time.deltaTime;
            float duration = Mathf.Max(0.01f, Context.Settings.GrabDuration);
            float progress = Mathf.Clamp01(_grabTimer / duration);
            Context.Pose.UpdateGrab(Context.Target, progress, Context.Settings);

            if (progress < 1f)
            {
                return;
            }

            if (!Context.TryCaptureCharacter())
            {
                StateMachine.ChangeState(TentacleState.Recovering);
                return;
            }

            StateMachine.ChangeState(TentacleState.Lifting);
        }
    }
}