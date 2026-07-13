using System;
using System.Collections.Generic;
using Game.Entities.Areas;
using Game.Entities.Character;
using Game.Entities.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class EnemiesSpawner : IDisposable
    {
        [SerializeField] private Transform _formationAnchor;
        [SerializeField] private GroundBattleField _groundBattleField;
        [SerializeField] private List<EnemyPrefabData> _enemyPrefabs;

        [Header("Formation")]
        [SerializeField, Min(1)] private int _groupsPerRow = 2;
        [SerializeField, Min(1)] private int _unitsPerRow = 2;
        [SerializeField, Min(0.1f)] private float _unitSpacing = 1.1f;
        [SerializeField, Min(0f)] private float _firstGroupDistance = 2f;
        [SerializeField, Min(0f)] private float _distanceBetweenGroups = 1.5f;
        [SerializeField, Min(0f)] private float _navMeshSampleRadius = 0.35f;

        private readonly List<EnemyControl> _enemyControls = new();
        private CharacterControl _target;

        public event Action<EnemyControl> OnEnemyDied;

        public IReadOnlyList<EnemyControl> EnemyControls => _enemyControls;

        public void Init(LevelData levelData, int groupCount, CharacterControl target)
        {
            _target = target;
            List<EnemiesType> enemies = BuildEnemiesList(levelData);
            SpawnFormation(enemies, Mathf.Max(1, groupCount));
        }

        public void Spawn(Vector3 position, EnemiesType enemyType = EnemiesType.Standard)
        {
            SpawnEnemy(enemyType, position, Quaternion.LookRotation(-_formationAnchor.forward));
        }

        public void Dispose()
        {
            for (int i = 0; i < _enemyControls.Count; i++)
            {
                _enemyControls[i].OnDied -= HandleEnemyDied;
            }
        }

        private List<EnemiesType> BuildEnemiesList(LevelData levelData)
        {
            List<EnemiesType> enemies = new();

            for (int i = 0; i < levelData.EnemiesCount.Count; i++)
            {
                var enemyData = levelData.EnemiesCount[i];

                for (int enemyIndex = 0; enemyIndex < enemyData.Value; enemyIndex++)
                {
                    enemies.Add(enemyData.Type);
                }
            }

            return enemies;
        }

        private void SpawnFormation(IReadOnlyList<EnemiesType> enemies, int groupCount)
        {
            if (enemies.Count == 0)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(_formationAnchor.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            groupCount = Mathf.Min(groupCount, enemies.Count);
            int unitsPerGroup = Mathf.CeilToInt(enemies.Count / (float)groupCount);
            int rowsPerGroup = Mathf.CeilToInt(unitsPerGroup / (float)_unitsPerRow);
            float groupWidth = (_unitsPerRow - 1) * _unitSpacing;
            float groupDepth = (rowsPerGroup - 1) * _unitSpacing;
            float horizontalGroupStep = groupWidth + _distanceBetweenGroups;
            float verticalGroupStep = groupDepth + _distanceBetweenGroups;

            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                int groupIndex = enemyIndex / unitsPerGroup;
                int indexInGroup = enemyIndex % unitsPerGroup;
                int groupRow = groupIndex / _groupsPerRow;
                int groupColumn = groupIndex % _groupsPerRow;
                int groupsInThisRow = Mathf.Min(_groupsPerRow, groupCount - groupRow * _groupsPerRow);
                float centeredGroupColumn = groupColumn - (groupsInThisRow - 1) * 0.5f;
                int row = indexInGroup / _unitsPerRow;
                int column = indexInGroup % _unitsPerRow;
                int groupStartIndex = groupIndex * unitsPerGroup;
                int unitsInGroup = Mathf.Min(unitsPerGroup, enemies.Count - groupStartIndex);
                int unitsInRow = Mathf.Min(_unitsPerRow, unitsInGroup - row * _unitsPerRow);
                float centeredColumn = column - (unitsInRow - 1) * 0.5f;
                float distanceFromFlag = _firstGroupDistance + groupRow * verticalGroupStep + row * _unitSpacing;
                float lateralOffset = centeredGroupColumn * horizontalGroupStep + centeredColumn * _unitSpacing;
                Vector3 plannedPosition = _formationAnchor.position
                                          + forward * distanceFromFlag
                                          + right * lateralOffset;

                Vector3 spawnPosition = plannedPosition;
                if (NavMesh.SamplePosition(plannedPosition, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                }

                SpawnEnemy(enemies[enemyIndex], spawnPosition, Quaternion.LookRotation(-forward));
            }
        }

        private void SpawnEnemy(EnemiesType enemyType, Vector3 position, Quaternion rotation)
        {
            EnemyControl enemy = Object.Instantiate(GetPrefab(enemyType), position, rotation);
            enemy.Init(_target);
            enemy.OnDied += HandleEnemyDied;
            _enemyControls.Add(enemy);
            _groundBattleField.AddEnemy(enemy.transform);
        }

        private EnemyControl GetPrefab(EnemiesType enemyType)
        {
            for (int i = 0; i < _enemyPrefabs.Count; i++)
            {
                if (_enemyPrefabs[i].Type == enemyType)
                {
                    return _enemyPrefabs[i].Prefab;
                }
            }

            throw new InvalidOperationException($"Enemy prefab for {enemyType} is not configured.");
        }

        private void HandleEnemyDied(EnemyControl enemy)
        {
            enemy.OnDied -= HandleEnemyDied;
            _enemyControls.Remove(enemy);
            _groundBattleField.RemoveEnemy(enemy.transform);
            OnEnemyDied?.Invoke(enemy);
        }
    }

    [Serializable]
    public class EnemyPrefabData
    {
        [SerializeField] private EnemiesType _type;
        [SerializeField] private EnemyControl _prefab;

        public EnemiesType Type => _type;
        public EnemyControl Prefab => _prefab;
    }
}
