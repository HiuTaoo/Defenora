using _Script.Object_Pooling;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.ItemScript
{
    public class DynamiteProjectile : MonoBehaviour, IProjectile
    {
        [Header("Projectile Settings")]
        public float damage = 20f;
        public float speed = 5f;
        public float arcHeight = 2f;         
        public float rotationSpeed = 360f;   
        public float explosionRadius = 1.5f; 
        private LayerMask enemyLayer;         

        private Vector2 startPosition;
        private Vector2 targetPosition;
        
        private float flightTime;
        private float elapsedTime;
        private bool isExploded;
        
        private Animator animator;

        private void Awake()
        {
            enemyLayer = LayerMask.GetMask("NPC", "Building", "Player");
            animator = GetComponent<Animator>();
        }

        public void Init(Vector2 startPos, Vector2 targetPos, float damage)
        {
            transform.position = startPos;
            startPosition = startPos;
            targetPosition = targetPos;

            isExploded = false;
            elapsedTime = 0f;

            float distance = Vector2.Distance(startPos, targetPos);
            flightTime = distance / speed;

            if (flightTime <= 0.01f)
            {
                flightTime = 0.01f;
            }
        }

        public void SetDamage(float dmg)
        {
            damage = dmg;
        }

        private void Update()
        {
            if (isExploded) return;

            MoveAndRotate();
        }

        private void MoveAndRotate()
        {
            elapsedTime += Time.deltaTime;

            float percent = elapsedTime / flightTime;

            if (percent >= 1f)
            {
                transform.position = targetPosition;
                Explode();
                return;
            }

            Vector2 currentPos = Vector2.Lerp(startPosition, targetPosition, percent);
            
            currentPos.y += arcHeight * Mathf.Sin(percent * Mathf.PI);

            transform.position = currentPos;

            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        private void Explode()
        {
            isExploded = true;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Building") || hit.CompareTag("NPC") || hit.CompareTag("Player")) 
                {
                    OnHit(hit.gameObject);
                }
            }

            animator.Play("Explosion");
        }

        public void OnHit(GameObject target)
        {
            var health = target.GetComponentInChildren<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            var isPlayer = target.CompareTag("Player");
            if (isPlayer)
                HandlePlayerHit(target.transform.position);
            
        }

        public void ResetProjectile()
        {
            PoolManager.Instance.Despawn(gameObject);
        }

        private void HandlePlayerHit(Vector3 playerPosition)
        {
            if (WalletManager.Instance == null)
            {
                Debug.LogError("[Dynamite] Không tìm thấy WalletManager.Instance để trừ vàng của Player!");
                return;
            }

            WalletManager.Instance.ForceSpendCoins(1);

            var coinObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.goldBagPrefab, playerPosition,
                Quaternion.identity);
            if (coinObj != null && coinObj.TryGetComponent(out Item coinItem))
                coinItem.StartDrop(playerPosition, transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}