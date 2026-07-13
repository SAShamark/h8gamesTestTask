using System;
using Game.Entities.Character;
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
                OnCharacterEnter?.Invoke(this, characterControl);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out CharacterControl characterControl))
            {
                OnCharacterExit?.Invoke(this, characterControl);
            }
        }

        protected void NotifyCompleted()
        {
            OnCompleted?.Invoke(this);
        }
    }
}
