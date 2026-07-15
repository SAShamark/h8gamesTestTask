using System;
using System.Collections.Generic;
using Game.Entities.Units.Teammates;
using UnityEngine;

namespace Game.Entities.Units.Character.Parts
{
    [Serializable]
    public class CharacterChargeAura
    {
        [SerializeField] private SpriteRenderer _areaRenderer;
        [SerializeField, Min(0.1f)] private float _radius = 4f;

        private IReadOnlyList<TeammateControl> _teammates;
        private Transform _characterTransform;
        private bool _isActive;

        public void Init(Transform characterTransform)
        {
            _characterTransform = characterTransform;

            float diameter = _radius * 2f;
            Vector2 spriteSize = _areaRenderer.sprite.bounds.size;
            _areaRenderer.transform.localScale = new Vector3(
                diameter / spriteSize.x,
                diameter / spriteSize.y,
                1f);
            _areaRenderer.gameObject.SetActive(false);
        }

        public void Tick()
        {
            UpdateTeammateBuffs();
        }

        private void UpdateTeammateBuffs()
        {
            if (!_isActive)
            {
                return;
            }

            float radiusSqr = _radius * _radius;

            foreach (TeammateControl teammate in _teammates)
            {
                Vector3 offset = teammate.transform.position - _characterTransform.position;
                offset.y = 0f;
                teammate.SetChargeBuff(teammate.IsAlive && offset.sqrMagnitude <= radiusSqr);
            }
        }

        public void Activate(IReadOnlyList<TeammateControl> teammates)
        {
            _teammates = teammates;
            _isActive = true;
            _areaRenderer.gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _areaRenderer.gameObject.SetActive(false);

            foreach (TeammateControl teammate in _teammates)
            {
                teammate.SetChargeBuff(false);
            }
        }
    }
}
