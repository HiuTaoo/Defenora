using _Script.BT.Node;
using UnityEngine;

public class PanicFleeActionNode : BTActionNode
{
    private readonly Builder builder;
    private float _panicTimer;
    private bool _hasDestination;

    public PanicFleeActionNode(Unit unit) : base(unit)
    {
        builder = unit as Builder;
    }

    public override BTStatus Tick()
    {
        if (!builder.isPanicking)
        {
            ResetNodeData();
            return BTStatus.Failure;
        }

        _panicTimer += Time.deltaTime;

        if (_panicTimer >= 5f)
        {
            var enemyStillAround = false;
            var hits = Physics2D.OverlapCircleAll(builder.transform.position, builder.viewDistance);

            foreach (var hit in hits)
                if (hit != null && hit.CompareTag("Enemy"))
                {
                    enemyStillAround = true;
                    break;
                }

            if (!enemyStillAround)
            {
                builder.characterMovement.RequestStopMoving();
                builder.ResetState();
                builder.GetBT()?.ClearState();

                ResetNodeData();
                return BTStatus.Success;
            }

            _panicTimer = 0f;
            _hasDestination = false;
        }

        if (!_hasDestination || (builder.characterMovement != null && !builder.characterMovement.moving))
        {
            var currentGridPos = GraphNode.Instance.WorldToGridPos(builder.transform.position, builder.layerIndex);
            var randomOffset = new Vector3Int(Random.Range(-4, 5), Random.Range(-4, 5), 0);
            var targetGridPos = currentGridPos + randomOffset;

            var node = GraphNode.Instance.GetNode(targetGridPos, builder.layerIndex);
            if (node != null && node.isWalkable)
            {
                var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(currentGridPos, builder.layerIndex,
                    targetGridPos, builder.layerIndex);
                if (path != null && path.segments.Count > 0)
                {
                    builder.characterMovement.RequestStopMoving();
                    builder.MoveToTargetPosition(path);
                    _hasDestination = true;
                }
            }
        }

        builder.currentState = UnitState.Move;
        builder.animState = AnimState.Moving;

        return BTStatus.Running;
    }

    private void ResetNodeData()
    {
        _panicTimer = 0f;
        _hasDestination = false;
    }
}