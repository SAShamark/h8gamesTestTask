using System;
using Game.Entities.Units;
using UnityEngine;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class UnitsSpawner
    {
        [SerializeField] private TeammateControl _teammatePrefab;
    }
}