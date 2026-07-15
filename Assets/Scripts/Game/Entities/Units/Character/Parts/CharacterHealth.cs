using System;
using Game.Entities.Units.Base;
using UnityEngine;

namespace Game.Entities.Units.Character.Parts
{
    [Serializable]
    public class CharacterHealth : Health
    {
        [SerializeField] private float _regenerationDelay = 3f;
        [SerializeField] private float _regenerationPerSecond = 20f;

        private float _lastCombatActivityTime;

        public override void Init()
        {
            base.Init();
            RegisterCombatActivity();
        }

        public override void ApplyDamage(float damage)
        {
            RegisterCombatActivity();
            base.ApplyDamage(damage);
        }

        public void Tick(float deltaTime)
        {
            if (Time.time < _lastCombatActivityTime + _regenerationDelay || CurrentHealth >= MaxHealth)
            {
                return;
            }

            Heal(_regenerationPerSecond * deltaTime);
        }

        public void RegisterCombatActivity()
        {
            _lastCombatActivityTime = Time.time;
        }
    }
}
