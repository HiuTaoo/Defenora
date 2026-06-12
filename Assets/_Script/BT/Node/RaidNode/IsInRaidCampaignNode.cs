using _Script.BT.Node;

public class IsInRaidCampaignNode : BTActionNode
{
    public IsInRaidCampaignNode(Unit unit) : base(unit)
    {
    }

    public override BTStatus Tick()
    {
        if (RaidManager.Instance != null &&
            RaidManager.Instance.activeRaidTarget != null &&
            RaidManager.Instance.raidSubscribedUnits.Contains(unit))
            return BTStatus.Success;
        return BTStatus.Failure;
    }
}