using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance;

    [Header("Tilemap for Visualizing Path")]
    [SerializeField] private Tilemap emptyTilemap;
    [SerializeField] private TileBase redTileBase;

    private GraphNode graphNode;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        graphNode = GetComponent<GraphNode>();

        var result = FindMultiLayerPath(
            new Vector3Int(-16, 2, 0), 0,
            new Vector3Int(-3, -13, 0), 2
        );

        result.PrintPath();
    }

    #region graphNode Core

    public MultiLayerPath FindMultiLayerPath(Vector3Int startPos, int startLayer, Vector3Int targetPos, int targetLayer)
    {
        MultiLayerPath result = new MultiLayerPath();

        Node startNode = graphNode.GetNode(startPos, startLayer);
        Node targetNode = graphNode.GetNode(targetPos, targetLayer);

        if (startNode == null || targetNode == null)
        {
            Debug.LogWarning("Không thể tìm thấy start hoặc target node!");
            return result;
        }

        var path = FindPathMultiLayerAStar(startNode, targetNode);
        if (path != null)
        {
            result = ConvertToSegmentedPath(path);
            result.isValid = true;
        }

        return result;
    }

    private List<MultiLayerNode> FindPathMultiLayerAStar(Node startNode, Node targetNode)
    {
        var openSet = new SortedSet<MultiLayerNode>();
        var closedSet = new HashSet<string>();
        var allNodes = new Dictionary<string, MultiLayerNode>();

        var start = new MultiLayerNode(startNode)
        {
            gCost = 0,
            hCost = GetHeuristic(startNode, targetNode)
        };

        openSet.Add(start);
        allNodes[GetNodeKey(startNode)] = start;

        while (openSet.Count > 0)
        {
            var current = openSet.Min;
            openSet.Remove(current);

            string currentKey = GetNodeKey(current.node);
            closedSet.Add(currentKey);

            if (current.node.position == targetNode.position && current.node.layerIndex == targetNode.layerIndex)
                return RetracePath(start, current);

            foreach (var neighbor in GetAllPossibleNeighbors(current.node))
            {
                string key = GetNodeKey(neighbor);
                if (closedSet.Contains(key)) continue;

                float cost = current.gCost + GetMovementCost(current.node, neighbor);

                if (!allNodes.TryGetValue(key, out MultiLayerNode neighborML))
                {
                    neighborML = new MultiLayerNode(neighbor);
                    allNodes[key] = neighborML;
                }
                else if (openSet.Contains(neighborML))
                {
                    openSet.Remove(neighborML); // SortedSet requires remove before update
                }

                if (cost < neighborML.gCost)
                {
                    neighborML.gCost = cost;
                    neighborML.hCost = GetHeuristic(neighbor, targetNode);
                    neighborML.parent = current;
                    neighborML.isStairTransition = current.node.layerIndex != neighbor.layerIndex;
                }

                openSet.Add(neighborML);
            }
        }

        Debug.LogWarning("Không tìm thấy đường đi đa tầng!");
        return null;
    }

    #endregion

    #region Helper Methods

    private List<Node> GetAllPossibleNeighbors(Node node)
    {
        List<Node> result = new List<Node>();

        foreach (var neighbor in node.neighbors)
            if (neighbor.isWalkable) result.Add(neighbor);

        if (node.isStair && node.stairTargetNode != null && node.stairTargetNode.isWalkable)
            result.Add(node.stairTargetNode);

        return result;
    }

    private float GetMovementCost(Node from, Node to)
    {
        return (from.layerIndex != to.layerIndex) ? 1f : 1f; // Giữ penalty = 0 cho stair
    }

    private float GetHeuristic(Node from, Node to)
    {
        float distance = Mathf.Abs(from.position.x - to.position.x) + Mathf.Abs(from.position.y - to.position.y);
        distance += Mathf.Abs(from.layerIndex - to.layerIndex); // Phạt nếu khác tầng
        return distance;
    }

    private string GetNodeKey(Node node) => $"{node.layerIndex}_{node.position.x}_{node.position.y}";

    private List<MultiLayerNode> RetracePath(MultiLayerNode start, MultiLayerNode end)
    {
        List<MultiLayerNode> path = new List<MultiLayerNode>();
        for (var current = end; current != start; current = current.parent)
            path.Add(current);
        path.Add(start);
        path.Reverse();
        return path;
    }

    #endregion

    #region Path Conversion

    private MultiLayerPath ConvertToSegmentedPath(List<MultiLayerNode> mlPath)
    {
        MultiLayerPath result = new MultiLayerPath();
        if (mlPath == null || mlPath.Count == 0) return result;

        List<Vector3Int> segmentPositions = new();
        int currentLayer = mlPath[0].node.layerIndex;
        Vector3Int segmentStart = mlPath[0].node.position;

        foreach (var mlNode in mlPath)
        {
            if (mlNode.node.layerIndex == currentLayer)
            {
                segmentPositions.Add(mlNode.node.position);
            }
            else
            {
                AddSegment(result, currentLayer, segmentStart, segmentPositions);
                currentLayer = mlNode.node.layerIndex;
                segmentStart = mlNode.node.position;
                segmentPositions = new List<Vector3Int> { mlNode.node.position };
            }
        }

        if (segmentPositions.Count > 0)
            AddSegment(result, currentLayer, segmentStart, segmentPositions);

        return result;
    }

    private void AddSegment(MultiLayerPath result, int layer, Vector3Int start, List<Vector3Int> positions)
    {
        Vector3Int end = positions[^1];
        string desc = (start == end) ? $"Tại vị trí {start}" : $"Đi từ {start} đến {end}";
        result.segments.Add(new PathSegment(layer, new List<Vector3Int>(positions), desc));
    }

    #endregion

    #region Debug

    public void HoverPath(Vector3Int position)
    {
        emptyTilemap.SetTile(position, redTileBase);
    }

    #endregion
}
