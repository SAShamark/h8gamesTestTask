using System;
using Game.Entities.Character;
using UnityEngine;

namespace Game.Entities.Areas
{
    public class BaseArea : MonoBehaviour
    {
        [SerializeField] private AreaType _areaType;

        public AreaType AreaType => _areaType;
        public event Action<CharacterControl> OnCharacterEnter;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out CharacterControl characterControl))
            {
                OnCharacterEnter?.Invoke(characterControl);
            }
        }
    }
}