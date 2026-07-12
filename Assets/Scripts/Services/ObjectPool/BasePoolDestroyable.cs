using UnityEngine;

namespace Services.ObjectPool
{
    public class BasePoolDestroyable : MonoBehaviour
    {
        private IObjectPool _objectPool;

        public void Init(IObjectPool objectPool)
        {
            _objectPool = objectPool;
        }

        public virtual void DestroyObject()
        {
            if (_objectPool != null)
            {
                _objectPool.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}