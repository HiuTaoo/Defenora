using _Script.BT.Node;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class IsAllyNeedHealNode : BTActionNode
{
    private readonly Monk monk;

    public IsAllyNeedHealNode(Unit unit) : base(unit)
    {
        monk = unit as Monk;
    }

    public override BTStatus Tick()
    {
        if (monk == null) return BTStatus.Failure;

        var layerMask = LayerMask.GetMask("NPC");
        var size = Physics2D.OverlapCircleNonAlloc(monk.transform.position, monk.viewDistance, monk.results, layerMask);

        for (var i = 0; i < size; i++)
        {
            var hit = monk.results[i];
            if (hit == null || hit.gameObject == monk.gameObject || hit.CompareTag("Enemy")) continue;

            var allyHealth = hit.GetComponentInChildren<Health>();
            if (allyHealth == null || allyHealth.CurrentHealth <= 0) continue;

            if (allyHealth.CurrentHealth < allyHealth.maxHealth)
            {
                monk.monkBlackBoard.lowHPAlly = hit.gameObject;
                return BTStatus.Success;
            }
        }

        monk.monkBlackBoard.lowHPAlly = null;
        return BTStatus.Failure;
    }
}