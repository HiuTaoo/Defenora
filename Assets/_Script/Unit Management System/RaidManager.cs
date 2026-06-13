using System.Collections.Generic;
using System.Linq;
using _Script.UI.UI_Script;
using UnityEngine;

public class RaidManager : MonoBehaviour
{
    public static RaidManager Instance { get; private set; }

    [Header("Raid Status")] public GameObject activeRaidTarget;
    public Unit leaderUnit;
    public bool isAssembleComplete;
    public RaidState raidState;
    public bool IsRaidActive => activeRaidTarget != null && activeRaidTarget.activeInHierarchy;

    public List<Unit> raidSubscribedUnits = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.OnInteractButtonPressed += HandlePlayerRaidCommand;
    }

    private void OnDisable()
    {
        if (PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.OnInteractButtonPressed -= HandlePlayerRaidCommand;
    }

    private void HandlePlayerRaidCommand(GameObject targetObj, InteractButtonState state)
    {
        if (state == InteractButtonState.Attack && targetObj != null && targetObj.CompareTag("SpawnPoint"))
        {
            ConfirmDialog.Instance.Show(
                "Do you want to initiate an Raid? " +
                "This can bring many risks, please consider carefully before starting",
                () =>
                {
                    AudioManager.Instance.PlaySFX(SoundNames.SfxTing);
                    LaunchRaid(targetObj);
                    AudioManager.Instance.PlayMusic(SoundNames.BattleTheme);
                },
                () => { }
            );
        }
    }

    private void LaunchRaid(GameObject spawnPointTarget)
    {
        activeRaidTarget = spawnPointTarget;
        isAssembleComplete = false;

        raidState = RaidState.Assemble;

        leaderUnit = null;
        raidSubscribedUnits.Clear();

        var spawnPointComponent = spawnPointTarget.GetComponent<SpawnPoint>();
        var targetLayer = spawnPointComponent != null ? spawnPointComponent.layerIndex : 0;

        var allUnits = UnitManager.Instance.allUnits;

        var warriors = allUnits.OfType<Warrior>().ToList();
        raidSubscribedUnits.AddRange(warriors);

        var archers = allUnits.OfType<Archer>().ToList();
        foreach (var archer in archers)
        {
            if (archer.isStationed)
                continue;
            raidSubscribedUnits.Add(archer);
        }

        var monks = allUnits.OfType<Monk>().ToList();
        var halfMonkCount = Mathf.CeilToInt(monks.Count / 2f);
        for (var i = 0; i < halfMonkCount; i++) raidSubscribedUnits.Add(monks[i]);

        if (raidSubscribedUnits.Count == 0) return;

        var minDistanceToGate = float.MaxValue;
        var activeWarriors = raidSubscribedUnits.OfType<Warrior>().ToList();

        if (activeWarriors.Count > 0)
            foreach (var warrior in activeWarriors)
            {
                var dist = Vector2.Distance(warrior.transform.position, spawnPointTarget.transform.position);
                if (dist < minDistanceToGate)
                {
                    minDistanceToGate = dist;
                    leaderUnit = warrior;
                }
            }
        else
            foreach (var unit in raidSubscribedUnits)
            {
                var dist = Vector2.Distance(unit.transform.position, spawnPointTarget.transform.position);
                if (dist < minDistanceToGate)
                {
                    minDistanceToGate = dist;
                    leaderUnit = unit;
                }
            }

        foreach (var unit in raidSubscribedUnits)
        {
            unit.currentTarget = spawnPointTarget.transform;
            unit.currentTargetLayerIndex = targetLayer;
            unit.isAlerted = true;
            unit.aggroTimer = 9999f;

            if (unit is Warrior w) w.warriorBlackBoard.detectedEnemy = spawnPointTarget;
            if (unit is Archer a) a.archerBlackBoard.detectedEnemy = spawnPointTarget;
            if (unit is Monk m) m.monkBlackBoard.detectedEnemy = spawnPointTarget;

            unit.GetBT()?.ClearState();
        }

        if (leaderUnit != null)
            Debug.LogWarning(
                $"[Raid System] 👑 Chọn [{leaderUnit.unitType}] {leaderUnit.unitName} làm Trung tâm tập kết! Toàn quân bắt đầu tìm đường hội quân...");
    }

    private void CheckAssembleProgress()
    {
        if (isAssembleComplete || leaderUnit == null) return;

        var currentAliveUnits = raidSubscribedUnits
            .Where(u => u != null && u.gameObject.activeInHierarchy && u.currentState != UnitState.Dead).ToList();

        if (currentAliveUnits.Count <= 1)
        {
            isAssembleComplete = true;
            raidState = RaidState.March;
            Debug.LogWarning(
                "[Raid System] 🚩 Không có lính đi kèm hoặc lính chết hết, Trưởng đoàn ĐỒNG LOẠT XUẤT PHÁT HÀNH QUÂN ĐƠN ĐỘC!");
            return;
        }

        var arrivedCount = 0;

        var assembleRadius = 3.0f; 

        foreach (var unit in currentAliveUnits)
            if (unit == leaderUnit || Vector2.Distance(unit.transform.position, leaderUnit.transform.position) <=
                assembleRadius)
                arrivedCount++;

        var assembleRatio = (float)arrivedCount / currentAliveUnits.Count;

        var requiredRatio = 0.85f; 

        if (assembleRatio >= requiredRatio)
        {
            isAssembleComplete = true;
            raidState = RaidState.March;

            Debug.LogWarning(
                $"[Raid System] 🚩 Đội hình đã tập hợp đạt {assembleRatio * 100:F0}% (Yêu cầu: {requiredRatio * 100}%). ĐỒNG LOẠT XUẤT PHÁT HÀNH QUÂN!");
        }
    }

    private void TerminateCurrentRaid()
    {
        PlayTheme();
  
        foreach (var unit in raidSubscribedUnits)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy) continue;

            unit.StopMove();
            unit.currentTarget = null;
            unit.currentTargetLayerIndex = -1;
            unit.isAlerted = false;
            unit.aggroTimer = 0f;

            if (unit is Warrior w) w.warriorBlackBoard.detectedEnemy = null;
            if (unit is Archer a) a.archerBlackBoard.detectedEnemy = null;
            if (unit is Monk m)
            {
                m.monkBlackBoard.lowHPAlly = null;
                m.monkBlackBoard.aoeHealTargets.Clear();
            }

            unit.currentState = UnitState.Idle;
            unit.animState = AnimState.Idle;

            unit.GetBT()?.ClearState();
        }

        activeRaidTarget = null;
        leaderUnit = null;
        isAssembleComplete = false;
        raidSubscribedUnits.Clear();
    }

    private void PlayTheme()
    {
        if(TimeOfDaySystem.Instance.IsNightTime())
            AudioManager.Instance.PlayMusic(SoundNames.NightTheme);
        else
            AudioManager.Instance.PlayMusic(SoundNames.DayTheme);
    }

    private void Update()
    {
        if (activeRaidTarget != null && !activeRaidTarget.activeInHierarchy)
        {
            TerminateCurrentRaid();
            return;
        }

        if (activeRaidTarget != null && raidSubscribedUnits.Count > 0)
        {
            var aliveUnitsCount = 0;
            foreach (var unit in raidSubscribedUnits)
                if (unit != null && unit.gameObject.activeInHierarchy && unit.currentState != UnitState.Dead)
                    aliveUnitsCount++;

            if (aliveUnitsCount == 0)
            {
                Debug.LogError(
                    "[Raid System] ❌ THẤT BẠI! Toàn bộ quân lực tham chiến đã tử trận! Giải tán chiến dịch...");

                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification(
                        "Raid Failed! All your units have fallen in battle!", NotificationColorType.Warning);

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundNames.SfxWarning);

                TerminateCurrentRaid();
                return;
            }
        }

        if (activeRaidTarget != null && !isAssembleComplete)
            CheckAssembleProgress();
    }
}