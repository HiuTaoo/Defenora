using System.Collections;
using System.Collections.Generic;
using _Script.Object_Pooling;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;
// Cần thiết để sử dụng List cho việc gom ô loang
using Random = UnityEngine.Random;

public abstract class Animal : MonoBehaviour, IPoolable
{
    protected CircleCollider2D animalCollider2D;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected AgentPhysics2D agentPhysics2D;
    [HideInInspector] public FloorAgent floorAgent;
    [HideInInspector] public Health health;
    [HideInInspector] public SpriteRenderer spriteRenderer;

    [Header("Animal Info")]
    public AnimalType animalType;
    public int layerIndex;
    public bool isDangerous = false;
    public Vector2 runDirection;
    public float currentHealth;

    [Header("Animal Settings")]
    public float alertDistance = 5f;
    public int maxDuration = 15;
    public float runSpeed = 5f;
    public float panicTime = 3f;

    [Header("Unstuck System")] [SerializeField]
    private float stuckCheckInterval = 5.0f;

    private Vector3 _lastPosition;
    private float _stuckTimer;

    protected Coroutine panicCoroutine;
    protected System.Random random;
    protected Coroutine randomAnimationCoroutine;
    protected Coroutine checkDangerCoroutine;

    protected virtual void Awake()
    {
        animalCollider2D = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        floorAgent = GetComponentInChildren<FloorAgent>();
        random = new System.Random();
        health = GetComponentInChildren<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDie += HandleDeath;
        }
    }

    protected virtual void Update()
    {
        currentHealth = health.CurrentHealth;
        layerIndex = floorAgent.currentFloorIndex;

        HandleAnimalUnstuck();
    }


    #region Random Animation Loop
    protected virtual IEnumerator RandomAnimationLoop()
    {
        while (!isDangerous)
        {
            int waitTime = random.Next(3, maxDuration);
            yield return new WaitForSeconds(waitTime);

            int nextState = random.Next(0, 2);
            switch (nextState)
            {
                case 0:
                    animator.Play("Idle");
                    break;
                case 1:
                    animator.Play("Eat");
                    break;
            }
        }

        randomAnimationCoroutine = null;
    }
    #endregion

    #region Panic Run
    public virtual void StartPanicRun()
    {
        if (panicCoroutine == null)
        {
            float angle = Random.Range(0f, 360f);
            runDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

            panicCoroutine = StartCoroutine(PanicRunTimer());
        }
    }

    protected virtual IEnumerator PanicRunTimer()
    {
        animator.Play("Run");
        yield return new WaitForSeconds(panicTime);
        isDangerous = false;
        panicCoroutine = null;
    }

    protected virtual void PanicMove()
    {
        if (!isDangerous) return;

        Vector2 currentPosition = rb.position;
        float moveDistance = runSpeed * Time.fixedDeltaTime;

        bool isBlocked = agentPhysics2D.IsBlock(currentPosition, runDirection, moveDistance + 0.05f, animalCollider2D);

        if (!isBlocked)
        {
            Vector2 newPosition = currentPosition + runDirection * moveDistance;
            rb.MovePosition(newPosition);
        }
        else
        {
            float angle = Random.Range(0f, 360f);
            runDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        }
    }

    protected virtual void HandleFlipDirection()
    {
        if (runDirection.x < 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (runDirection.x > 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
    #endregion

    #region Check Dangerous Zone
    protected virtual IEnumerator CheckDangerLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(0.3f);

        while (true)
        {
            if (!isDangerous)
                CheckDangerousZone();

            yield return wait;
        }
    }

    protected virtual void CheckDangerousZone()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, alertDistance);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                isDangerous = true;
                return;
            }
        }
    }

    public void AlertNearbyAnimals()
    {
        var results = new Collider2D[15];

        var size = Physics2D.OverlapCircleNonAlloc(transform.position, alertDistance, results);

        for (var i = 0; i < size; i++)
        {
            var hit = results[i];

            if (hit != null && hit.gameObject != gameObject && hit.CompareTag("Animal"))
            {
                var neighborAnimal = hit.GetComponent<Animal>();
                if (neighborAnimal != null && !neighborAnimal.isDangerous) neighborAnimal.isDangerous = true;
            }
        }
    }
    #endregion

    private void HandleAnimalUnstuck()
    {
        if (GraphNode.Instance == null) return;

        if (Vector3.Distance(transform.position, _lastPosition) > 0.05f)
        {
            _lastPosition = transform.position;
            _stuckTimer = 0f;
            return;
        }

        _stuckTimer += Time.deltaTime;

        if (_stuckTimer >= stuckCheckInterval)
        {
            var currentLayer = layerIndex;
            var gridX = Mathf.FloorToInt(transform.position.x);
            var gridY = Mathf.FloorToInt(transform.position.y);
            var currentGridPos = new Vector3Int(gridX, gridY, 0);

            var currentNode = GraphNode.Instance.GetNode(currentGridPos, currentLayer);

            if (currentNode != null && currentNode.isWalkable)
            {
                _stuckTimer = 0f;
                _lastPosition = transform.position;
                return;
            }

            _stuckTimer = 0f;
            _lastPosition = transform.position;

            Debug.LogWarning(
                $"[Animal Unstuck] Phát hiện thú [{animalType}] {gameObject.name} kẹt tại ô cấm {currentGridPos} quá {stuckCheckInterval}s! Đang loang dịch chuyển khẩn cấp...");

            var targetFound = false;
            var bestTargetGrid = Vector3Int.zero;

            const int maxRadiusSearch = 5;
            for (var r = 1; r <= maxRadiusSearch; r++)
            {
                var candidatesAtRadius = new List<Vector3Int>();

                for (var xOffset = -r; xOffset <= r; xOffset++)
                for (var yOffset = -r; yOffset <= r; yOffset++)
                    if (Mathf.Abs(xOffset) == r || Mathf.Abs(yOffset) == r)
                    {
                        var checkPos = currentGridPos + new Vector3Int(xOffset, yOffset, 0);
                        var node = GraphNode.Instance.GetNode(checkPos, currentLayer);

                        if (node != null && node.isWalkable) candidatesAtRadius.Add(checkPos);
                    }

                if (candidatesAtRadius.Count > 0)
                {
                    candidatesAtRadius.Sort((a, b) =>
                        Vector3.Distance(transform.position, new Vector3(a.x + 0.5f, a.y + 0.5f, 0))
                            .CompareTo(Vector3.Distance(transform.position, new Vector3(b.x + 0.5f, b.y + 0.5f, 0)))
                    );

                    bestTargetGrid = candidatesAtRadius[0];
                    targetFound = true;
                    break;
                }
            }

            if (targetFound)
            {
                var targetWorldPos =
                    new Vector3(bestTargetGrid.x + 0.5f, bestTargetGrid.y + 0.5f, transform.position.z);

                transform.position = targetWorldPos;
                if (rb != null) rb.position = targetWorldPos;

                _lastPosition = targetWorldPos;

                if (isDangerous)
                {
                    isDangerous = false;
                    if (panicCoroutine != null) StopCoroutine(panicCoroutine);
                    panicCoroutine = null;
                    randomAnimationCoroutine = StartCoroutine(RandomAnimationLoop());
                }

                Debug.Log(
                    $"[Animal Unstuck Thành Công] Đã giải cứu [{animalType}] sang ô trống an toàn: {bestTargetGrid}");
            }
            else
            {
                Debug.LogError(
                    $"[Animal Unstuck Thất Bại] Đã quét rộng {maxRadiusSearch} ô quanh {currentGridPos} nhưng không tìm được ô trống nào cho thú!");
            }
        }
    }
    
    protected virtual void HandleDeath()
    {
        if (checkDangerCoroutine != null) StopCoroutine(checkDangerCoroutine);
        if (randomAnimationCoroutine != null) StopCoroutine(randomAnimationCoroutine);
        if (panicCoroutine != null) StopCoroutine(panicCoroutine);

        AlertNearbyAnimals();
        Die();

        if (animalCollider2D != null) animalCollider2D.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        ObjectSpawner.Instance.RegisterDeadAnimal(layerIndex);
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDie -= HandleDeath;
        }
    }
    
    public void Die()
    {
        InstaniateObject(PrefabConfig.Instance.meatPrefab,
            gameObject.transform.position, layerIndex, 1);
        var coinObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.coinPrefab, transform.position,
            Quaternion.identity);
        coinObj.GetComponent<Coin>().StartDrop(coinObj.transform.position, transform.position, layerIndex);
        
        PoolManager.Instance.Despawn(transform.gameObject);
    }
    
    public GameObject InstaniateObject(GameObject obj, Vector3 worldPosition, int currentLayerIndex, int amount)
    {
        var spawnedObj = PoolManager.Instance.Spawn(obj,
            worldPosition, Quaternion.identity);

        if (worldPosition.x > transform.position.x)
            spawnedObj.transform.localScale = new Vector3(-1, 1, 1);

        var itemComponent = spawnedObj.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.layerIndex = currentLayerIndex;
            itemComponent.amount = amount;
        }

        if (spawnedObj != null) itemComponent.StartDrop(worldPosition, transform.position);
        return spawnedObj;
    }

    public void OnSpawned()
    {
        if (health != null)
        {
            health.SetMaxHealth(health.maxHealth, true);
            currentHealth = health.maxHealth;
        }

        isDangerous = false;
        runDirection = Vector2.zero;
        enabled = true;

        if (animalCollider2D != null)
            animalCollider2D.enabled = true;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = false;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        StopAllAnimalCoroutines();

        if (gameObject.activeInHierarchy)
        {
            randomAnimationCoroutine = StartCoroutine(RandomAnimationLoop());
            checkDangerCoroutine = StartCoroutine(CheckDangerLoop());
        }

        _lastPosition = transform.position;
        _stuckTimer = 0f;

        if (RegionManager.Instance != null) RegionManager.Instance.RegisterObject(gameObject);
    }

    public void OnDespawned()
    {
        StopAllAnimalCoroutines();
        _stuckTimer = 0f;

        if (RegionManager.Instance != null) RegionManager.Instance.UnregisterObject(gameObject);
    }

    private void StopAllAnimalCoroutines()
    {
        if (panicCoroutine != null)
        {
            StopCoroutine(panicCoroutine);
            panicCoroutine = null;
        }

        if (randomAnimationCoroutine != null)
        {
            StopCoroutine(randomAnimationCoroutine);
            randomAnimationCoroutine = null;
        }

        if (checkDangerCoroutine != null)
        {
            StopCoroutine(checkDangerCoroutine);
            checkDangerCoroutine = null;
        }
    }
}

public enum AnimalType
{
    Sheep
}