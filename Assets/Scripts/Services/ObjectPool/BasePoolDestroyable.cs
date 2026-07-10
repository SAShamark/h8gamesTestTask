using UnityEngine;

namespace Services.ObjectPool
{
    public class BasePoolDestroyable : MonoBehaviour
    {
        private ObjectPool _objectPool;

        public void Init(ObjectPool objectPool)
        {
            _objectPool = objectPool;
        }

        public virtual void DestroyObject()
        {
            if (_objectPool != null)
            {
                _objectPool.TurnOffObject(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}