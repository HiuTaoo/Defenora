using System;
using UnityEngine;

namespace _Script.Unit_Management_System.HealthComponent
{
    public class Health: MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 1f;
        
        public float CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth <= 0;
        
        private UnitStatsManager statsManager;

        public event Action<float, float> OnHealthChanged; 
        public event Action<float> OnTakeDamage;           
        public event Action OnDie;          
        

        private void Awake()
        {
            statsManager = transform.parent.GetComponentInChildren<UnitStatsManager>();
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
            CurrentHealth = Mathf.Clamp(health, 0f, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void Die()
        {
            OnDie?.Invoke();
        }

        public bool IsFull()
        {
            return CurrentHealth == maxHealth;
        }
        
        private void OnEnable()
        {
            if (statsManager != null)
            {
                statsManager.OnLevelUp += HandleLevelUp; 
            }
        }

        private void OnDisable()
        {
            if (statsManager != null)
            {
                statsManager.OnLevelUp -= HandleLevelUp; 
            }
        }

        private void HandleLevelUp()
        {
            SetMaxHealth(statsManager.MaxHealth, true);
        }
        
        public void SetMaxHealth(float newMaxHealth, bool refillHealth)
        {
            maxHealth = newMaxHealth;

            if (refillHealth)
            {
                CurrentHealth = maxHealth;
            }
            else
            {
                CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
            }

            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}