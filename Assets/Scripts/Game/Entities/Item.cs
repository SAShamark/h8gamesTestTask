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

        public Transform Transform => transform;
        public bool IsCollected { get; private set; }

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
            IsCollected = false;
            _collider.enabled = true;
        }

        public void MarkCollected()
        {
            ResetScale();
            IsCollected = true;
            _collider.enabled = false;
        }

        public void ReturnToPool()
        {
            transform.DOKill();
            ResetScale();
            IsCollected = false;
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
