using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Areas;
using Game.Entities.Spawners;
using Game.Entities.Units;
using Game.Entities.Units.Character;
using Game.Entities.Units.Slots;
using UI.Managers;
using UI.Screens;
using UI.Screens.Variants.Gameplay;
using UnityEngine;

namespace Game
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private CharacterControl _characterControl;
        [SerializeField] private CameraControl _cameraControl;
        [SerializeField] private SpawnersManager _spawnersManager;
        [SerializeField] private UnitSlots _unitSlots;
        [SerializeField] private FlagCaptureArea _flagCaptureArea;
        [SerializeField] private List<BaseArea> _areas;

        private UnitsManager _unitsManager;
        private UIManager _uiManager;
        private GameplayScreen _gameplayScreen;
        private AreasController _areasController;
        private GameplayController _gameplayController;

        private void Start()
        {
            InitializeWorldSystems();
            InitializeGameplayScreen();
            InitializeCharacter();
            InitializeAreas();
            InitializeGameplay();
        }


        private void InitializeWorldSystems()
        {
            _unitsManager = new UnitsManager(_spawnersManager, _characterControl);

            _spawnersManager.Init(_unitSlots, _levelConfig.Levels, _levelConfig.EnemiesPerGroup, _characterControl);
            _unitsManager.Init();
            _cameraControl.Init(_characterControl.transform, _characterControl.MovementLogic);
        }

        private void InitializeGameplayScreen()
        {
            _uiManager = UIManager.Instance;
            _uiManager.Init();
            _uiManager.ScreensManager.ShowScreen(ScreenTypes.Gameplay);
            _gameplayScreen = _uiManager.ScreensManager.GetScreen(ScreenTypes.Gameplay) as GameplayScreen;
        }

        private void InitializeCharacter()
        {
            _characterControl.Init(_gameplayScreen.Joystick, _cameraControl);
            _gameplayScreen.BindInventory(_characterControl.Inventory);
        }

        private void InitializeAreas()
        {
            _areasController = new AreasController(_areas, _gameplayScreen, _unitsManager,
                _spawnersManager, _unitSlots, _characterControl.ShootingLogic);
            _areasController.Init();
        }

        private void InitializeGameplay()
        {
            _gameplayController = new GameplayController(_characterControl, _unitsManager, _flagCaptureArea);
            _gameplayController.Init();
        }

        private void OnDestroy()
        {
            _gameplayController.Dispose();
            _areasController.Dispose();
            _unitsManager.Dispose();
            _spawnersManager.Dispose();
        }
    }
}
