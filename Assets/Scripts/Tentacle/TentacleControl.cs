using UnityEngine;

namespace Tentacle
{
    [RequireComponent(typeof(Animator))]
    public class TentacleControl : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _model;
        [SerializeField] private float _detectionRadius = 12f;
        [SerializeField] private float _detectionExitPadding = 1.25f;
        [SerializeField] private float _minimumAlertTimeBeforeGrab = 0.3f;
        [SerializeField] private float _postThrowCooldown = 0.65f;
        [SerializeField] private float _rotationSpeed = 540f;
        [SerializeField] private float _rotationSmoothTime = 0.16f;

        [SerializeField] private TentacleSettings _settings = new();
        [SerializeField] private TentacleState _state;

        private TentacleStateMachine _stateMachine;

        public TentacleState State => _state;

        private void Awake()
        {
            _animator ??= GetComponent<Animator>();

            TentacleContext context = new(transform, _model, _target, _animator, _settings,
                _detectionRadius, _detectionExitPadding, _minimumAlertTimeBeforeGrab,
                _postThrowCooldown, _rotationSpeed, _rotationSmoothTime);
            _stateMachine = new TentacleStateMachine(context, SetCurrentState);
        }

        private void OnEnable()
        {
            _stateMachine?.Start();
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        private void OnDisable()
        {
            _stateMachine?.Stop();
        }

        private void SetCurrentState(TentacleState state)
        {
            _state = state;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        }
    }
}
