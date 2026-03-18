using UnityEngine;

namespace _Script.ItemScript
{
    public interface IProjectile
    {
        void Init(Vector2 startPos, Vector2 shootDir);

        void OnHit(GameObject target);
        
        void SetDamage(float damage);

        void ResetProjectile();
    }
}