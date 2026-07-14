using System;
using Game.Entities.Areas;
using Game.Entities.Character;
using Game.Entities.Units;
using Services.Currency;
using UnityEngine;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class SpawnersManager : IDisposable
    {
        private const float DroppedItemGroundOffset = 0.1f;

        [SerializeField] private StartCurrenciesSpawner _startCurrenciesSpawner;
        [SerializeField] private UnitsSpawner _unitsSpawner;
        [SerializeField] private EnemiesSpawner _enemiesSpawner;
        [SerializeField] private BarracksSpawner _barracksSpawner;

        public System.Collections.Generic.IReadOnlyList<TeammateControl> TeammateControls =>
            _unitsSpawner.TeammateControls;
        public System.Collections.Generic.IReadOnlyList<EnemyControl> EnemyControls => _enemiesSpawner.EnemyControls;

        public void Init(UnitSlots unitSlots, LevelData levelData, int enemiesPerGroup,
            CharacterControl characterControl)
        {
            _unitsSpawner.OnTeammateDied += HandleTeammateDied;
            _enemiesSpawner.OnEnemyDied += HandleEnemyDied;

            _unitsSpawner.Init(unitSlots);
            _enemiesSpawner.Init(levelData, enemiesPerGroup, characterControl);
            _barracksSpawner.Init(unitSlots, _unitsSpawner);
            _startCurrenciesSpawner.Init();
        }

        public void SpawnBarrack(Vector3 position)
        {
            _barracksSpawner.Spawn(position);
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
            _enemiesSpawner.Spawn(position);
        }

        public void Dispose()
        {
            _unitsSpawner.OnTeammateDied -= HandleTeammateDied;
            _enemiesSpawner.OnEnemyDied -= HandleEnemyDied;
            _enemiesSpawner.Dispose();
            _barracksSpawner.Dispose();
        }

        private void HandleEnemyDied(EnemyControl enemy)
        {
            Vector3 dropPosition = enemy.transform.position + Vector3.up * DroppedItemGroundOffset;
            _startCurrenciesSpawner.SpawnItem(CurrencyType.Gold, dropPosition);
        }

        private void HandleTeammateDied(TeammateControl teammate)
        {
            Vector3 dropPosition = teammate.transform.position + Vector3.up * DroppedItemGroundOffset;
            _startCurrenciesSpawner.SpawnItem(CurrencyType.Silver, dropPosition);
        }
    }
}
