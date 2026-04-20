
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.ItemScript
{
    public class ArrowProjectile : MonoBehaviour, IProjectile
    {
        private Vector2 direction;

        [Header("Projectile Settings")]
        public float damage;
        public float speed = 5f;
        public float lifeTime = 3f;
        public float hitDelay = 0.5f;

        private float lifeTimer;
        private float hitTimer;

        private Vector2 lastPosition;
        private bool isHit;

        public void Init(Vector2 startPos, Vector2 shootDir, float damage)
        {
            SetDamage(damage);
            transform.position = startPos;

            direction = shootDir.normalized;
            lastPosition = startPos;

            SetRotation(direction);

            lifeTimer = 0f;
            hitTimer = 0f;
            isHit = false;
        }

        public void SetDamage(float dmg)
        {
            damage = dmg;
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

        private void Move()
        {
            Vector2 start = lastPosition;
            Vector2 end = start + direction * speed * Time.deltaTime;

            RaycastHit2D hit = Physics2D.Linecast(start, end);

            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                transform.position = hit.point;
                OnHit(hit.collider.gameObject);
                return;
            }

            transform.position = end;
            lastPosition = end;
        }

        private void HandleLifeTime()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifeTime)
            {
                ResetProjectile();
            }
        }

        private void HandleHitState()
        {
            hitTimer += Time.deltaTime;
            if (hitTimer >= hitDelay)
            {
                ResetProjectile();
            }
        }

        public void OnHit(GameObject target)
        {
            isHit = true;
            hitTimer = 0f;
            GetComponent<Collider2D>().enabled = false;

            var health = target.GetComponentInChildren<Health>();
            if (health == null)
                return;
            health.TakeDamage(damage);
        }

        public void ResetProjectile()
        {
            PoolManager.Instance.Despawn(gameObject);
        }

        private void SetRotation(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}