using System;
using UnityEngine;

namespace _Script.Unit_Management_System.HealthComponent
{
    public class Health: MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 100f;
        
        public float CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth <= 0;

        public event Action<float, float> OnHealthChanged; 
        public event Action<float> OnTakeDamage;           
        public event Action OnDie;          
        

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return; 

            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

            OnTakeDamage?.Invoke(damage);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (IsDead)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void RepairBuilding(float amount)
        {
            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void SetCurrentHealth(float health)
        {
            CurrentHealth = health;
        }

        private void Die()
        {
            OnDie?.Invoke();
        }

        public bool IsFull()
        {
            return CurrentHealth == maxHealth;
        }
    }
}