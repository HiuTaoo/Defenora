using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.Node.BuilderNode.RepairStructure;
using _Script.BT.Node.EnemyNode;
using _Script.BT.Node.EnemyNode.TNTGoblinNode;
using _Script.BT.Node.EnemyNode.TNTtntGoblinNode;
using _Script.BT.Node.EnemyNode.TorchGoblinNode;
using _Script.BT.Node.EnemyNode.unitNode;
using _Script.Enum;
using _Script.ItemScript;
using _Script.Object_Pooling;
using UnityEditor;
using UnityEngine;

namespace _Script.Unit_Management_System.Enemy
{
    public class TNTGoblin: global::Unit
    {
        [Header("Torch Goblin Stat")] 
        public EnemyDirection enemyDirection;


        protected override void Awake()
        {
            base.Awake();
            unitType = UnitType.TNTGoblin;
            bt = CreateBehaviourTree(this);
        }

        protected override void Update()
        {
            base.Update();
            bt?.Tick();
            UpdateDirection();
            animFSM.ChangeState(currentState, animState);
            UpdateFaceToTarget();
        }

        private BehaviourTree CreateBehaviourTree(TNTGoblin tntGoblin)
        {
            var huntAndAttackSequence = new SequenceNode(
                new FindNearestTargetNode(tntGoblin),
                new TNTGoblinMoveToTargetNode(tntGoblin),
                new SelectorNode(
                    new SequenceNode(
                        new HasPlayerInTNTGoblinAttackRangeNode(tntGoblin),
                        new TNTGoblinAttackPlayerNode(tntGoblin)
                    ),
                    new SequenceNode(
                        new HasNPCInEnemyAttackRangeNode(tntGoblin),
                        new TNTGoblinAttackNPCNode(tntGoblin)
                    ),
                    new EnemyAttackBuildingNode(tntGoblin)
                ),
                new ClearBuildingTargetNode(tntGoblin) 
            );
            
            var backToSpawnPointSequence = new SequenceNode(
                new IsDawnNode(tntGoblin),
                new ResetStateNode(tntGoblin),
                new MoveToSpawnPointNode(tntGoblin),
                new DespawnNode(tntGoblin));

            // ROOT CỦA CÂY BEHAVIOR TREE
            var root = new SelectorNode(
                backToSpawnPointSequence,
                new SequenceNode(
                    new IsNightStartNode(tntGoblin),
                    huntAndAttackSequence
                )
            );

            return new BehaviourTree(root);
        }

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
        
        public bool CheckAndSetNearbyBuildingTarget()
        {
            int size = Physics2D.OverlapCircleNonAlloc(transform.position, attackRange, results);

            for (int i = 0; i < size; i++)
            {
                var hit = results[i];
                if (hit != null && hit.CompareTag("Building"))
                {
                    var building = hit.GetComponent<global::Building>();
                    
                    if (building == null || building.buildingState != BuildingState.Completed)
                        continue;

                    Vector2 closestPoint = hit.ClosestPoint(transform.position);
                    float dist = Vector2.Distance(transform.position, closestPoint);

                    if (dist <= (attackRange * 0.75f)) 
                    {
                        currentTarget = hit.transform;
                        currentTargetLayerIndex = building.layerIndex; 
                        return true;
                    }
                }
            }
            return false;
        }
        
        public bool CheckNPCInAttackRange()
        {
            var npcs = DetectNPCs(attackRange, GetCurrentFacingVector());
            return npcs.Count > 0;
        }
        
        private void UpdateFaceToTarget()
        {
            if(currentTarget == null)
                return;
            var dir = - transform.position + currentTarget.transform.position;
            UpdateFacing(dir);
        }
        
        public Vector2 GetCurrentFacingVector()
        {
            if (enemyDirection == EnemyDirection.Up) 
                return Vector2.up;
            
            if (enemyDirection == EnemyDirection.Down) 
                return Vector2.down;

            return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
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
        

        #endregion

        public override void UseSpecialAbility()
        {
            var dynamite = PoolManager.Instance.Spawn(PrefabConfig.Instance.dynamitePrefab, transform.position, Quaternion.identity);
            var dynamiteComp = dynamite.GetComponent<DynamiteProjectile>();
    
            if (dynamiteComp != null)
            {
                if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
                {
                    dynamiteComp.Init(transform.position, currentTarget.transform.position, attackDamage);
                }
                else if (currentTarget != null)
                {
                    dynamiteComp.Init(transform.position, currentTarget.transform.position, attackDamage);
                }
            }
        }
        
        public override List<(string name, string value)> GetSpecialStats()
        {
            var extraStats = new List<(string name, string value)>();
        
            extraStats.Add(("Attack Damage", attackDamage.ToString(CultureInfo.InvariantCulture))); 
            extraStats.Add(("Attack CD", attackCooldown.ToString(CultureInfo.InvariantCulture))); 
            
            return extraStats;
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            DrawAttackCone();
        }

        private void DrawAttackCone()
        {
            var origin = transform.position;
            var damageRange = attackRange;

            var direction = Vector3.right;
            if (enemyDirection == EnemyDirection.Up) direction = Vector3.up;
            else if (enemyDirection == EnemyDirection.Down) direction = Vector3.down;
            else direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;


            Handles.color = new Color(1f, 0f, 0f, 0.2f);
            Handles.DrawSolidArc(
                origin,
                Vector3.forward,
                Quaternion.Euler(0, 0, -viewAngle / 2) * direction,
                viewAngle,
                damageRange
            );


            Gizmos.color = Color.red;

            var leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * direction;
            var rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * direction;

            Gizmos.DrawLine(origin, origin + leftBoundary * damageRange);
            Gizmos.DrawLine(origin, origin + rightBoundary * damageRange);
        }
    
#endif
    }
}