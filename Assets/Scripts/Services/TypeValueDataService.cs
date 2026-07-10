using System;
using UnityEngine;

namespace Services
{
    [Serializable]
    public class TypeValueDataService<TType, TValue>
    {
        [SerializeField] private TType _type;
        [SerializeField] private TValue _value;
        public TType Type => _type;
        public TValue Value => _value;
        public TypeValueDataService(TType type, TValue value)
        {
            _type = type;
            _value = value;
        }
    }
}