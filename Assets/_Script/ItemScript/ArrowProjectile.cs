using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.ItemScript
{
    public class ArrowProjectile : MonoBehaviour, IProjectile, IPoolable
    {
        [Header("Projectile Settings")] public float damage;
        public float speed = 5f;
        public float lifeTime = 3f;

        [Tooltip("Thời gian chờ bốc hơi sau khi cắm vào mục tiêu")]
        public float hitDelay = 0.1f; 
        
        private Vector2 direction;
        private float hitTimer;
        private bool isHit;
        private Vector2 lastPosition;
        private float lifeTimer;

        private Collider2D projectileCollider;
        private int combinedTargetLayerMask; 

        private void Awake()
        {
            projectileCollider = GetComponent<Collider2D>();
            combinedTargetLayerMask = LayerMask.GetMask("NPC", "SpawnPoint");
        }

        private void Update()
        {
            if (isHit)
            {
                HandleHitState();
                return;
            }

            Move();
            HandleLifeTime();
        }

        public void Init(Vector2 startPos, Vector2 shootDir, float damage)
        {
            SetDamage(damage);
            transform.position = startPos;

            direction = shootDir.normalized;
            lastPosition = startPos;

            SetRotation(direction);
        }

        public void SetDamage(float dmg)
        {
            damage = dmg;
        }

        public void OnHit(GameObject target)
        {
            if (isHit) return; 

            isHit = true;
            hitTimer = 0f;

            if (projectileCollider != null) projectileCollider.enabled = false;

            var health = target.GetComponentInChildren<Health>();
            if (health == null) return;
                
            health.TakeDamage(damage);
        }

        public void ResetProjectile()
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }

        private void Move()
        {
            var start = lastPosition;
            var end = start + direction * (speed * Time.deltaTime);

            var hit = Physics2D.Linecast(start, end, combinedTargetLayerMask);

            if (hit.collider != null)
            {
                if (hit.collider.isTrigger) return;

                if (hit.collider.CompareTag("NPC"))
                {
                }
                else if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Animal") || hit.collider.CompareTag("SpawnPoint"))
                {
                    transform.position = hit.point;
                    lastPosition = hit.point;

                    OnHit(hit.collider.gameObject);
                    return; 
                }
            }
            transform.position = end;
            lastPosition = end;
        }

        private void HandleLifeTime()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifeTime) ResetProjectile();
        }

        private void HandleHitState()
        {
            hitTimer += Time.deltaTime;
            if (hitTimer >= hitDelay) ResetProjectile();
        }

        private void SetRotation(Vector2 dir)
        {
            if (dir == Vector2.zero) return;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnSpawned()
        {
            lifeTimer = 0f;
            hitTimer = 0f;
            isHit = false;

            if (projectileCollider != null)
                projectileCollider.enabled = true;

            gameObject.SetActive(true);
        }

        public void OnDespawned()
        {
            if (projectileCollider != null)
                projectileCollider.enabled = false;

            direction = Vector2.zero;
            gameObject.SetActive(false);
        }
    }
}