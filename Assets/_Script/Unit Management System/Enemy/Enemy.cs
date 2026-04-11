using System;
using UnityEngine;

namespace _Script.Unit_Management_System.Enemy
{
    public abstract class Enemy : MonoBehaviour
    {
        [HideInInspector] public CharacterMovement characterMovement;
        [HideInInspector] public SpriteRenderer spriteRenderer;
        [HideInInspector] public FloorAgent floorAgent;
        
        [Header("Enemy Stat")]
        public float viewDistance = 6f;
        public float viewAngle = 180f;
        public float attackRange = 2f;
        public float attackAngle = 180f;
           
        [Header("Target Info")]
        public Transform currentTarget;
        
        public bool isAttacking { get; protected set; }
        public bool isInWindup { get; protected set; }
        
        private void Awake()
        {
            characterMovement = GetComponentInChildren<CharacterMovement>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            floorAgent = GetComponentInChildren<FloorAgent>();
        }

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
        
        public virtual void StartAttackSignal()
        {
            isAttacking = true;
            isInWindup = true;
        }

        public virtual void EndWindupSignal()
        {
            isInWindup = false;
        }

        public virtual void EndAttackSignal()
        {
            isAttacking = false;
            isInWindup = false;
        }
    }
}