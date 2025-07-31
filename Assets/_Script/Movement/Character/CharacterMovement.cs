using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 3.5f;

    private Tilemap tilemap;
    private FloorAgent floorAgent;
    private AgentPhysics2D agentPhysics2D;
    public Rigidbody2D rb;
    private CircleCollider2D circleCollider2D;
    private Camera cam;
    public PathFinding currentPath = null;
    public Coroutine moveCoroutine;

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
        tilemap = GameObject.Find("Tilemap Null").GetComponent<Tilemap>();
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
        {
            MoveByMouse();
        }
    }

    public void MoveByMouse()
    {
        if (!cam.gameObject.activeInHierarchy)
            return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3Int targetPosition = Vector3Int.FloorToInt(mouseWorldPos);
        targetPosition.z = 0;

        if (GraphNode.Instance == null)
        {
            Debug.LogError("GraphNode.Instance is NULL!");
            return;
        }

        //Debug.Log($"Clicked world pos: {mouseWorldPos}, grid pos: {targetPosition}");

        int layerCount = GraphNode.Instance.layerDatas.Length;
        int layer = 0;
        bool canMove = false;

        for (int i = layerCount - 1; i >= 0; i--)
        {
            var graph = GraphNode.Instance.layerGraphs[i];
            if (graph.nodes.TryGetValue(targetPosition, out Node node))
            {
                if (node != null && node.isWalkable)
                {
                    layer = i;
                    canMove = true;
                    break;
                }
            }
        }

        if (canMove)
        {
            //Debug.Log($"Di chuyển tới: {targetPosition} ở tầng {layer}");
            MoveToPosition(targetPosition, layer);
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy vị trí hợp lệ để di chuyển tới: {targetPosition}");
        }
    }

    public void MoveToPosition(Vector3Int position, int layer)
    {
        Vector3 worldPosition = transform.position;
        Vector3Int gridPosition = Vector3Int.FloorToInt(worldPosition);
        gridPosition.z = 0;

        currentPath = PathfindingAlgorithm.Instance.FindMultiLayerPath(gridPosition, floorAgent.currentFloorIndex, position, layer);

        //currentPath.PrintPath();

        StopAllCoroutines();
        moveCoroutine = StartCoroutine(FollowPathCoroutine(currentPath));
    }


    public IEnumerator FollowPathCoroutine(PathFinding path)
    {
        if (path.totalCost == 0)
            yield return null;

        moving = true;

        foreach (var segment in path.segments)
        {
            foreach (var tilePos in segment.positions)
            {
                Vector3 targetCenter = tilemap.GetCellCenterWorld(tilePos);
                targetCenter.z = transform.parent.position.z;

                Vector3 currentPosition = transform.parent.position;

                Vector2 toTarget = (Vector2)(targetCenter - currentPosition);

                if (toTarget.sqrMagnitude > 0.001f)
                {
                    if (Mathf.Abs(toTarget.x) > Mathf.Abs(toTarget.y))
                    {
                        direction = toTarget.x > 0 ? Vector2.right : Vector2.left;
                    }
                    else
                    {
                        direction = toTarget.y > 0 ? Vector2.up : Vector2.down;
                    }

                    HandleFlip();
                    //Debug.Log($"Direction updated: {direction} | From: {currentPosition} To: {targetCenter}");
                }

                while ((transform.parent.position - targetCenter).sqrMagnitude > 0.01f)
                {
                    if (agentPhysics2D.IsBuilding(transform.parent.position, direction, 0.01f, moveSpeed, circleCollider2D))
                    {
                        PathfindingAlgorithm.Instance.ClearPath();
                        moving = false;
                        yield break;
                    }
                    Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetCenter, moveSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(nextPosition);
                    moving = true;
                    yield return null;
                }
                CurrentLayer = segment.layerIndex;
            }
        }

        PathfindingAlgorithm.Instance.ClearPath();
        moving = false;
        moveCoroutine = null;
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


    private void HandleFlip()
    {
        if (direction == Vector2.left)
        {
            Vector3 scale = transform.parent.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.parent.localScale = scale;
        }
        if (direction == Vector2.right)
        {
            Vector3 scale = transform.parent.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.parent.localScale = scale;
        }
    }
    public void UpdateLayerIndex()
    {
        if(CurrentLayer != floorAgent.currentFloorIndex) 
            floorAgent.MoveToFloor(CurrentLayer);
    }
}
