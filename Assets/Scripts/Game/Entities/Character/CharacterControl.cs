using System;
using UnityEngine;

namespace Game.Entities.Character
{
    public class CharacterControl : MonoBehaviour, IInventoryOwner
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private Health _health;
        [SerializeField] private MovementControl _movementControl;
        [SerializeField] private CharacterAnimationControl _animationControl;

        private FloatingJoystick _joystick;

        public Inventory Inventory => _inventory;
        public Health Health => _health;
        public MovementControl MovementControl => _movementControl;

        public void Init(FloatingJoystick joystick)
        {
            _joystick = joystick;
            _health.Init();
            _inventory.Init();
            _movementControl.Init(transform);
            _animationControl.Init(_movementControl);

            Subscribes();
        }

        private void Update()
        {
            _movementControl.Move(_joystick.Direction, Time.deltaTime);
            _animationControl.Tick(Time.deltaTime);
            _inventory.Tick(transform.position);
        }

        private void OnDestroy()
        {
            Unsubscribes();
            _health.Dispose();
        }


        private void Subscribes()
        {
        }

        private void Unsubscribes()
        {
        }
    }
}
