using UI.Screens.Base;
using UnityEngine;

namespace UI.Screens.Variants.Gameplay
{
    public class GameplayScreen : BaseScreen
    {
        [SerializeField] private FloatingJoystick _floatingJoystick;
        public FloatingJoystick Joystick => _floatingJoystick;
    }
}
