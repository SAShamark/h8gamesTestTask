using System;
using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Areas;
using Game.Entities.Character;
using Game.Entities.Spawners;
using Game.Entities.Units;
using UI.Managers;
using UI.Screens;
using UI.Screens.Variants.Gameplay;
using UnityEngine;

namespace Game
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private LevelsConfig _levelsConfig;
        [SerializeField] private UnitSlots _unitSlots;
        [SerializeField] private CharacterControl _characterControl;
        [SerializeField] private CameraControl _cameraControl;
        [SerializeField] private SpawnersManager _spawnersManager;
        [SerializeField] private List<BaseArea> _areas;

        private UnitsManager _unitsManager;
        private UIManager _uiManager;
        private GameplayScreen _gameplayScreen;

        private void Start()
        {
            _unitsManager = new UnitsManager(_spawnersManager);

            var currentLevel = 0;
            _spawnersManager.Init(
                _unitSlots,
                _levelsConfig.Levels[currentLevel],
                _levelsConfig.EnemyGroupCount,
                _characterControl);
            _unitsManager.Init();
            _cameraControl.Init(_characterControl.transform, _characterControl.MovementLogic);

            _uiManager = UIManager.Instance;
            _uiManager.Init();
            _uiManager.ScreensManager.ShowScreen(ScreenTypes.Gameplay);
            _gameplayScreen = _uiManager.ScreensManager.GetScreen(ScreenTypes.Gameplay) as GameplayScreen;

            _characterControl.Init(_gameplayScreen.Joystick);
            _gameplayScreen.BindInventory(_characterControl.Inventory);
            _unitsManager.OnEnemiesDefeated += _characterControl.PlayVictory;
            _characterControl.Health.OnDeath += HandleCharacterDied;

            foreach (BaseArea area in _areas)
            {
                SubscribeToArea(area);
            }
        }

        private void OnDestroy()
        {
            _unitsManager.OnEnemiesDefeated -= _characterControl.PlayVictory;
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
