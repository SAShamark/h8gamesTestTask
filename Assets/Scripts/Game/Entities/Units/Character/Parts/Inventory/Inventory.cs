using System;
using System.Collections.Generic;
using Services.Currency;
using UnityEngine;

namespace Game.Entities.Units.Character.Parts.Inventory
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
        [SerializeField] private InventoryItemDeliveryAnimation _deliveryAnimation = new();
        [SerializeField] private InventoryItemDropAnimation _dropAnimation = new();

        private readonly Collider[] _overlapResults = new Collider[12];
        private List<Item> _items;

        public event Action<CurrencyType, int> OnItemsCountChanged;

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
            NotifyItemsCountChanged(item.CurrencyType);

            Transform itemTransform = item.Transform;
            Vector3 targetLocalPosition = GetLocalPosition(_items.Count - 1);

            if (_itemAnimation.Enabled)
            {
                _itemAnimation.Play(itemTransform, _container, targetLocalPosition, _itemLocalRotation,
                    () => GetLocalPosition(_items.IndexOf(item)));
                return;
            }

            itemTransform.SetParent(_container, true);
            itemTransform.localPosition = targetLocalPosition;
            itemTransform.localEulerAngles = _itemLocalRotation;
        }

        public bool TryTakeItem(CurrencyType currencyType, out Item item)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].CurrencyType != currencyType)
                {
                    continue;
                }

                item = _items[i];
                _items.RemoveAt(i);
                UpdateStackPositions(i);
                NotifyItemsCountChanged(currencyType);
                return true;
            }

            item = null;
            return false;
        }

        public bool TryDeliverItem(CurrencyType currencyType, Transform deliveryTarget, Action onDelivered)
        {
            if (!TryTakeItem(currencyType, out Item item))
            {
                return false;
            }

            _deliveryAnimation.Play(item, deliveryTarget, () =>
            {
                item.ReturnToPool();
                onDelivered.Invoke();
            });

            return true;
        }

        public int GetItemsCount(CurrencyType currencyType)
        {
            int count = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].CurrencyType == currencyType)
                {
                    count++;
                }
            }

            return count;
        }

        public void DropAll(Vector3 origin)
        {
            int itemsCount = _items.Count;
            HashSet<CurrencyType> droppedCurrencyTypes = new();

            for (int i = 0; i < itemsCount; i++)
            {
                Item item = _items[i];
                droppedCurrencyTypes.Add(item.CurrencyType);
                _dropAnimation.Play(item, origin, i, itemsCount);
            }

            _items.Clear();

            foreach (CurrencyType currencyType in droppedCurrencyTypes)
            {
                NotifyItemsCountChanged(currencyType);
            }
        }

        private void NotifyItemsCountChanged(CurrencyType currencyType)
        {
            OnItemsCountChanged?.Invoke(currencyType, GetItemsCount(currencyType));
        }

        private Vector3 GetLocalPosition(int index)
        {
            return _firstItemLocalPosition + new Vector3(0f, index * _itemVerticalSpacing,
                -index * _itemBackwardSpacing);
        }

        private void UpdateStackPositions(int startIndex)
        {
            for (int i = startIndex; i < _items.Count; i++)
            {
                Transform itemTransform = _items[i].Transform;
                if (itemTransform.parent == _container)
                {
                    itemTransform.localPosition = GetLocalPosition(i);
                }
            }
        }
    }
}
