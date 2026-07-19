using System;
using Tentacle.States;

namespace Tentacle
{
    public sealed class TentacleStateMachine
    {
        private readonly TentacleStateBase[] _states;
        private readonly Action<TentacleState> _onStateChanged;
        private readonly TentacleContext _context;
        private TentacleStateBase _currentState;

        public TentacleStateMachine(TentacleContext context, Action<TentacleState> onStateChanged)
        {
            _context = context;
            _onStateChanged = onStateChanged;
            _states = new TentacleStateBase[]
            {
                new TentacleIdleState(context, this),
                new TentacleAlertState(context, this),
                new TentacleGrabbingState(context, this),
                new TentacleLiftingState(context, this),
                new TentacleHoldingState(context, this),
                new TentacleThrowingState(context, this),
                new TentacleRecoveringState(context, this),
                new TentacleCooldownState(context, this)
            };
        }

        public void Start()
        {
            ChangeState(TentacleState.Idle, true);
        }

        public void Update()
        {
            _currentState?.Update();
        }

        public void ChangeState(TentacleState state)
        {
            ChangeState(state, false);
        }

        public void Stop()
        {
            _currentState?.Exit();
            _currentState = null;

            _context.AbortCycle();
            _onStateChanged?.Invoke(TentacleState.Idle);
        }

        private void ChangeState(TentacleState state, bool force)
        {
            TentacleStateBase nextState = _states[(int)state];
            if (!force && _currentState == nextState)
            {
                return;
            }

            _currentState?.Exit();
            _currentState = nextState;
            _onStateChanged?.Invoke(state);
            _currentState.Enter();
        }
    }
}
