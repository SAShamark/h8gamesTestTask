using System;
using System.Collections.Generic;
using Game.Entities.Units;
using Services;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    [CreateAssetMenu(fileName = "LevelsConfig", menuName = "ScriptableObjects/LevelsConfig")]
    public class LevelsConfig : ScriptableObject
    {
        [SerializeField] private List<LevelData> _levels;
        [SerializeField, Min(1)] private int _enemiesPerGroup = 6;
        
        public List<LevelData> Levels => _levels;
        
        public int EnemiesPerGroup => _enemiesPerGroup;
    }

    [Serializable]
    public class LevelData
    {
        [SerializeField] private List<TypeValueDataService<EnemiesType, int>> _enemiesCount;
        public List<TypeValueDataService<EnemiesType, int>> EnemiesCount => _enemiesCount;
    }
}
