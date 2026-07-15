using System;
using Game.Entities.Areas;
using Game.Entities.Units;
using Game.Entities.Units.Character;
using Game.Entities.Units.Enemies;
using Game.Entities.Units.Slots;
using Game.Entities.Units.Teammates;
using Services.Currency;
using UnityEngine;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class SpawnersManager : IDisposable
    {
        [SerializeField] private StartCurrenciesSpawner _startCurrenciesSpawner;
        [SerializeField] private UnitsSpawner _unitsSpawner;
        [SerializeField] private BarracksSpawner _barracksSpawner;

        public System.Collections.Generic.IReadOnlyList<TeammateControl> TeammateControls =>
            _unitsSpawner.TeammateControls;
        public System.Collections.Generic.IReadOnlyList<EnemyControl> EnemyControls => _unitsSpawner.EnemyControls;

        public void Init(UnitSlots unitSlots, LevelData levelData, int enemiesPerGroup,
            CharacterControl characterControl)
        {
            _unitsSpawner.OnTeammateDied += HandleTeammateDied;
            _unitsSpawner.OnEnemyDied += HandleEnemyDied;

            _unitsSpawner.Init(unitSlots, levelData, enemiesPerGroup, characterControl);
            _barracksSpawner.Init(unitSlots, _unitsSpawner);
            _startCurrenciesSpawner.Init();
        }

        public void SpawnBarrack(Vector3 position)
        {
            _barracksSpawner.Spawn(position);
        }

        public void SetBarracksSpawning(bool isEnabled)
        {
            _barracksSpawner.SetSpawningEnabled(isEnabled);
        }

        public DeliveryArea SpawnNextBarrackArea(Transform completedArea)
        {
            return _barracksSpawner.SpawnNextArea(completedArea);
        }

        public void SpawnTeammate(Vector3 position)
        {
            _unitsSpawner.SpawnTeammate(position);
        }

        public void SpawnEnemy(Vector3 position)
        {
            _unitsSpawner.SpawnEnemy(position);
        }

        public void Dispose()
        {
            _unitsSpawner.OnTeammateDied -= HandleTeammateDied;
            _unitsSpawner.OnEnemyDied -= HandleEnemyDied;
            _unitsSpawner.Dispose();
            _barracksSpawner.Dispose();
        }

        private void HandleEnemyDied(EnemyControl enemy)
        {
            _startCurrenciesSpawner.SpawnDroppedItem(CurrencyType.Gold, enemy.transform.position);
        }

        private void HandleTeammateDied(TeammateControl teammate)
        {
            _startCurrenciesSpawner.SpawnDroppedItem(CurrencyType.Silver, teammate.transform.position);
        }
    }
}
