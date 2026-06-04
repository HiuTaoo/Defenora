using System.Collections.Generic;
using _Script.BT.Node;
using UnityEngine;

public class HasNPCInEnemyAttackRangeNode : BTActionNode
{
    public HasNPCInEnemyAttackRangeNode(Unit unit) : base(unit)
    {
    }

    public override BTStatus Tick()
    {
        if (unit.currentTarget != null &&
            unit.currentTarget.CompareTag("NPC") &&
            unit.currentTarget.gameObject.activeInHierarchy)
        {
            var dist = Vector2.Distance(unit.transform.position, unit.currentTarget.position);
            if (dist <= unit.attackRange) return BTStatus.Success;
        }

        var npcs = unit.DetectAllNPCsInRange(unit.attackRange);

        if (npcs != null && npcs.Count > 0)
        {
            var validNPCs = new List<GameObject>();

            foreach (var npcObj in npcs)
            {
                if (npcObj == null) continue;

                var npcComponent = npcObj.GetComponent<Unit>();

                if (npcComponent is Archer archer && archer.assignedBuilding != null) continue;

                validNPCs.Add(npcObj);
            }

            if (validNPCs.Count > 0)
            {
                var target = unit.SelectClosestTarget(validNPCs).transform;
                unit.currentTarget = target;
                return BTStatus.Success;
            }
        }

        return BTStatus.Failure;
    }
}