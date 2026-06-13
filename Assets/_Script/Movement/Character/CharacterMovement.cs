using System.Collections;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 3.5f;

    private FloorAgent floorAgent;
    private AgentPhysics2D agentPhysics2D;
    public Rigidbody2D rb;
    private CircleCollider2D circleCollider2D;
    private Camera cam;
    public PathFinding currentPath = null;
    public Coroutine moveCoroutine;
    public bool stopRequested = false;

    public bool moving = false;
    public Vector2 direction { get; private set; } = Vector2.zero;
    private int _currentLayer;

    public int CurrentLayer
    {
        get => _currentLayer;
        set
        {
            _currentLayer = value;
            UpdateLayerIndex();
        }
    }

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        floorAgent = GetComponent<FloorAgent>();
        agentPhysics2D = GetComponent<AgentPhysics2D>();
        circleCollider2D = GetComponentInParent<CircleCollider2D>();
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    private void Update()
    {
        /*if (Input.GetMouseButtonDown(0) && GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
        {
            MoveByMouse();
        }*/
    }

    public void MoveByMouse()
    {
        if (!cam.gameObject.activeInHierarchy)
            return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        if (GraphNode.Instance == null)
        {
            Debug.LogError("GraphNode.Instance is NULL!");
            return;
        }

        int layerCount = GraphNode.Instance.layerDatas.Length;
        int layer = 0;
        bool canMove = false;

        var targetPosition = Vector3Int.zero;

        for (int i = layerCount - 1; i >= 0; i--)
        {
            var calculatedGrid = GraphNode.Instance.WorldToGridPos(mouseWorldPos, i);
            
            var graph = GraphNode.Instance.layerGraphs[i];
            if (graph.nodes.TryGetValue(calculatedGrid, out var node))
            {
                if (node != null && node.isWalkable)
                {
                    targetPosition = calculatedGrid;
                    layer = i;
                    canMove = true;
                    break;
                }
            }
        }

        if (canMove)
        {
            MoveToPosition(targetPosition, layer);
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy vị trí hợp lệ để di chuyển tới tại tọa độ thế giới: {mouseWorldPos}");
        }
    }

    public void MoveToPosition(Vector3Int position, int layer)
    {
        var gridPosition = GraphNode.Instance.WorldToGridPos(transform.position, floorAgent.currentFloorIndex);

        currentPath = PathfindingAlgorithm.Instance.FindMultiLayerPath(gridPosition, floorAgent.currentFloorIndex, position, layer);

        StopAllCoroutines();
        moveCoroutine = StartCoroutine(FollowPathCoroutine(currentPath));
    }
    
    public IEnumerator FollowPathCoroutine(PathFinding path)
    {
        if (path == null || path.segments == null || path.segments.Count == 0)
        {
            moving = false;
            yield break;
        }

        stopRequested = false;
        moving = true;

        var unit = transform.parent.GetComponent<Unit>();
        if (unit != null)
            unit.currentState = UnitState.Move;

        foreach (var segment in path.segments)
        {
            for (int i = 0; i < segment.positions.Count; i++)
            {
                var tilePos = segment.positions[i];

                var targetCenter = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, transform.parent.position.z);

                while (unit.currentState == UnitState.Move && (transform.parent.position - targetCenter).sqrMagnitude > 0.01f)
                {
                    if (stopRequested)
                    {
                        moving = false;
                        currentPath = null;
                        direction = Vector2.zero;
                        moveCoroutine = null;

                        if (unit != null && unit.currentState == UnitState.Move)
                            unit.currentState = UnitState.Idle;

                        yield break;
                    }
                    
                    var dir = ((Vector2)targetCenter - rb.position).normalized;
                    direction = dir;
                    HandleFlip(dir);

                    if (agentPhysics2D.IsBuilding(
                            transform.parent.position,
                            direction,
                            0.01f,
                            moveSpeed,
                            circleCollider2D))
                    {
                        moving = false;

                        if (unit != null)
                            unit.currentState = UnitState.Idle;

                        currentPath = null;
                        moveCoroutine = null;
                        yield break;
                    }

                    Vector2 nextPosition = Vector2.MoveTowards(
                        rb.position,
                        targetCenter,
                        moveSpeed * Time.fixedDeltaTime);
                    
                    rb.MovePosition(nextPosition);
                    if(!_currentLayer.Equals(segment.layerIndex))
                        _currentLayer = segment.layerIndex;
                    yield return null;
                }
            }
        }

        moving = false;
        currentPath = null;
        direction = Vector2.zero;
        moveCoroutine = null;

        if (unit != null && unit.currentState == UnitState.Move)
            unit.currentState = UnitState.Idle;
    }
    
    public void MoveTo(Vector2 targetPosition)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveToPositionCoroutine(targetPosition));
    }

    private IEnumerator MoveToPositionCoroutine(Vector2 targetPosition)
    {
        moving = true;

        while ((rb.position - targetPosition).sqrMagnitude > 0.01f)
        {
            Vector2 direction = (targetPosition - rb.position).normalized;
            HandleFlip(direction);

            Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);

            yield return new WaitForFixedUpdate();
        }

        moving = false;
        moveCoroutine = null;
    }

    private void HandleFlip(Vector2 direction)
    {
        if (direction.x < 0)
        {
            Vector3 scale = transform.parent.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.parent.localScale = scale;
        }
        else if (direction.x > 0)
        {
            Vector3 scale = transform.parent.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.parent.localScale = scale;
        }
    }

    public void HandleFlipByPosition(Vector3 targetPosition)
    {
        Vector3 currentPosition = transform.parent.position;

        float deltaX = targetPosition.x - currentPosition.x;

        if (Mathf.Abs(deltaX) < 0.01f)
            return; 

        Vector3 scale = transform.parent.localScale;

        if (deltaX < 0)
            scale.x = -Mathf.Abs(scale.x);
        else
            scale.x = Mathf.Abs(scale.x);

        transform.parent.localScale = scale;
    }
    
    public void UpdateLayerIndex()
    {
        if(CurrentLayer != floorAgent.currentFloorIndex) 
            floorAgent.MoveToFloor(CurrentLayer);
    }
    
    public void StopMoving()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        currentPath = null;

        moving = false;
        direction = Vector2.zero;
    }
    
    public void RequestStopMoving()
    {
        stopRequested = true;
    }
}