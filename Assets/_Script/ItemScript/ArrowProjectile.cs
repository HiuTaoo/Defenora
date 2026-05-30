using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.ItemScript
{
    public class ArrowProjectile : MonoBehaviour, IProjectile
    {
        [Header("Projectile Settings")] public float damage;

        public float speed = 5f;
        public float lifeTime = 3f;
        public float hitDelay = 0.5f;
        private Vector2 direction;
        private float hitTimer;
        private bool isHit;

        private Vector2 lastPosition;

        private float lifeTimer;

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

            lifeTimer = 0f;
            hitTimer = 0f;
            isHit = false;
        }

        public void SetDamage(float dmg)
        {
            damage = dmg;
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

        private void Move()
        {
            var start = lastPosition;
            var end = start + direction * (speed * Time.deltaTime);

            int targetLayerMask = LayerMask.GetMask("NPC");

            var hit = Physics2D.Linecast(start, end, targetLayerMask);

            if (hit.collider != null && (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Animal")))
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
            if (lifeTimer >= lifeTime) ResetProjectile();
        }

        private void HandleHitState()
        {
            hitTimer += Time.deltaTime;
            if (hitTimer >= hitDelay) ResetProjectile();
        }

        private void SetRotation(Vector2 dir)
        {
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}