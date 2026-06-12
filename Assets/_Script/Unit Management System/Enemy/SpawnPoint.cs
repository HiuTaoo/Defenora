using System.Collections;
using System.Collections.Generic;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPoint : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    public int layerIndex;
    public float spawnDelay = 1f;

    [Header("Spawn Settings")]
    [Tooltip("Danh sách các loại quái vật ĐƯỢC PHÉP xuất hiện tại cổng này")]
    public List<GameObject> allowedEnemyPrefabs = new List<GameObject>();

    [Header("Self-Defense Settings")] [Tooltip("Số lượng quái sinh ra để bảo vệ mặc định ở ngày 1")] [SerializeField]
    private int baseDefenseSpawnCount = 5;

    [Tooltip("Cứ sau bao nhiêu ngày thì độ khó tự vệ sẽ tăng lên một bậc")] [SerializeField]
    private int daysToIncrementDifficulty = 3;

    [Tooltip("Số lượng quái cộng thêm sau mỗi lần tăng bậc độ khó")] [SerializeField]
    private int spawnMultiplierPerStep = 3;

    public float currentHealth;
    public bool isAttacked;

    private Health health;
    private float _lastHealthMilestone;
    private float _activationThreshold;

    private Coroutine _regularSpawnCoroutine;
    private Coroutine _defenseSpawnCoroutine;
    private Coroutine _aggroTimeoutCoroutine;

    private void Awake()
    {
        health = GetComponentInChildren<Health>();
        if (health == null) health = gameObject.AddComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDie += HandleGateDestroyed;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDie -= HandleGateDestroyed;
        }
    }

    private void Update()
    {
        if (health != null) currentHealth = health.CurrentHealth;
    }

    public void OnSpawned()
    {
        if (health != null)
        {
            health.SetMaxHealth(health.maxHealth, true);
            currentHealth = health.maxHealth;
        }

        ResetHealthMilestones();
        isAttacked = false;

        Debug.Log($"[{gameObject.name}] 🟢 OnSpawned: Cổng quái đã được nạp lại dữ liệu máu và mốc tự vệ hoàn chỉnh.");
    }

    public void OnDespawned()
    {
        if (_regularSpawnCoroutine != null)
        {
            StopCoroutine(_regularSpawnCoroutine);
            _regularSpawnCoroutine = null;
        }

        if (_defenseSpawnCoroutine != null)
        {
            StopCoroutine(_defenseSpawnCoroutine);
            _defenseSpawnCoroutine = null;
        }

        if (_aggroTimeoutCoroutine != null)
        {
            StopCoroutine(_aggroTimeoutCoroutine);
            _aggroTimeoutCoroutine = null;
        }

        Debug.Log($"[{gameObject.name}] 🔴 OnDespawned: Đã dọn dẹp sạch toàn bộ Coroutine của cổng quái.");
    }

    public void ResetHealthMilestones()
    {
        if (health != null)
        {
            _lastHealthMilestone = health.maxHealth;
            _activationThreshold = health.maxHealth * 0.30f;

            Debug.Log(
                $"[{gameObject.name}] 🛡️ Đã đồng bộ mốc tự vệ: Máu tối đa = {_lastHealthMilestone}, Ngưỡng kích hoạt = {_activationThreshold}");
        }
    }

    private void HandleHealthChanged(float newHealth, float maxHealth)
    {
        if (newHealth < currentHealth)
        {
            isAttacked = true;

            if (_aggroTimeoutCoroutine != null) StopCoroutine(_aggroTimeoutCoroutine);
            _aggroTimeoutCoroutine = StartCoroutine(AggroTimeoutRoutine(10f));
        }

        var healthLostSinceLastMilestone = _lastHealthMilestone - newHealth;

        if (healthLostSinceLastMilestone >= _activationThreshold && newHealth > 0)
        {
            var currentDay = 1;
            if (TimeOfDaySystem.Instance != null) currentDay = TimeOfDaySystem.Instance.CurrentDay;

            var currentDifficultyStep = (currentDay - 1) / daysToIncrementDifficulty;

            var defenseSpawnCount = baseDefenseSpawnCount + currentDifficultyStep * spawnMultiplierPerStep;

            Debug.LogWarning(
                $"[SpawnPoint] 🚨 Cổng quái [{gameObject.name}] bị mất 30% máu vào Ngày {currentDay}! " +
                $"Cấp độ khó hiện tại: {currentDifficultyStep} -> Triệu hồi {defenseSpawnCount} viện binh tự vệ tốc độ cao!");

            _lastHealthMilestone = newHealth;

            if (_defenseSpawnCoroutine != null) StopCoroutine(_defenseSpawnCoroutine);
            _defenseSpawnCoroutine = StartCoroutine(SpawnDefenseSequenceRoutine(defenseSpawnCount));
        }
    }

    private IEnumerator AggroTimeoutRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        isAttacked = false;
        _aggroTimeoutCoroutine = null;

        Debug.Log(
            $"[SpawnPoint] 🛡️ Cổng quái [{gameObject.name}] đã yên bình trở lại sau {duration} giây không dính sát thương.");
    }

    private void HandleGateDestroyed()
    {
        Debug.Log($"[SpawnPoint] 💥 Cổng quái [{gameObject.name}] đã bị phá hủy hoàn toàn!");

        if (SpawnManager.Instance != null && SpawnManager.Instance.spawnPoints.Contains(this))
            SpawnManager.Instance.spawnPoints.Remove(this);

        SpawnManager.Instance.RemoveSpawnPoint(this);
        PoolManager.Instance.Despawn(gameObject);
    }

    public void OrderSpawnRandomly(int count)
    {
        if (allowedEnemyPrefabs == null || allowedEnemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] ⚠️ Không có Prefab quái nào được gán!");
            return;
        }

        if (_regularSpawnCoroutine != null) StopCoroutine(_regularSpawnCoroutine);
        _regularSpawnCoroutine = StartCoroutine(SpawnRandomSequenceRoutine(count));
    }

    private IEnumerator SpawnRandomSequenceRoutine(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var randomIndex = Random.Range(0, allowedEnemyPrefabs.Count);
            var chosenPrefab = allowedEnemyPrefabs[randomIndex];

            if (chosenPrefab != null)
                SpawnObject(chosenPrefab);

            yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log($"[{gameObject.name}] ⚔️ Cổng đã hoàn thành sinh {count} quái vật theo lệnh đêm.");
        _regularSpawnCoroutine = null;
    }

    private IEnumerator SpawnDefenseSequenceRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, allowedEnemyPrefabs.Count);
            GameObject chosenPrefab = allowedEnemyPrefabs[randomIndex];

            if (chosenPrefab != null)
            {
                SpawnObject(chosenPrefab);
            }

            yield return new WaitForSeconds(spawnDelay * 0.3f); 
        }

        _defenseSpawnCoroutine = null;
    }

    private void SpawnObject(GameObject prefab)
    {
        var enemy = PoolManager.Instance.Spawn(prefab, transform.position, Quaternion.identity);
        var unit = enemy.GetComponent<Unit>();

        unit.characterMovement.CurrentLayer = layerIndex;
        unit.enemySpawnPoint = transform.gameObject;

        UnitManager.Instance.RegisterUnit(unit);
        Debug.Log($"Spawn Enemy {enemy.name}");
    }
}