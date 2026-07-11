using UnityEngine;

namespace Game.Entities.Character
{
    public class CharacterControl : MonoBehaviour
    {
        [SerializeField] private MovementControl _movementControl;
        [SerializeField] private CharacterAnimationControl _animationControl;

        private FloatingJoystick _joystick;
        public MovementControl MovementControl => _movementControl;

        private void Awake()
        {
            _movementControl.Init(transform);
            _animationControl.Init(_movementControl);
            enabled = false;
        }

        public void Init(FloatingJoystick joystick)
        {
            _joystick = joystick;
            enabled = true;
        }

        private void Update()
        {
            _movementControl.Move(_joystick.Direction, Time.deltaTime);
            _animationControl.Tick(Time.deltaTime);
        }
    }
}
