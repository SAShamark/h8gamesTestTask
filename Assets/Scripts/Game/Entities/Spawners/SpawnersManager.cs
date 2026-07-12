using System;
using UnityEngine;

namespace Game.Entities
{
    [Serializable]
    public class SpawnersManager
    {
        [SerializeField] StartCurrenciesSpawner _startCurrenciesSpawner;

        public void Init()
        {
            _startCurrenciesSpawner.Init();
        }
    }
}
