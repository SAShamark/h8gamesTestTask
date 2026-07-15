using System;
using Game.Entities.Units.Character;
using UnityEngine;

namespace Game.Entities.Areas
{
    public class BaseArea : MonoBehaviour
    {
        [SerializeField] private AreaType _areaType;

        public AreaType AreaType => _areaType;
        public event Action<BaseArea, CharacterControl> OnCharacterEnter;
        public event Action<BaseArea, CharacterControl> OnCharacterExit;
        public event Action<BaseArea> OnCompleted;

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out CharacterControl characterControl))
            {
                HandleCharacterEnter(characterControl);
                OnCharacterEnter?.Invoke(this, characterControl);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out CharacterControl characterControl))
            {
                HandleCharacterExit(characterControl);
                OnCharacterExit?.Invoke(this, characterControl);
            }
        }

        protected virtual void HandleCharacterEnter(CharacterControl character)
        {
        }

        protected virtual void HandleCharacterExit(CharacterControl character)
        {
        }

        protected void NotifyCompleted()
        {
            OnCompleted?.Invoke(this);
        }
    }
}
