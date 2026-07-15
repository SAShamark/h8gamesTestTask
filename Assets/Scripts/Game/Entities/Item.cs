using DG.Tweening;
using Services.Currency;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities
{
    public class Item : MonoBehaviour
    {
        [SerializeField] private CurrencyType _currencyType;
        [SerializeField] private Vector3 _nextItemLocalOffset = new(0f, 0.11f, 0f);
        private Collider _collider;
        private BasePoolDestroyable _poolDestroyable;
        private Vector3 _defaultLocalScale;
        private bool _isCollected;

        public Transform Transform => transform;
        public bool IsCollected => _isCollected;
        public CurrencyType CurrencyType => _currencyType;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _poolDestroyable = GetComponent<BasePoolDestroyable>();
            _defaultLocalScale = transform.localScale;
        }

        public void PrepareForSpawn()
        {
            ResetScale();
            _isCollected = false;
            _collider.enabled = true;
        }

        public void MarkCollected()
        {
            ResetScale();
            _isCollected = true;
            _collider.enabled = false;
        }

        public void ReturnToPool()
        {
            transform.DOKill();
            ResetScale();
            _isCollected = false;
            _collider.enabled = true;
            _poolDestroyable.DestroyObject();
        }

        public Vector3 GetNextItemLocalOffset()
        {
            return _nextItemLocalOffset;
        }

        public void ResetScale()
        {
            transform.localScale = _defaultLocalScale;
        }
    }
}
