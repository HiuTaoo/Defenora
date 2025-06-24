using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private Tilemap tilemap;

    private FloorAgent floorAgent;
    private Rigidbody2D rb;
    private Camera cam;
    public bool moving { get; private set; } = false;
    public Vector2 direction { get; private set; } = Vector2.zero;
    public int currentLayer = 0;
    
    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        floorAgent = GetComponent<FloorAgent>();
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MoveByMouse();
        }
    }

    public void MoveByMouse()
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3Int targetPosition = Vector3Int.FloorToInt(mouseWorldPos);

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
                //Debug.Log($"Layer {i}, node: {(node != null ? "OK" : "NULL")}, walkable: {node?.isWalkable}");
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

        PathFinding path = PathfindingAlgorithm.Instance.FindMultiLayerPath(gridPosition, floorAgent.currentFloorIndex, position, layer);
        
        path.PrintPath();

        StopAllCoroutines();
        StartCoroutine(FollowPathCoroutine(path));
    }

    private IEnumerator FollowPathCoroutine(PathFinding path)
    {
        moving = true;

        foreach (var segment in path.segments)
        {
            foreach (var tilePos in segment.positions)
            {
                Vector3 targetCenter = tilemap.GetCellCenterWorld(tilePos);

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
                    Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetCenter, moveSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(nextPosition);
                    yield return null;
                }

                currentLayer = segment.layerIndex;
            }
        }

        PathfindingAlgorithm.Instance.ClearPath();
        moving = false;
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
        if(currentLayer != floorAgent.currentFloorIndex) 
            floorAgent.MoveToFloor(currentLayer);
    }
}
