using UnityEngine;

namespace Game.Entities.Character
{
    public interface IProjectileTarget
    {
        bool IsAlive { get; }
        Vector3 AimPosition { get; }

        void ApplyDamage(float damage);
    }
}
