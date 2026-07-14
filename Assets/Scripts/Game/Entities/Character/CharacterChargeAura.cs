using System.Collections.Generic;
using Game.Entities.Units;
using UnityEngine;

namespace Game.Entities.Character
{
    public class CharacterChargeAura : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _areaRenderer;
        [SerializeField, Min(0.1f)] private float _radius = 4f;

        private IReadOnlyList<TeammateControl> _teammates;
        private bool _isActive;

        private void Awake()
        {
            float diameter = _radius * 2f;
            Vector2 spriteSize = _areaRenderer.sprite.bounds.size;
            _areaRenderer.transform.localScale = new Vector3(
                diameter / spriteSize.x,
                diameter / spriteSize.y,
                1f);
            _areaRenderer.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            float radiusSqr = _radius * _radius;

            foreach (TeammateControl teammate in _teammates)
            {
                Vector3 offset = teammate.transform.position - transform.position;
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
