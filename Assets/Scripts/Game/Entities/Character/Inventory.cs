using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Character
{
    [Serializable]
    public class Inventory
    {
        [SerializeField] private Transform _container;
        [SerializeField] private float _collectableRange = 1.2f;
        [SerializeField] private Vector3 _firstItemLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 _itemLocalRotation = Vector3.zero;
        [SerializeField] private float _itemVerticalSpacing = 0.1f;
        [SerializeField] private float _itemBackwardSpacing = 0.01f;
        [SerializeField] private InventoryItemAnimation _itemAnimation = new();

        private readonly Collider[] _overlapResults = new Collider[12];
        private List<Item> _items;

        public void Init()
        {
            _items = new List<Item>();
        }

        public void Tick(Vector3 ownerPosition)
        {
            int count = Physics.OverlapSphereNonAlloc(ownerPosition, _collectableRange, _overlapResults);
            for (int i = 0; i < count; i++)
            {
                Item item = _overlapResults[i].GetComponent<Item>();
                if (item == null || item.IsCollected)
                {
                    continue;
                }

                Add(item);
            }
        }

        public void Add(Item item)
        {
            item.MarkCollected();
            _items.Add(item);

            Transform itemTransform = item.Transform;
            itemTransform.SetParent(_container, true);

            Vector3 targetLocalPosition = GetLocalPosition(_items.Count - 1);
            if (_itemAnimation.Enabled)
            {
                _itemAnimation.Play(itemTransform, targetLocalPosition, _itemLocalRotation);
                return;
            }

            itemTransform.localPosition = targetLocalPosition;
            itemTransform.localEulerAngles = _itemLocalRotation;
        }

        private Vector3 GetLocalPosition(int index)
        {
            return _firstItemLocalPosition + new Vector3(0f, index * _itemVerticalSpacing,
                -index * _itemBackwardSpacing);
        }
    }
}
