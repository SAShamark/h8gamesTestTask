using System;
using System.Collections.Generic;
using Game.Entities.Units;
using Services;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "LevelsConfig", menuName = "ScriptableObjects/LevelsConfig")]
    public class LevelsConfig : ScriptableObject
    {
        [SerializeField] private List<LevelData> _levels;
        [SerializeField] private int _enemyGroupCount;
        
        public List<LevelData> Levels => _levels;
        
        public int EnemyGroupCount => _enemyGroupCount;
    }

    [Serializable]
    public class LevelData
    {
        [SerializeField] private List<TypeValueDataService<EnemiesType, int>> _enemiesCount;
        public List<TypeValueDataService<EnemiesType, int>> EnemiesCount => _enemiesCount;
    }
}