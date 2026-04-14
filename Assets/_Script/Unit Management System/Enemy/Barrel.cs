using System.Collections;
using _Script.BT;
using _Script.BT.Node.EnemyNode;
using _Script.BT.Node.EnemyNode.BarrelNode;
using _Script.BT.Node.EnemyNode.unitNode;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.Unit_Management_System.Enemy
{
    public class Barrel : Unit
    {
        [Header("Explosion Settings")]
        public float explosionDamage = 50f;
        public float explosionRadius = 2.5f;
        public float fuseTime = 3f; 
        public LayerMask damageLayerMask; 
        
        [Header("Detection Settings")]
        public float viewDistance = 5f; // Tầm nhìn quét nhà xung quanh
        public float triggerRange = 0.5f;

        private bool isExploding = false;
        private Animator animator;

        protected override void Awake()
        {
            base.Awake();
            bt = CreateBehaviourTree(this);
            unitType = UnitType.Barrel; 
            damageLayerMask = LayerMask.GetMask("Building", "NPC");
            animator = GetComponent<Animator>();
            animator.Play("In");
        }

        protected override void Update()
        {
            base.Update();
            bt?.Tick();
            animFSM.ChangeState(currentState, animState);
        }

        #region Behaviour Tree

        private BehaviourTree CreateBehaviourTree(Barrel barrel)
        {
            var attackBuildingSequence = new SequenceNode(
                new FindNearestBuildingNode(barrel),     
                new BarrelMoveToTargetBuildingNode(barrel),  
                new BarrelExplodeNode(barrel)            
            );

            var root = new SequenceNode(
                new IsNightStartNode(barrel),            
                attackBuildingSequence                  
            );
    
            return new BehaviourTree(root);
        }

        #endregion
        
        #region Logic Quét Mục Tiêu
        public bool CheckTargetBuildingInAttackRange()
        {
            if (currentTarget == null || !currentTarget.CompareTag("Building")) return false;
            
            var buildingCol = currentTarget.GetComponent<Collider2D>();
            if (buildingCol == null || buildingCol.isTrigger) return false;

            Vector2 closestPoint = buildingCol.ClosestPoint(transform.position);
            float dist = Vector2.Distance(transform.position, closestPoint);

            return dist <= triggerRange;
        }

        public bool CheckAndSetCloserBuildingTarget()
        {
            int size = Physics2D.OverlapCircleNonAlloc(transform.position, viewDistance, results);
            bool targetChanged = false;

            float currentDist = float.MaxValue;
            if (currentTarget != null)
            {
                var col = currentTarget.GetComponent<Collider2D>();
                if (col != null)
                    currentDist = Vector2.Distance(transform.position, col.ClosestPoint(transform.position));
                else
                    currentDist = Vector2.Distance(transform.position, currentTarget.position);
            }

            Transform bestTarget = currentTarget;

            for (int i = 0; i < size; i++)
            {
                var hit = results[i];
                if (hit != null && hit.CompareTag("Building"))
                {
                    var building = hit.GetComponent<global::Building>();
                    if (building == null || building.buildingState == BuildingState.Destroyed) continue;

                    if (hit.transform == currentTarget) continue;

                    float distToNewBuilding = Vector2.Distance(transform.position, hit.ClosestPoint(transform.position));

                    if (distToNewBuilding < currentDist - 0.5f)
                    {
                        bestTarget = hit.transform;
                        currentDist = distToNewBuilding;
                        targetChanged = true;
                    }
                }
            }

            if (targetChanged)
            {
                currentTarget = bestTarget;
                var b = currentTarget.GetComponent<global::Building>();
                if (b != null) currentTargetLayerIndex = b.layerIndex;
                return true;
            }

            return false;
        }
        #endregion

        public override void UseSpecialAbility()
        {
            TriggerExplosion();
        }

        protected override void HandleDeath()
        {
            var col = GetComponent<Collider2D>();
            col.enabled = false;
            
            if (!isExploding)
            {
                TriggerExplosion();
            }
            this.enabled = false;
        }

        public void TriggerExplosion()
        {
            if (isExploding) return;
            isExploding = true;

            if (health != null) health.enabled = false;

            StopMove();

            currentState = UnitState.Defend; 
            animState = AnimState.Defending;      
            animFSM.ChangeState(currentState, animState);

            StartCoroutine(WaitAndAttackRoutine());
        }

        public void Explosion()
        {
            var col = GetComponent<Collider2D>();
            col.enabled = false;
            
            if (!isExploding)
            {
                TriggerExplosion();
            }
            this.enabled = false;
        }

        private IEnumerator WaitAndAttackRoutine()
        {
            yield return new WaitForSeconds(fuseTime);

            currentState = UnitState.Attack; 
            animState = AnimState.Attacking;   
            animFSM.ChangeState(currentState, animState);
            
        }

        public void Explode()
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, explosionRadius, results, damageLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCol = results[i];
                
                if (hitCol == null || hitCol.gameObject == gameObject || hitCol.isTrigger) continue;

                if (hitCol.CompareTag("Building") || hitCol.CompareTag("NPC") || hitCol.CompareTag("Player"))
                {
                    var targetHealth = hitCol.GetComponentInChildren<Health>();
                    if (targetHealth != null && targetHealth.CurrentHealth > 0)
                    {
                        targetHealth.TakeDamage(explosionDamage);
                    }
                }
            }
        }

#if UNITY_EDITOR
        // Vẽ vùng sát thương màu đỏ trong Scene View để dễ căn chỉnh
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
#endif
    }
}