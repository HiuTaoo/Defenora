using System;
using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.Node.BuilderNode.RepairStructure;
using _Script.BT.Node.EnemyNode;
using _Script.BT.Node.EnemyNode.TorchGoblinNode;
using _Script.BT.Node.EnemyNode.unitNode;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.Unit_Management_System.HealthComponent;
using UnityEditor;
using UnityEngine;

namespace _Script.Unit_Management_System.Enemy
{
    public class TorchGoblin: global::Unit
    {
        [Header("Torch Goblin Stat")] 
        public EnemyDirection enemyDirection;
        
        [Header("Combat Layers")]
        public int attackLayerMask;

        protected override void Awake()
        {
            base.Awake();
            unitType = UnitType.TorchGoblin;
            bt = CreateBehaviourTree(this);
            attackLayerMask = LayerMask.GetMask("NPC", "Building", "Player");
        }

        protected override void Update()
        {
            base.Update();
            /*if (currentTarget == null)
            {
                var building = FindNearestBuilding(transform.position);
                if (building != null)
                {
                    currentTarget = building.transform;
                    currentTargetLayerIndex = building.layerIndex;
                }
            }*/
            bt?.Tick();
            UpdateDirection();
            animFSM.ChangeState(currentState, animState);
            UpdateFaceToTarget();
        }

        #region BT
        private BehaviourTree CreateBehaviourTree(TorchGoblin torchGoblin)
        {
            var huntAndAttackSequence = new SequenceNode(
                new FindNearestTargetNode(torchGoblin),
                new TorchGoblinMoveToTargetNode(torchGoblin),
                new SelectorNode(
                    new SequenceNode(
                        new HasPlayerInTorchGoblinAttackRangeNode(torchGoblin),
                        new EnemyAttackPlayerNode(torchGoblin)
                    ),
                    new SequenceNode(
                        new HasNPCInEnemyAttackRangeNode(torchGoblin),
                        new TorchGoblinAttackNPCNode(torchGoblin)
                    ),
                    new EnemyAttackBuildingNode(torchGoblin)
                ),
                
                new ClearBuildingTargetNode(torchGoblin) 
            );

            var backToSpawnPointSequence = new SequenceNode(
                new IsDawnNode(torchGoblin),
                new ResetStateNode(torchGoblin),
                new MoveToSpawnPointNode(torchGoblin),
                new DespawnNode(torchGoblin));

            var root = new SelectorNode( 
                backToSpawnPointSequence,
                new SequenceNode(        
                    new IsNightStartNode(torchGoblin),
                    huntAndAttackSequence     
                )
            );

            return new BehaviourTree(root);
        }

        #endregion

        #region Method
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
            var maxAllowableAngle = viewAngle / 2f;

            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, damageRadius, results, attackLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCol = results[i];
                if (hitCol == null || hitCol.isTrigger || hitCol.gameObject == gameObject) continue;

                var targetGO = hitCol.gameObject;
                if (targetGO.CompareTag("Enemy")) continue;

                var isPlayer = targetGO.CompareTag("Player");
                var targetHealth = hitCol.GetComponentInChildren<Health>();

                if (!isPlayer && (targetHealth == null || targetHealth.CurrentHealth <= 0)) continue;

                if (hitCol.TryGetComponent(out global::Building building) &&
                    building.buildingState != BuildingState.Completed) continue;

                var closestPoint = hitCol.ClosestPoint(transform.position);
                var dirToTarget = (closestPoint - (Vector2)transform.position).normalized;

                if (Vector2.Angle(facingDir, dirToTarget) > maxAllowableAngle) continue;

                if (isPlayer)
                    HandlePlayerHit(hitCol.transform.position);
                else
                    targetHealth.TakeDamage(attackDamage);
                
                results[i] = null;
            }

            AudioManager.Instance.PlaySFX3D(SoundNames.SfxLancerHit, audioSource);
        }

        private void HandlePlayerHit(Vector3 playerPosition)
        {
            if (WalletManager.Instance == null)
            {
                Debug.LogError("[TorchGoblin] Không tìm thấy WalletManager.Instance để trừ vàng của Player!");
                return;
            }

            WalletManager.Instance.ForceSpendCoins(1);

            var coinObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.coinPrefab, playerPosition,
                Quaternion.identity);
            if (coinObj != null && coinObj.TryGetComponent(out Coin coin))
            {
                coin.StartDrop(transform.position, layerIndex);
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

        public bool CheckPlayerInAttackRange()
        {
            var player = DetectPlayer(attackRange, GetCurrentFacingVector());
            return player != null;
        }
        
        #endregion

        public override void UseSpecialAbility()
        {
            throw new NotImplementedException();
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


            Handles.color = new Color(1f, 0f, 0f, 0.2f); 
            Handles.DrawSolidArc(
                origin,
                Vector3.forward, 
                Quaternion.Euler(0, 0, -viewAngle / 2) * direction,
                viewAngle,
                damageRange
            );


            Gizmos.color = Color.red;

            Vector3 leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * direction;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * direction;

            Gizmos.DrawLine(origin, origin + leftBoundary * damageRange);
            Gizmos.DrawLine(origin, origin + rightBoundary * damageRange);
        
        }
#endif
    }
}