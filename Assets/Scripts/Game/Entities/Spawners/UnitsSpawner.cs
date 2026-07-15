using System;
using System.Collections.Generic;
using Game.Entities.Units.Character;
using Game.Entities.Units.Enemies;
using Game.Entities.Units.Slots;
using Game.Entities.Units.Teammates;
using UnityEngine;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class UnitsSpawner : IDisposable
    {
        [SerializeField] private TeammatesSpawner _teammatesSpawner;
        [SerializeField] private EnemiesSpawner _enemiesSpawner;

        public event Action<TeammateControl> OnTeammateDied;
        public event Action<EnemyControl> OnEnemyDied;

        public IReadOnlyList<TeammateControl> TeammateControls => _teammatesSpawner.TeammateControls;
        public IReadOnlyList<EnemyControl> EnemyControls => _enemiesSpawner.EnemyControls;

        public void Init(UnitSlots unitSlots, LevelData levelData, int enemiesPerGroup,
            CharacterControl characterControl)
        {
            _teammatesSpawner.OnTeammateDied += HandleTeammateDied;
            _enemiesSpawner.OnEnemyDied += HandleEnemyDied;

            _teammatesSpawner.Init(unitSlots);
            _enemiesSpawner.Init(levelData, enemiesPerGroup, characterControl);
        }

        public void SpawnTeammate(Vector3 position)
        {
            _teammatesSpawner.Spawn(position);
        }

        public void SpawnTeammate(Vector3 position, Transform reservedSlot)
        {
            _teammatesSpawner.Spawn(position, reservedSlot);
        }

        public void SpawnEnemy(Vector3 position)
        {
            _enemiesSpawner.Spawn(position);
        }

        public void Dispose()
        {
            _teammatesSpawner.OnTeammateDied -= HandleTeammateDied;
            _enemiesSpawner.OnEnemyDied -= HandleEnemyDied;

            _teammatesSpawner.Dispose();
            _enemiesSpawner.Dispose();
        }

        private void HandleTeammateDied(TeammateControl teammate)
        {
            OnTeammateDied?.Invoke(teammate);
        }

        private void HandleEnemyDied(EnemyControl enemy)
        {
            OnEnemyDied?.Invoke(enemy);
        }
    }
}
