using System;
using System.Collections.Generic;
using Game.Entities.Areas;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class BarracksSpawner : IDisposable
    {
        [SerializeField] private BarrackControl _barrackPrefab;
        [SerializeField] private DeliveryArea _barrackAreaPrefab;
        [SerializeField] private Vector3 _nextBarrackAreaLocalOffset = new(3.2f, 0f, 0f);

        private readonly List<BarrackControl> _barracks = new();
        private UnitSlots _unitSlots;
        private UnitsSpawner _unitsSpawner;

        public IReadOnlyList<BarrackControl> Barracks => _barracks;

        public void Init(UnitSlots unitSlots, UnitsSpawner unitsSpawner)
        {
            _unitSlots = unitSlots;
            _unitsSpawner = unitsSpawner;
        }

        public void Spawn(Vector3 position)
        {
            BarrackControl barrack = Object.Instantiate(_barrackPrefab, position, Quaternion.identity);
            barrack.OnSpawnTeammate += _unitsSpawner.SpawnTeammate;
            barrack.Init(_unitSlots);
            _barracks.Add(barrack);
        }

        public DeliveryArea SpawnNextArea(Transform completedArea)
        {
            Vector3 position = completedArea.position
                               + completedArea.rotation * _nextBarrackAreaLocalOffset;

            return Object.Instantiate(_barrackAreaPrefab, position, completedArea.rotation);
        }

        public void Dispose()
        {
            foreach (BarrackControl barrack in _barracks)
            {
                barrack.OnSpawnTeammate -= _unitsSpawner.SpawnTeammate;
            }

            _barracks.Clear();
        }
    }
}