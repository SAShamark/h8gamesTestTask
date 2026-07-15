using System;
using UnityEngine;

namespace Game.Entities.Units
{
    public interface IProjectileTarget
    {
        bool IsAlive { get; }
        Vector3 AimPosition { get; }

        event Action Died;

        void ApplyDamage(float damage);
        void PlayHitFeedback(Vector3 hitPosition);
    }
}
