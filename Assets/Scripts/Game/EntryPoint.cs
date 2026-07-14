using System;
using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Areas;
using Game.Entities.Character;
using Game.Entities.Spawners;
using Game.Entities.Units;
using UI.Managers;
using UI.Popups;
using UI.Popups.Variables;
using UI.Screens;
using UI.Screens.Variants.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private LevelsConfig _levelsConfig;
        [SerializeField] private UnitSlots _unitSlots;
        [SerializeField] private CharacterControl _characterControl;
        [SerializeField] private CameraControl _cameraControl;
        [SerializeField] private FlagCaptureArea _flagCaptureArea;
        [SerializeField] private SpawnersManager _spawnersManager;
        [SerializeField] private List<BaseArea> _areas;

        private UnitsManager _unitsManager;
        private UIManager _uiManager;
        private GameplayScreen _gameplayScreen;
        private Vector3 _characterStartPosition;
        private Quaternion _characterStartRotation;

        private void Start()
        {
            _characterStartPosition = _characterControl.transform.position;
            _characterStartRotation = _characterControl.transform.rotation;
            _unitsManager = new UnitsManager(_spawnersManager, _characterControl);

            var currentLevel = 0;
            _spawnersManager.Init(
                _unitSlots,
                _levelsConfig.Levels[currentLevel],
                _levelsConfig.EnemiesPerGroup,
                _characterControl);
            _unitsManager.Init();
            _cameraControl.Init(_characterControl.transform, _characterControl.MovementLogic);

            _uiManager = UIManager.Instance;
            _uiManager.Init();
            _uiManager.ScreensManager.ShowScreen(ScreenTypes.Gameplay);
            _gameplayScreen = _uiManager.ScreensManager.GetScreen(ScreenTypes.Gameplay) as GameplayScreen;

            _characterControl.Init(_gameplayScreen.Joystick, _cameraControl);

            foreach (BaseArea area in _areas)
            {
                SubscribeToArea(area);
            }

            _gameplayScreen.BindInventory(_characterControl.Inventory);
            _unitsManager.OnEnemiesDefeated += HandleEnemiesDefeated;
            _flagCaptureArea.OnCaptured += HandleFlagCaptured;
            _characterControl.Health.OnDeath += HandleCharacterDied;
        }

        private void OnDestroy()
        {
            _unitsManager.OnEnemiesDefeated -= HandleEnemiesDefeated;
            _flagCaptureArea.OnCaptured -= HandleFlagCaptured;
            _characterControl.Health.OnDeath -= HandleCharacterDied;

            foreach (BaseArea area in _areas)
            {
                UnsubscribeFromArea(area);
            }

            _unitsManager.Dispose();
            _spawnersManager.Dispose();
        }

        private void HandleCharacterDied()
        {
            _unitsManager.PlayEnemiesVictory();
            _uiManager.PopupsManager.ShowPopup(PopupTypes.Result);

            ResultPopup resultPopup = (ResultPopup)_uiManager.PopupsManager.GetPopup(PopupTypes.Result);
            resultPopup.OnButtonClicked += HandleResultRestartClicked;
        }

        private void HandleEnemiesDefeated()
        {
            _flagCaptureArea.Unlock();
        }

        private void HandleFlagCaptured()
        {
            _characterControl.PlayVictory();
            _unitsManager.PlayTeammatesVictory();
            _uiManager.PopupsManager.ShowPopup(PopupTypes.LevelComplete);

            LevelCompletedPopup levelCompletedPopup =
                (LevelCompletedPopup)_uiManager.PopupsManager.GetPopup(PopupTypes.LevelComplete);
            levelCompletedPopup.OnButtonClicked += HandleLevelCompleteRestartClicked;
        }

        private void HandleResultRestartClicked()
        {
            _uiManager.PopupsManager.HidePopup(PopupTypes.Result);
            _characterControl.Respawn(_characterStartPosition, _characterStartRotation);
            _unitsManager.ResumeAfterCharacterRespawn();
        }

        private void HandleLevelCompleteRestartClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void HandleAreaCompleted(BaseArea area)
        {
            switch (area.AreaType)
            {
                case AreaType.Barrack:
                    UnsubscribeFromArea(area);
                    _areas.Remove(area);
                    area.gameObject.SetActive(false);
                    _spawnersManager.SpawnBarrack(area.transform.position);

                    DeliveryArea nextBarrackArea = _spawnersManager.SpawnNextBarrackArea(area.transform);
                    _areas.Add(nextBarrackArea);
                    SubscribeToArea(nextBarrackArea);
                    break;
                case AreaType.SlotsUpgrade:
                    _unitSlots.AddSlot();
                    ((DeliveryArea)area).ResetProgress();
                    break;
                case AreaType.GunUpgrade:
                    _characterControl.ShootingLogic.UpgradeGun();
                    ((DeliveryArea)area).ResetProgress();
                    break;
            }
        }

        private void HandleCharacterEnter(BaseArea area, CharacterControl character)
        {
            if (area.AreaType == AreaType.Charge)
            {
                _gameplayScreen.ShowChargeButton(_unitsManager.ChargeUnits);
            }
        }

        private void HandleCharacterExit(BaseArea area, CharacterControl character)
        {
            if (area.AreaType == AreaType.Charge)
            {
                _gameplayScreen.HideChargeButton();
            }
        }

        private void SubscribeToArea(BaseArea area)
        {
            area.OnCharacterEnter += HandleCharacterEnter;
            area.OnCharacterExit += HandleCharacterExit;
            area.OnCompleted += HandleAreaCompleted;
        }

        private void UnsubscribeFromArea(BaseArea area)
        {
            area.OnCharacterEnter -= HandleCharacterEnter;
            area.OnCharacterExit -= HandleCharacterExit;
            area.OnCompleted -= HandleAreaCompleted;
        }
    }
}
