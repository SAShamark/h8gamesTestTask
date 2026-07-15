using System;
using System.Collections.Generic;
using Game.Entities.Units;
using Game.Entities.Units.Enemies;
using Services;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "ScriptableObjects/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private LevelData _levels;
        [SerializeField, Min(1)] private int _enemiesPerGroup = 6;
        
        public LevelData Levels => _levels;
        
        public int EnemiesPerGroup => _enemiesPerGroup;
    }

    [Serializable]
    public class LevelData
    {
        [SerializeField] private List<TypeValueDataService<EnemiesType, int>> _enemiesCount;
        public List<TypeValueDataService<EnemiesType, int>> EnemiesCount => _enemiesCount;
    }
}
