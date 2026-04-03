using UnityEngine;

namespace _Script.Unit_Management_System.Enemy
{
    public abstract class Enemy : CombatUnit
    {
        [Header("Enemy Info")]
        public Transform currentTarget;

        public virtual bool HasTarget()
        {
            return currentTarget != null;
        }

        public virtual void SetTarget(Transform target)
        {
            currentTarget = target;
        }

        public virtual void ClearTarget()
        {
            currentTarget = null;
        }
    }
}