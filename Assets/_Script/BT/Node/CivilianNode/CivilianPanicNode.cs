using _Script.Unit_Management_System.UnitScript;
using UnityEngine;

namespace _Script.BT.Node.CivilianNode
{
    public class CivilianPanicNode : BTActionNode
    {
        public Civilian civilian;
        private float _panicTimer;
        private bool _hasDestination;

        public CivilianPanicNode(Unit unit) : base(unit)
        {
            civilian = unit as Civilian;
        }

        public override BTStatus Tick()
        {
            if (!civilian.isPanicking)
            {
                ResetNodeData();
                return BTStatus.Failure;
            }

            _panicTimer += Time.deltaTime;

            if (_panicTimer >= 5f)
            {
                var enemyStillAround = false;
                var hits = Physics2D.OverlapCircleAll(civilian.transform.position, civilian.viewDistance);

                foreach (var hit in hits)
                    if (hit != null && hit.CompareTag("Enemy"))
                    {
                        enemyStillAround = true;
                        break;
                    }

                if (!enemyStillAround)
                {
                    Debug.Log(
                        $"[{civilian.gameObject.name}] 🛡️ Đã an toàn sau 5s chạy trốn. Tiến hành giải phóng cây BT!");

                    civilian.StopMove();
                    civilian.ResetState();
                    civilian.GetBT()?.ClearState();

                    ResetNodeData();
                    return BTStatus.Success;
                }

                _panicTimer = 0f;
                _hasDestination = false;
            }

            if (!_hasDestination || (civilian.characterMovement != null && !civilian.characterMovement.moving))
            {
                var currentGridPos =
                    GraphNode.Instance.WorldToGridPos(civilian.transform.position, civilian.layerIndex);
                
                var randomOffset = new Vector3Int(Random.Range(-4, 5), Random.Range(-4, 5), 0);
                var targetGridPos = currentGridPos + randomOffset;

                var node = GraphNode.Instance.GetNode(targetGridPos, civilian.layerIndex);
                if (node != null && node.isWalkable)
                {
                    var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(currentGridPos, civilian.layerIndex,
                        targetGridPos, civilian.layerIndex);
                        
                    if (path != null && path.segments.Count > 0)
                    {
                        civilian.MoveToTargetPosition(path);
                        _hasDestination = true;
                    }
                    else
                    {
                        _hasDestination = false;
                    }
                }
            }

            civilian.currentState = UnitState.Move;
            civilian.animState = AnimState.Moving;

            return BTStatus.Running;
        }

        private void ResetNodeData()
        {
            _panicTimer = 0f;
            _hasDestination = false;
        }
    }
}