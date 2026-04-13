using System;
using _Script.BT;
using _Script.BT.Node.EnemyNode;
using _Script.BT.Node.EnemyNode.TorchGoblinNode;
using _Script.Enum;
using _Script.Unit_Management_System.HealthComponent;
using UnityEditor;
using UnityEngine;

namespace _Script.Unit_Management_System.Enemy
{
    public class TorchGoblin: Unit
    {
        [Header("Torch Goblin Stat")] 
        public EnemyDirection enemyDirection;
        public float viewDistance = 5f;
        [Range(0, 360)]
        public float viewAngle;
        public float attackDamage = 10f;
        public float attackRange = 2f;
        
        [Header("Combat Layers")]
        public int attackLayerMask;

        protected override void Awake()
        {
            base.Awake();
            unitType = UnitType.TorchGoblin;
            bt = CreateBehaviourTree(this);
            attackLayerMask = LayerMask.GetMask("NPC", "Building");
        }

        protected override void Update()
        {
            base.Update();
            if (currentTarget == null)
            {
                var building = FindNearestBuilding(transform.position);
                if (building != null)
                {
                    currentTarget = building.transform;
                    currentTargetLayerIndex = building.layerIndex;
                }
            }
            bt?.Tick();
            UpdateDirection();
            animFSM.ChangeState(currentState, animState);
            UpdateFaceToTarget();
        }

        #region BT
        private BehaviourTree CreateBehaviourTree(TorchGoblin torchGoblin)
        {
            
            var attackBuildinSequence = new SequenceNode(
                new HasTargetBuildingNode(torchGoblin),
                new MoveToTargetBuildingNode(torchGoblin), 
                new TorchGoblinAttack(torchGoblin),        
                new ClearBuildingTargetNode(torchGoblin)  
            );

            var attackNPCSequence = new SequenceNode(
                new HasNPCInAttackRangeNode(torchGoblin),                 
                new AttackNPCNode(torchGoblin)           
            );
            
            var root = new SelectorNode(
                attackNPCSequence,
                attackBuildinSequence);

            return new BehaviourTree(root);
        }
        #endregion

        #region Method

        public bool CheckTargetBuildingInAttackRange()
        {
            if(currentTarget == null)
                return false;
            
            var building = currentTarget;

            var buildingCol = building.GetComponent<Collider2D>();
            if (buildingCol == null || buildingCol.isTrigger)
                return false;

            Vector2 closest = buildingCol.ClosestPoint(transform.position);

            float dist = Vector2.Distance(transform.position, closest);

            return dist <= (attackRange * 0.75);
        }
        
        public EnemyDirection GetDirection(Vector2 from, Vector2 to)
        {
            var dir = to - from;

            if (dir.sqrMagnitude < 0.0001f)
                return EnemyDirection.None;

            dir.Normalize();

            var dirRight = new Vector2(Mathf.Abs(dir.x), dir.y);

            var angle = Vector2.Angle(Vector2.down, dirRight);

            return angle switch
            {
                <= 45f => EnemyDirection.Down,   
                <= 135f => EnemyDirection.Right, 
                _ => EnemyDirection.Up           
            };
        }

        public void UpdateDirection()
        {
            if (currentTarget == null)
                return;
            var dir = GetDirection(transform.position,
                currentTarget.transform.position);
            enemyDirection = dir;
            animFSM.SetEnemyDirection(enemyDirection);
        }
        
        public bool IsInAttackPosition()
        {
            if (currentTarget == null) 
                return false;

            var buildingFP = currentTarget.GetComponent<ObjectFootprint>();
            if (buildingFP == null) 
                return false;

            Vector3Int targetGridPos = Vector3Int.FloorToInt(currentTarget.transform.position);
            targetGridPos.z = 0;

            Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
            currentGridPos.z = 0;

            var attackOffsets = BuildOrthogonalPerimeterOffsets(buildingFP);

            foreach (var offset in attackOffsets)
            {
                Vector3Int validAttackPos = targetGridPos + offset;
                validAttackPos.z = 0; 

                if (currentGridPos == validAttackPos)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateFaceToTarget()
        {
            if(currentTarget == null)
                return;
            var dir = - transform.position + currentTarget.transform.position;
            UpdateFacing(dir);
        }
        
        public void DealDamage()
        {
            float damageRadius = attackRange;
            Vector2 facingDir = GetCurrentFacingVector();

            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, damageRadius, results, attackLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCol = results[i];

                if (hitCol.gameObject == gameObject) continue;

                if (hitCol.isTrigger) continue;

                if (hitCol.gameObject.CompareTag("Enemy")) continue;

                var targetHealth = hitCol.GetComponentInChildren<Health>();
        
                if (targetHealth == null || targetHealth.CurrentHealth <= 0) continue;

                if (hitCol.TryGetComponent<global::Building>(out var building))
                {
                    if (building.buildingState != BuildingState.Completed) continue;
                }

                Vector2 closest = hitCol.ClosestPoint(transform.position);
                Vector2 dirToTarget = (closest - (Vector2)transform.position).normalized;

                float angle = Vector2.Angle(facingDir, dirToTarget);

                if (angle <= (viewAngle / 2f))
                {
                    targetHealth.TakeDamage(attackDamage);
            
                    results[i] = null; 
                }
            }
        }

        public Vector2 GetCurrentFacingVector()
        {
            if (enemyDirection == EnemyDirection.Up) 
                return Vector2.up;
            
            if (enemyDirection == EnemyDirection.Down) 
                return Vector2.down;

            return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }
        
        public bool CheckNPCInAttackRange()
        {
            var npcs = DetectNPCs(attackRange, GetCurrentFacingVector());
            return npcs.Count > 0;
        }
        
        
        #endregion

        public override void UseSpecialAbility()
        {
            throw new NotImplementedException();
        }

        private void OnDrawGizmos()
        {
            // 1. Vẽ vùng nhìn (Màu vàng)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewDistance);

            // 2. Vẽ vùng tấn công (Màu đỏ)
            DrawAttackCone();
        }

        private void DrawAttackCone()
        {
            Vector3 origin = transform.position;
            float damageRange = attackRange;
        
            Vector3 direction = Vector3.right;
            if (enemyDirection == EnemyDirection.Up) direction = Vector3.up;
            else if (enemyDirection == EnemyDirection.Down) direction = Vector3.down;
            else direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;

#if UNITY_EDITOR
            Handles.color = new Color(1f, 0f, 0f, 0.2f); 
            Handles.DrawSolidArc(
                origin,
                Vector3.forward, 
                Quaternion.Euler(0, 0, -viewAngle / 2) * direction,
                viewAngle,
                damageRange
            );
#endif

            Gizmos.color = Color.red;

            Vector3 leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * direction;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * direction;

            Gizmos.DrawLine(origin, origin + leftBoundary * damageRange);
            Gizmos.DrawLine(origin, origin + rightBoundary * damageRange);
        
        }
    }
}