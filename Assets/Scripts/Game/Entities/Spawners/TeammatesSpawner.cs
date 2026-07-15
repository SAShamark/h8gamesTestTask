using System;
using System.Collections.Generic;
using Game.Entities.Units.Slots;
using Game.Entities.Units.Teammates;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class TeammatesSpawner : IDisposable
    {
        [SerializeField] private TeammateControl _teammatePrefab;
        [SerializeField] private Transform _container;

        private readonly List<TeammateControl> _teammateControls = new();
        private UnitSlots _unitSlots;

        public event Action<TeammateControl> OnTeammateDied;

        public IReadOnlyList<TeammateControl> TeammateControls => _teammateControls;

        public void Init(UnitSlots unitSlots)
        {
            _unitSlots = unitSlots;
        }

        public void Spawn(Vector3 position)
        {
            if (!_unitSlots.TryReserveSlot(out Transform reservedSlot))
            {
                return;
            }

            Spawn(position, reservedSlot);
        }

        public void Spawn(Vector3 position, Transform reservedSlot)
        {
            TeammateControl teammate = Object.Instantiate(_teammatePrefab, position, Quaternion.identity, _container);
            teammate.Init(_unitSlots, reservedSlot);
            teammate.OnDied += HandleTeammateDied;
            _teammateControls.Add(teammate);
        }

        public void Dispose()
        {
            for (int i = 0; i < _teammateControls.Count; i++)
            {
                _teammateControls[i].OnDied -= HandleTeammateDied;
            }
        }

        private void HandleTeammateDied(TeammateControl teammate)
        {
            teammate.OnDied -= HandleTeammateDied;
            _teammateControls.Remove(teammate);
            OnTeammateDied?.Invoke(teammate);
        }
    }
}
