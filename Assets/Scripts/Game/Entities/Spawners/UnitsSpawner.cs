using System;
using System.Collections.Generic;
using Game.Entities.Units;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class UnitsSpawner
    {
        [SerializeField] private TeammateControl _teammatePrefab;

        private readonly List<TeammateControl> _teammateControls = new();
        private UnitSlots _unitSlots;

        public event Action<TeammateControl> OnTeammateDied;

        public IReadOnlyList<TeammateControl> TeammateControls => _teammateControls;

        public void Init(UnitSlots unitSlots)
        {
            _unitSlots = unitSlots;
        }

        public void SpawnTeammate(Vector3 position)
        {
            if (!_unitSlots.TryReserveSlot(out Transform reservedSlot))
            {
                return;
            }

            SpawnTeammate(position, reservedSlot);
        }

        public void SpawnTeammate(Vector3 position, Transform reservedSlot)
        {
            TeammateControl teammate = Object.Instantiate(_teammatePrefab, position, Quaternion.identity);
            teammate.Init(_unitSlots, reservedSlot);
            teammate.OnDied += HandleTeammateDied;
            _teammateControls.Add(teammate);
        }

        private void HandleTeammateDied(TeammateControl teammate)
        {
            teammate.OnDied -= HandleTeammateDied;
            _teammateControls.Remove(teammate);
            OnTeammateDied?.Invoke(teammate);
        }
    }
}
