using Game.Entities;
using Game.Entities.Character;
using UI.Managers;
using UI.Screens;
using UI.Screens.Variants.Gameplay;
using UnityEngine;

namespace Game
{
    public class GameplayManager: MonoBehaviour
    {
        [SerializeField] private CharacterControl _characterControl;
        [SerializeField] private CameraControl _cameraControl;

        private UIManager _uiManager;
        private GameplayScreen _gameplayScreen;

        private void Start()
        {
            _cameraControl.Init(_characterControl.transform, _characterControl.MovementControl);
            _uiManager = UIManager.Instance;
            _uiManager.Init();
            _uiManager.ScreensManager.ShowScreen(ScreenTypes.Gameplay);
            _gameplayScreen = _uiManager.ScreensManager.GetScreen(ScreenTypes.Gameplay) as GameplayScreen;
            _characterControl.Init(_gameplayScreen.Joystick);
        }
    }
}
