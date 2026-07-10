using System.Collections.Generic;
using UnityEngine;

namespace Services.Currency
{
    [CreateAssetMenu(fileName = "CurrencyCollection", menuName = "ScriptableObjects/UI/Sprites/CurrencyCollection")]
    public class CurrencyCollection : ScriptableObject
    {
        [SerializeField] private List<TypeValueDataService<CurrencyType, Sprite>> _currencySprites;
        
        public List<TypeValueDataService<CurrencyType, Sprite>> CurrencySprites => _currencySprites;

        public Sprite GetSprite(CurrencyType type)
        {
            foreach (var item in _currencySprites)
            {
                if (item.Type.Equals(type))
                    return item.Value;
            }
            Debug.LogWarning($"Sprite for CurrencyType {type} not found!");
            return null;
        }
    }
}