using System.Collections.Generic;
using UnityEngine;

namespace Services.ObjectPool
{
    public interface IObjectPool
    {
        void ReturnToPool(GameObject go);
    }
    public class ObjectPool<T> : IObjectPool where T : Component    {
        private T _prefab;
        private Transform _container;
        
        private List<T> _includedPool = new();
        private List<T> _excludedPool = new();

        public ObjectPool(T prefab, int count, Transform container)
        {
            _prefab = prefab;
            _container = container;

            CreatePool(count);
        }

        private void CreatePool(int count)
        {
            _excludedPool.Clear();
            _includedPool.Clear();
            
            for (int i = 0; i < count; i++)
            {
                CreateElement();
            }
        }

        private T CreateElement(bool isActiveByDefault = false)
        {
            T createdElement = Object.Instantiate(_prefab, _container);
            createdElement.gameObject.SetActive(isActiveByDefault);

            var destroyables = createdElement.GetComponents<BasePoolDestroyable>();
            if (destroyables is { Length: > 0 })
            {
                foreach (BasePoolDestroyable poolDestroyable in destroyables)
                {
                    poolDestroyable.Init(this);
                }
            }
            else
            {
                createdElement.gameObject.AddComponent<BasePoolDestroyable>().Init(this);
            }

            if (isActiveByDefault)
            {
                _includedPool.Add(createdElement);
            }
            else
            {
                _excludedPool.Add(createdElement);
            }

            return createdElement;
        }

        private bool HasFreeElement(out T element)
        {
            if (_excludedPool.Count > 0)
            {
                T cachedElement = _excludedPool[0];
                element = cachedElement;
                
                cachedElement.gameObject.SetActive(true);
                _includedPool.Add(cachedElement);
                _excludedPool.RemoveAt(0);
                return true;
            }

            element = null;
            return false;
        }

        public void ReturnToPool(T element)
        {
            element.transform.SetParent(_container, false);
            element.gameObject.SetActive(false);
            _includedPool.Remove(element);
            _excludedPool.Add(element);
        }

        public void ReturnToPool(GameObject go)
        {
            if (go.TryGetComponent(out T element))
            {
                ReturnToPool(element);
            }
        }

        public T GetFreeElement()
        {
            if (HasFreeElement(out T element))
            {
                return element;
            }
            
            return CreateElement(true);
        }
    }
}
