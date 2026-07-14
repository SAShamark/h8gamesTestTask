using System;
using Game.Entities.Character;
using Services.Currency;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Spawners
{
    [Serializable]
    public class StartCurrenciesSpawner
    {
        private const float GroundOffset = 0.1f;

        [SerializeField] private Item _itemPrefab1;
        [SerializeField] private int _itemsCount1 = 6;
        [SerializeField] private Item _itemPrefab2;
        [SerializeField] private int _itemsCount2 = 6;
        [SerializeField] private Transform _itemsContainer;

        private ObjectPool<Item> _itemsPool1;
        private ObjectPool<Item> _itemsPool2;

        public void Init()
        {
            _itemsPool1 = new ObjectPool<Item>(_itemPrefab1, _itemsCount1, _itemsContainer);
            _itemsPool2 = new ObjectPool<Item>(_itemPrefab2, _itemsCount2, _itemsContainer);

            Vector3 spawnPosition = SpawnItemsColumn(
                _itemPrefab1,
                _itemsCount1,
                Vector3.up * GroundOffset);
            SpawnItemsColumn(_itemPrefab2, _itemsCount2, spawnPosition);
        }

        private Vector3 SpawnItemsColumn(Item itemPrefab, int count, Vector3 startPosition)
        {
            Vector3 itemPosition = startPosition;

            for (int i = 0; i < count; i++)
            {
                Item item = SpawnItem(itemPrefab, itemPosition);
                itemPosition += item.GetNextItemLocalOffset();
            }

            return itemPosition;
        }

        private Item SpawnItem(Item itemPrefab, Vector3 localPosition)
        {
            ObjectPool<Item> pool = itemPrefab == _itemPrefab1 ? _itemsPool1 : _itemsPool2;
            Item item = pool.GetFreeElement();

            item.PrepareForSpawn();
            item.transform.localPosition = localPosition;
            return item;
        }

        public void SpawnItem(CurrencyType currencyType, Vector3 worldPosition)
        {
            Item itemPrefab = _itemPrefab1.CurrencyType == currencyType ? _itemPrefab1 : _itemPrefab2;
            ObjectPool<Item> pool = itemPrefab == _itemPrefab1 ? _itemsPool1 : _itemsPool2;
            Item item = pool.GetFreeElement();

            item.PrepareForSpawn();
            item.transform.position = worldPosition;
        }
    }
}
