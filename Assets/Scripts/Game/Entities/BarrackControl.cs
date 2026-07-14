using System;
using Game.Entities.Units;
using UnityEngine;

namespace Game.Entities
{
    public class BarrackControl : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _fill;
        [SerializeField] private float _spawnCooldown = 2f;
        [SerializeField] private Transform _spawnPoint;

        private UnitSlots _unitSlots;
        private float _nextSpawnTime;
        private Vector3 _fillDefaultScale;
        private Vector3 _fillDefaultLocalPosition;
        private float _fillDefaultWidth;
        private bool _isSpawningEnabled = true;

        public event Action<Vector3, Transform> OnSpawnTeammate;
        private void Awake()
        {
            _fillDefaultScale = _fill.transform.localScale;
            _fillDefaultLocalPosition = _fill.transform.localPosition;
            _fillDefaultWidth = _fill.sprite.bounds.size.x * _fillDefaultScale.x;
        }

        public void Init(UnitSlots unitSlots)
        {
            _unitSlots = unitSlots;
            _nextSpawnTime = Time.time + _spawnCooldown;
            SetFillProgress(0f);
        }

        private void Update()
        {
            if (!_isSpawningEnabled)
            {
                return;
            }

            float cooldownStartTime = _nextSpawnTime - _spawnCooldown;
            SetFillProgress(Mathf.InverseLerp(cooldownStartTime, _nextSpawnTime, Time.time));

            if (Time.time < _nextSpawnTime || !_unitSlots.TryReserveSlot(out Transform slot))
            {
                return;
            }

            OnSpawnTeammate?.Invoke(_spawnPoint.position, slot);
            _nextSpawnTime = Time.time + _spawnCooldown;
            SetFillProgress(0f);
        }

        public void SetSpawningEnabled(bool isEnabled)
        {
            _isSpawningEnabled = isEnabled;
            SetFillProgress(0f);

            if (isEnabled)
            {
                _nextSpawnTime = Time.time + _spawnCooldown;
            }
        }

        private void SetFillProgress(float progress)
        {
            Vector3 scale = _fillDefaultScale;
            scale.x *= progress;
            _fill.transform.localScale = scale;

            Vector3 position = _fillDefaultLocalPosition;
            position.x -= _fillDefaultWidth * (1f - progress) * 0.5f;
            _fill.transform.localPosition = position;
        }
    }
}
