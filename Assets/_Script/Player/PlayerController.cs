using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, ISaveable
{
    public static PlayerController Instance { get; private set; }
    
    [Header("Builder Info")]
    public float moveSpeed = 3.5f;

    public Rigidbody2D rb;
    public Animator animator;
    public AgentPhysics2D agentPhysics2D;
    public CircleCollider2D builderCollider;
    public CharacterMovement characterMovement;
    public FloorAgent floorAgent;

    [Header("Player Unstuck System")] [SerializeField]
    private float stuckCheckInterval = 5.0f;

    private Vector3 _lastPosition;
    private float _stuckTimer; 

    public Vector2 MovementInput { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        builderCollider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
        floorAgent = GetComponentInChildren<FloorAgent>();
    }

    private void Start()
    {
        _lastPosition = transform.position;
    }

    private void Update()
    {
        HandleInput();
        HandlePlayerUnstuck(); 
    }

    private void FixedUpdate()
    {
        Move();
        HandleDirection();
    }

    private void HandleInput()
    {
        MovementInput = GameManager.Instance.gameContext.InputManager.GetMovementInput();
    }

    public int GetCurrentLayerIndex()
    {
        var flootAgent = gameObject.GetComponentInChildren<FloorAgent>();
        return flootAgent != null ? flootAgent.currentFloorIndex : 0;
    }

    public void Move()
    {
        Vector2 input = MovementInput;

        if (input.sqrMagnitude < 0.01f)
        {
            rb.velocity = Vector2.zero;
            animator.Play("Idle");
            return;
        }

        Vector2 currentPosition = rb.position;
        Vector2 direction = input.normalized;
        float moveDistance = moveSpeed * Time.fixedDeltaTime;

        bool isBlocked = agentPhysics2D.IsBlock(currentPosition, direction, moveDistance + 0.05f, GetComponent<CircleCollider2D>());

        if (!isBlocked && GameManager.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
        {
            animator.Play("Walk");
            Vector2 newPosition = currentPosition + direction * moveDistance;
            rb.MovePosition(newPosition);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void HandleDirection()
    {
        Vector2 velocity = MovementInput * moveSpeed;

        if (velocity.sqrMagnitude > 0.01f)
        {
            if (velocity.x < 0f)
            {
                Vector3 scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else if (velocity.x > 0f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }

    // ======================================================================
    // 🟢 HÀM XỬ LÝ CHỐNG KẸT LƯỚI CHO PLAYER 
    // ======================================================================
    private void HandlePlayerUnstuck()
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
            var currentLayer = GetCurrentLayerIndex();
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
                $"[Player Unstuck] Phát hiện Player đứng im tại ô CẤM ĐI {currentGridPos} quá {stuckCheckInterval}s! Tiến hành loang tìm ô trống...");

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

                Debug.Log(
                    $"[Player Unstuck Thành Công] Đã giải cứu Player ra khỏi vùng kẹt sang ô trống: {bestTargetGrid}");
            }
            else
            {
                Debug.LogError(
                    $"[Player Unstuck Thất Bại] Đã quét rộng đến {maxRadiusSearch} ô nhưng không tìm được vị trí trống nào giải cứu Player!");
            }
        }
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        saveData.playerPosition = transform.position;
        saveData.playerLayerIndex = GetCurrentLayerIndex();
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        transform.position = saveData.playerPosition;
        if (rb != null)
        {
            rb.position = saveData.playerPosition;
        }

        var floorAgent = gameObject.GetComponentInChildren<FloorAgent>();
        var characterMovement = gameObject.GetComponentInChildren<CharacterMovement>();
        if (characterMovement != null)
        {
            characterMovement.CurrentLayer = saveData.playerLayerIndex;
        }
        if (floorAgent != null)
        {
            floorAgent.MoveToFloor(saveData.playerLayerIndex);
        }

        _lastPosition = transform.position;
        _stuckTimer = 0f;
    }
}