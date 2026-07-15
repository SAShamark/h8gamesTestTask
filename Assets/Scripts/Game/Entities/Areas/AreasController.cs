using System;
using System.Collections.Generic;
using Game.Entities.Spawners;
using Game.Entities.Units;
using Game.Entities.Units.Character;
using Game.Entities.Units.Character.Parts;
using Game.Entities.Units.Slots;
using UI.Screens.Variants.Gameplay;

namespace Game.Entities.Areas
{
    public class AreasController
    {
        private readonly List<BaseArea> _areas;
        private readonly GameplayScreen _gameplayScreen;
        private readonly UnitsManager _unitsManager;
        private readonly SpawnersManager _spawnersManager;
        private readonly UnitSlots _unitSlots;
        private readonly ShootingLogic _shootingLogic;
        private readonly Dictionary<AreaType, Action<BaseArea>> _completionActions;

        public AreasController(List<BaseArea> areas, GameplayScreen gameplayScreen, UnitsManager unitsManager,
            SpawnersManager spawnersManager, UnitSlots unitSlots, ShootingLogic shootingLogic)
        {
            _areas = areas;
            _gameplayScreen = gameplayScreen;
            _unitsManager = unitsManager;
            _spawnersManager = spawnersManager;
            _unitSlots = unitSlots;
            _shootingLogic = shootingLogic;
            _completionActions = new Dictionary<AreaType, Action<BaseArea>>
            {
                { AreaType.Barrack, CompleteBarrackArea },
                { AreaType.SlotsUpgrade, CompleteSlotsUpgradeArea },
                { AreaType.GunUpgrade, CompleteGunUpgradeArea }
            };
        }

        public void Init()
        {
            foreach (BaseArea area in _areas)
            {
                Subscribe(area);
            }
        }

        public void Dispose()
        {
            foreach (BaseArea area in _areas)
            {
                Unsubscribe(area);
            }
        }

        private void Register(BaseArea area)
        {
            _areas.Add(area);
            Subscribe(area);
        }

        private void Unregister(BaseArea area)
        {
            Unsubscribe(area);
            _areas.Remove(area);
        }

        private void HandleAreaCompleted(BaseArea area)
        {
            if (_completionActions.TryGetValue(area.AreaType, out Action<BaseArea> completionAction))
            {
                completionAction.Invoke(area);
            }
        }

        private void CompleteBarrackArea(BaseArea area)
        {
            Unregister(area);
            area.gameObject.SetActive(false);
            _spawnersManager.SpawnBarrack(area.transform.position);

            DeliveryArea nextArea = _spawnersManager.SpawnNextBarrackArea(area.transform);
            Register(nextArea);
        }

        private void CompleteSlotsUpgradeArea(BaseArea area)
        {
            _unitSlots.AddSlot();
            ((DeliveryArea)area).ResetProgress();
        }

        private void CompleteGunUpgradeArea(BaseArea area)
        {
            _shootingLogic.UpgradeGun();
            ((DeliveryArea)area).ResetProgress();
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

        private void Subscribe(BaseArea area)
        {
            area.OnCharacterEnter += HandleCharacterEnter;
            area.OnCharacterExit += HandleCharacterExit;
            area.OnCompleted += HandleAreaCompleted;
        }

        private void Unsubscribe(BaseArea area)
        {
            area.OnCharacterEnter -= HandleCharacterEnter;
            area.OnCharacterExit -= HandleCharacterExit;
            area.OnCompleted -= HandleAreaCompleted;
        }
    }
}
