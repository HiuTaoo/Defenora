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
using UnityEngine;

namespace _Script.Unit_Management_System.Enemy
{
    public class TNTGoblin: global::Unit
    {
        [Header("Torch Goblin Stat")] 
        public EnemyDirection enemyDirection;

        public GameObject subTarget;

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
            var attackBuildingSequence = new SequenceNode(
                new FindNearestTargetNode(tntGoblin),
                new TNTGoblinMoveToTargetBuildingNode(tntGoblin), 
                new TNTGoblinAttack(tntGoblin),        
                new ClearBuildingTargetNode(tntGoblin)  
            );
            
            var attackNPCSequence = new SequenceNode(
                new HasNPCInTNTGoblinAttackRangeNode(tntGoblin),                 
                new TNTGoblinAttackNPCNode(tntGoblin)           
            );
            
            var backToSpawnPointSequence = new SequenceNode(
                new IsDawnNode(tntGoblin),
                new ResetStateNode(tntGoblin), 
                new MoveToSpawnPointNode(tntGoblin), 
                new DespawnNode(tntGoblin));

            var root = new SelectorNode(
                backToSpawnPointSequence,
                new SequenceNode(
                    new IsNightStartNode(tntGoblin),
                    new SelectorNode(
                        attackNPCSequence,
                        attackBuildingSequence)));
            
            return new  BehaviourTree(root);
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
                if (subTarget != null && subTarget.activeInHierarchy)
                {
                    dynamiteComp.Init(transform.position, subTarget.transform.position, attackDamage);
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
        }

    
#endif
    }
}