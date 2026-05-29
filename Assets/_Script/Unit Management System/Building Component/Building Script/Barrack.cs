public class Barrack : TrainingBuilding
{
    protected override TrainingConfig[] GetUpgradeConfigs() => (configData as BarrackData)?.upgradeConfigs;
    protected override int GetMaxTraineeCapacity() => (configData as BarrackData)?.maxTraineeCapacity ?? 3;
}