using UnityEngine;

namespace Game.Entities.Units
{
    public class BaseUnitControl : MonoBehaviour
    {
        private UnitSlots _unitSlots;
        private Transform _reservedSlot;
        private bool _isSlotReleased;

        public Transform ReservedSlot => _reservedSlot;

        public virtual void Init(UnitSlots unitSlots, Transform reservedSlot)
        {
            _unitSlots = unitSlots;
            _reservedSlot = reservedSlot;
        }

        protected virtual void OnDestroy()
        {
            ReleaseSlot();
        }

        protected void ReleaseSlot()
        {
            if (_unitSlots != null && !_isSlotReleased)
            {
                _unitSlots.ReleaseSlot(_reservedSlot);
                _isSlotReleased = true;
            }
        }

        protected void SetSlotOccupied(bool isOccupied)
        {
            _unitSlots.SetSlotOccupied(_reservedSlot, isOccupied);
        }
    }
}
