using DG.Tweening;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Character
{
    public class Item : MonoBehaviour
    {
        [SerializeField] private Vector3 _nextItemLocalOffset = new(0f, 0.11f, 0f);

        private Collider _collider;
        private BasePoolDestroyable _poolDestroyable;
        private bool _isCollected;

        public Transform Transform => transform;
        public bool IsCollected => _isCollected;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _poolDestroyable = GetComponent<BasePoolDestroyable>();
        }

        public void PrepareForSpawn()
        {
            _isCollected = false;
            _collider.enabled = true;
        }

        public void MarkCollected()
        {
            _isCollected = true;
            _collider.enabled = false;
        }

        public void ReturnToPool()
        {
            transform.DOKill();
            _isCollected = false;
            _collider.enabled = true;
            _poolDestroyable.DestroyObject();
        }

        public Vector3 GetNextItemLocalOffset()
        {
            return _nextItemLocalOffset;
        }
    }
}
