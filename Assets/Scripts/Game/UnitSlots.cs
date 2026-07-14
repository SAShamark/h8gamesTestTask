using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class UnitSlots : MonoBehaviour
    {
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private int _startSlotCount;
        [SerializeField, Min(0f)] private float _columnSpacing = 1.5f;
        [SerializeField, Min(0f)] private float _rowSpacing = 1.5f;
        [SerializeField, Min(1)] private int _maxSlotsPerRow = 5;

        private readonly List<Transform> _slots = new();
        private readonly HashSet<Transform> _occupiedSlots = new();

        public int SlotCount => _slots.Count;
        public int OccupiedSlotCount => _occupiedSlots.Count;

        private void Awake()
        {
            for (int i = 0; i < _startSlotCount; i++)
            {
                CreateSlot();
            }
        }

        public void AddSlot()
        {
            CreateSlot();
        }

        public bool TryReserveSlot(out Transform slot)
        {
            foreach (var t in _slots)
            {
                if (_occupiedSlots.Add(t))
                {
                    slot = t;
                    return true;
                }
            }

            slot = null;
            return false;
        }

        public void ReleaseSlot(Transform slot)
        {
            SetSlotOccupied(slot, false);
            _occupiedSlots.Remove(slot);
        }

        public void SetSlotOccupied(Transform slot, bool isOccupied)
        {
            slot.GetComponent<UnitSlotView>().SetOccupied(isOccupied);
        }

        private void CreateSlot()
        {
            int slotIndex = _slots.Count;
            int column = slotIndex % _maxSlotsPerRow;
            int row = slotIndex / _maxSlotsPerRow;
            GameObject slot = Instantiate(_slotPrefab, transform);
            slot.transform.localPosition = new Vector3(
                column * _columnSpacing,
                row * _rowSpacing,
                0f);
            slot.transform.localRotation = Quaternion.identity;
            _slots.Add(slot.transform);
        }
    }
}
