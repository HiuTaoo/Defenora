using _Script.Object_Pooling;

public class Monastery : TrainingBuilding
{
    protected override TrainingConfig[] GetUpgradeConfigs()
    {
        var data = configData as MonasteryData;
        if (data == null) return null;

        return new TrainingConfig[] {
            new TrainingConfig {
                targetType = UnitType.Monk,
                trainingDurationInGameHours = data.trainingDurationInGameHours,
                unitPrefab = PrefabConfig.Instance.monkPrefab
            }
        };
    }
    protected override int GetMaxTraineeCapacity() => (configData as MonasteryData)?.maxTraineeCapacity ?? 2;
}