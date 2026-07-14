using UnityEngine;

namespace Game.Entities.Character
{
    public interface IProjectileTarget
    {
        bool IsAlive { get; }
        int LifeVersion { get; }
        Vector3 AimPosition { get; }

        void ApplyDamage(float damage);
        void PlayHitFeedback(Vector3 hitPosition);
    }
}
