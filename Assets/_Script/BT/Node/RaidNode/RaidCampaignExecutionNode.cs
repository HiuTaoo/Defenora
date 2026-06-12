using _Script.BT.Node;
using UnityEngine;

public class RaidCampaignExecutionNode : BTActionNode
{
    private RaidState currentState = RaidState.Assemble;
    private bool hasCalculatedPath;

    public RaidCampaignExecutionNode(Unit unit) : base(unit)
    {
    }

    public override BTStatus Tick()
    {
        if (RaidManager.Instance == null || RaidManager.Instance.activeRaidTarget == null)
        {
            ResetNode();
            return BTStatus.Failure;
        }

        var targetGate = RaidManager.Instance.activeRaidTarget;
        if (!targetGate.activeInHierarchy)
        {
            ResetNode();
            return BTStatus.Success;
        }

        if (RaidManager.Instance.isAssembleComplete && currentState == RaidState.Assemble)
        {
            currentState = RaidState.March;
            hasCalculatedPath = false;
            unit.StopMove();
        }

        if (currentState == RaidState.March)
        {
            var distanceToGate = Vector2.Distance(unit.transform.position, targetGate.transform.position);
            var leader = RaidManager.Instance.leaderUnit;

            var strategicStoppingDistance = unit.viewDistance;

            if (unit == leader)
            {
                strategicStoppingDistance = unit.attackRange > 0 ? unit.attackRange : 1.5f;
            }
            else
            {
                if (unit.unitType == UnitType.Warrior)
                {
                    strategicStoppingDistance = unit.attackRange > 0 ? unit.attackRange : 1.5f;
                }
                else if (unit.unitType == UnitType.Archer)
                {
                    strategicStoppingDistance = unit.attackRange * 0.85f;
                }
                else if (unit.unitType == UnitType.Monk)
                {
                    var archerRange = 5f;
                    strategicStoppingDistance = archerRange + 2.0f;
                }
            }

            if (distanceToGate <= strategicStoppingDistance)
            {
                unit.StopMove();
                hasCalculatedPath = false;

                return BTStatus.Success;
            }
        }

        switch (currentState)
        {
            case RaidState.Assemble:
                if (RaidManager.Instance.leaderUnit == null) return BTStatus.Failure;

                if (unit == RaidManager.Instance.leaderUnit)
                {
                    if (unit.characterMovement.moving) unit.StopMove();
                    unit.currentState = UnitState.Idle;
                    unit.animState = AnimState.Idle;
                }
                else
                {
                    var distToLeader = Vector2.Distance(unit.transform.position,
                        RaidManager.Instance.leaderUnit.transform.position);

                    if (distToLeader > 2.0f)
                    {
                        if (!hasCalculatedPath || !unit.characterMovement.moving)
                        {
                            var leaderGrid = Vector3Int.FloorToInt(RaidManager.Instance.leaderUnit.transform.position);
                            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                                Vector3Int.FloorToInt(unit.transform.position), unit.layerIndex,
                                leaderGrid, RaidManager.Instance.leaderUnit.layerIndex);

                            if (path != null && path.segments.Count > 0)
                            {
                                unit.MoveToTargetPosition(path);
                                hasCalculatedPath = true;
                            }
                        }
                    }
                    else
                    {
                        if (unit.characterMovement.moving) unit.StopMove();
                        unit.currentState = UnitState.Idle;
                        unit.animState = AnimState.Idle;
                    }
                }

                return BTStatus.Running;

            case RaidState.March:
                if (!hasCalculatedPath || !unit.characterMovement.moving)
                {
                    var marchPath = unit.FindBestPathToTarget(targetGate, unit.currentTargetLayerIndex);
                    if (marchPath != null && marchPath.segments.Count > 0)
                    {
                        unit.MoveToTargetPosition(marchPath);
                        hasCalculatedPath = true;
                    }
                }

                return BTStatus.Running;
        }

        return BTStatus.Running;
    }

    public override void ClearState()
    {
        base.ClearState();
        ResetNode();
    }

    private void ResetNode()
    {
        currentState = RaidState.Assemble;
        hasCalculatedPath = false;
    }
}