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
        [SerializeField] private Transform _container;
        [SerializeField] private List<EnemyPrefabData> _enemyPrefabs;

        [Header("Formation")]
        [SerializeField, Min(1)] private int _unitsPerRow = 2;
        [SerializeField, Min(0.1f)] private float _unitSpacing = 1.1f;
        [SerializeField, Min(0f)] private float _groupRadius = 3.5f;
        [SerializeField] private float _circleStartAngle;
        [SerializeField, Min(0f)] private float _navMeshSampleRadius = 0.35f;

        private readonly List<EnemyControl> _enemyControls = new();
        private CharacterControl _target;

        public event Action<EnemyControl> OnEnemyDied;

        public IReadOnlyList<EnemyControl> EnemyControls => _enemyControls;

        public void Init(LevelData levelData, int enemiesPerGroup, CharacterControl target)
        {
            _target = target;
            List<EnemiesType> enemies = BuildEnemiesList(levelData);
            SpawnFormation(enemies, Mathf.Max(1, enemiesPerGroup));
        }

        public void Spawn(Vector3 position, EnemiesType enemyType = EnemiesType.Standard)
        {
            SpawnEnemy(enemyType, position, GetRotationTowardsCharacter(position));
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

        private void SpawnFormation(IReadOnlyList<EnemiesType> enemies, int enemiesPerGroup)
        {
            if (enemies.Count == 0)
            {
                return;
            }

            int groupCount = Mathf.CeilToInt(enemies.Count / (float)enemiesPerGroup);
            Vector3 circleForward = Vector3.ProjectOnPlane(
                _formationAnchor.forward, Vector3.up).normalized;

            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                int groupIndex = enemyIndex / enemiesPerGroup;
                int indexInGroup = enemyIndex % enemiesPerGroup;
                int groupStartIndex = groupIndex * enemiesPerGroup;
                int unitsInGroup = Mathf.Min(enemiesPerGroup, enemies.Count - groupStartIndex);
                int rowsInGroup = Mathf.CeilToInt(unitsInGroup / (float)_unitsPerRow);
                int row = indexInGroup / _unitsPerRow;
                int column = indexInGroup % _unitsPerRow;
                int unitsInRow = Mathf.Min(_unitsPerRow, unitsInGroup - row * _unitsPerRow);
                float centeredColumn = column - (unitsInRow - 1) * 0.5f;
                float centeredRow = row - (rowsInGroup - 1) * 0.5f;

                float groupAngle = _circleStartAngle + groupIndex * (360f / groupCount);
                Vector3 groupForward = Quaternion.AngleAxis(groupAngle, Vector3.up) * circleForward;
                Vector3 groupRight = Vector3.Cross(Vector3.up, groupForward);
                Vector3 plannedPosition = _formationAnchor.position
                                          + groupForward * (_groupRadius + centeredRow * _unitSpacing)
                                          + groupRight * (centeredColumn * _unitSpacing);

                Vector3 spawnPosition = plannedPosition;
                if (NavMesh.SamplePosition(plannedPosition, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                }

                SpawnEnemy(
                    enemies[enemyIndex], spawnPosition, GetRotationTowardsCharacter(spawnPosition));
            }
        }

        private Quaternion GetRotationTowardsCharacter(Vector3 spawnPosition)
        {
            Vector3 direction = _target.transform.position - spawnPosition;
            direction.y = 0f;
            return Quaternion.LookRotation(direction);
        }

        private void SpawnEnemy(EnemiesType enemyType, Vector3 position, Quaternion rotation)
        {
            EnemyControl enemy = Object.Instantiate(GetPrefab(enemyType), position, rotation, _container);
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
