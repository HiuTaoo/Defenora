using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GraphNode : MonoBehaviour
{
    public static GraphNode Instance;

    [SerializeField] public LayerData[] layerDatas;
    public Dictionary<int, PathfindingGraph> layerGraphs = new Dictionary<int, PathfindingGraph>();

    private bool isGraphBuilt = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        BuildAllLayerGraphs();
        //PrintNodeInfo(new Vector3Int(-11, -11, 0), 2);
    }

    #region Build Path Graph

    public void BuildAllLayerGraphs()
    {
        foreach (LayerData layerData in layerDatas)
        {
            BuildSingleLayerGraph(layerData);
        }

        foreach (LayerData layerData1 in layerDatas)
        {
            CreateStairConnection(layerData1);
        }

        isGraphBuilt = true;
    }

    public void BuildSingleLayerGraph(LayerData layerData)
    {
        PathfindingGraph graph = new PathfindingGraph();

        CreateNodeGraph(layerData, graph);
        LinkNeighBor(layerData, graph);

        layerGraphs[layerData.layerIndex] = graph;
    }

    private void CreateNodeGraph(LayerData layerData, PathfindingGraph graph)
    {
        graph.layerIndex = layerData.layerIndex;

        foreach (Tilemap tileMap in layerData.walkableTilemap)
        {
            BoundsInt bounds = tileMap.cellBounds;

            for (int x = bounds.min.x; x < bounds.max.x; x++)
            {
                for (int y = bounds.min.y; y < bounds.max.y; y++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);

                    if (graph.nodes.ContainsKey(position)) continue;

                    if (tileMap.HasTile(position))
                    {
                        bool isBlocked = false;

                        if (layerData.obstacleTilemap != null)
                        {
                            foreach (Tilemap obstacleTilemap in layerData.obstacleTilemap)
                            {
                                if (obstacleTilemap != null && obstacleTilemap.HasTile(position))
                                {
                                    isBlocked = true;
                                    break; 
                                }
                            }
                        }

                        if (!isBlocked)
                        {
                            Node node = new Node
                            {
                                position = position,
                                layerIndex = layerData.layerIndex,
                                isWalkable = true,
                                isBridge = false,
                                isStair = layerData.stairTilemap != null &&
                                          layerData.stairTilemap.HasTile(position),
                            };

                            //Debug.Log($"Node tại {position}, tầng {layerData.layerIndex}, isStair: {node.isStair}");
                            graph.nodes[position] = node;
                        }
                    }
                }
            }
        }

        CreateBridgeNode(layerData, graph);
        //Debug.Log($"Tầng {layerData.layerIndex} có tổng {graph.nodes.Count} nodes");
    }

    private void CreateStairConnection(LayerData layerData)
    {
        int currentLayerIndex = layerData.layerIndex;
        int targetLayerIndex = currentLayerIndex + 1;

        PathfindingGraph graph = layerGraphs[layerData.layerIndex];

        /*Debug.Log($"=== Tạo kết nối cầu thang: Tầng {currentLayerIndex} -> Tầng {targetLayerIndex} ===");
        Debug.Log($"Graph tầng {currentLayerIndex} có {graph.nodes.Count} nodes");*/

        if (!layerGraphs.TryGetValue(targetLayerIndex, out PathfindingGraph targetGraph))
        {
            Debug.LogWarning($"Không tìm thấy graph tầng {targetLayerIndex} để tạo liên kết cầu thang.");
            return;
        }

        //Debug.Log($"Graph tầng {targetLayerIndex} có {targetGraph.nodes.Count} nodes");

        int stairConnectionCount = 0;
        int currentStairNodes = 0;
        int targetStairNodes = 0;

        foreach (var kvp in graph.nodes)
        {
            Node currentNode = kvp.Value;
            Vector3Int position = currentNode.position;

            if (currentNode.isStair && layerData.stairTilemap.HasTile(position))
            {
                currentStairNodes++;
                //Debug.Log($"Tìm thấy stair node tại {position} ở tầng {currentLayerIndex}");

                if (targetGraph.nodes.TryGetValue(position, out Node targetNode))
                {
                    targetStairNodes++;
                    currentNode.stairTargetNode = targetNode;
                    targetNode.stairTargetNode = currentNode;
                    targetNode.isStair = true;
                    stairConnectionCount++;

                    //Debug.Log($"[StairLink] Đã tạo kết nối: {position} tầng {currentLayerIndex} <--> tầng {targetLayerIndex}");
                }
                else
                {
                    /*Debug.Log($"Không tìm thấy node tại {position} ở tầng {targetLayerIndex}");
                    Debug.Log($"Tạo stair node tại {position} ở tầng {targetLayerIndex}");*/

                    Node node = new Node
                    {
                        position = position,
                        layerIndex = targetLayerIndex,
                        isWalkable = true,
                        isStair = true,
                        isBridge = false,
                        stairTargetNode = currentNode,
                    };

                    LinkNeighBor(layerData, graph);
                    LinkNeighBor(layerDatas[targetLayerIndex], targetGraph);

                    targetGraph.nodes[position] = node;
                    currentNode.stairTargetNode = node;
                    stairConnectionCount++;

                    //Debug.Log($"[StairLink] Đã tạo kết nối: {position} tầng {currentLayerIndex} <--> tầng {targetLayerIndex}");
                }
            }
        }

        /*Debug.Log($"=== KẾT QUẢ ===");
        Debug.Log($"Stair nodes tầng {currentLayerIndex}: {currentStairNodes}");
        Debug.Log($"Stair nodes tầng {targetLayerIndex}: {targetStairNodes}");
        Debug.Log($"Tổng kết nối cầu thang được tạo: {stairConnectionCount}");*/
    }
    private void LinkNeighBor(LayerData layerData, PathfindingGraph graph)
    {
        graph.layerIndex = layerData.layerIndex;
        Vector3Int[] directions = {
        Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
    };

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            int bridgeNeighborCount = 0;

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborPos = node.position + direction;
                if (!graph.nodes.ContainsKey(neighborPos))
                    continue;

                Node neighbor = graph.nodes[neighborPos];

                if (node.neighbors.Contains(neighbor))
                    continue;

                if (node.isStair)
                {
                    if (neighbor.isStair)
                    {
                        node.neighbors.Add(neighbor);
                    }
                    else if ((direction == Vector3Int.up || direction == Vector3Int.down) && !neighbor.isStair)
                    {
                        node.neighbors.Add(neighbor);
                    }
                }
                else if (!node.isBridge) 
                {
                    if (neighbor.isStair && (direction == Vector3Int.up || direction == Vector3Int.down))
                    {
                        node.neighbors.Add(neighbor);
                    }
                    else if (!neighbor.isStair && !neighbor.isBridge)
                    {
                        node.neighbors.Add(neighbor);
                    }
                }

                if (node.isBridge)
                {
                    if (neighbor.isBridge)
                    {
                        node.neighbors.Add(neighbor);
                        bridgeNeighborCount++;
                        //Debug.Log($"Bridge connection: {node.position} -> {neighbor.position}");
                    }
                }
            }

            if (node.isBridge && bridgeNeighborCount == 1)
            {
                //Debug.Log($"Bridge endpoint detected at {node.position}");

                foreach (Vector3Int direction in directions)
                {
                    Vector3Int neighborPos = node.position + direction;

                    if (!graph.nodes.ContainsKey(neighborPos))
                        continue;

                    Node neighbor = graph.nodes[neighborPos];

                    if (node.neighbors.Contains(neighbor))
                        continue;

                    if (neighbor.isWalkable && !neighbor.isBridge && !neighbor.isStair)
                    {
                        node.neighbors.Add(neighbor);
                        neighbor.neighbors.Add(node);
                        //Debug.Log($"Bridge endpoint {node.position} connected to walkable {neighbor.position}");
                    }
                }
            }
        }

        //Debug.Log($"Built graph for layer {layerData.layerIndex} with {graph.nodes.Count} nodes");
    }
    private void CreateBridgeNode(LayerData layerData, PathfindingGraph graph)
    {
        if (layerData.bridgeTilemap == null) return;

        foreach (Vector3Int position in layerData.bridgeTilemap.cellBounds.allPositionsWithin)
        {
            if (layerData.bridgeTilemap.HasTile(position))
            {
                if (!graph.nodes.ContainsKey(position))
                {
                    Node node = new Node
                    {
                        position = position,
                        layerIndex = layerData.layerIndex,
                        isWalkable = true,
                        isStair = false,
                        isBridge = true,
                    };
                    graph.nodes[position] = node;
                    //Debug.Log($"Tạo bridge node tại {position} ở layer {layerData.layerIndex}");
                    continue;
                }
                if (graph.nodes.ContainsKey(position))
                {
                    graph.nodes[position].isBridge = true;
                    //Debug.Log($"Đã sửa node tại {position} ở layer {layerData.layerIndex} thành bridge");
                }
            }
        }
    }

    #endregion

    #region Access & Utility

    public Node GetNode(Vector3Int position, int layerIndex)
    {
        if (!isGraphBuilt)
        {
            Debug.LogWarning("Graph chưa được build!");
            return null;
        }

        if (layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
        {
            if (graph.nodes.TryGetValue(position, out Node node))
            {
                return node;
            }
        }

        return null;
    }

    public void PrintNodeInfo(Vector3Int position, int layerIndex)
    {
        if (!layerGraphs.TryGetValue(layerIndex, out var graph))
        {
            Debug.LogWarning($"Không tìm thấy layerIndex {layerIndex}");
            return;
        }

        if (!graph.nodes.TryGetValue(position, out var node))
        {
            Debug.LogWarning($"Không tìm thấy node tại vị trí {position} ở layer {layerIndex}");
            return;
        }

        PathfindingAlgorithm.Instance.HoverPath(position);

        Debug.Log($"--- THÔNG TIN NODE ---");
        Debug.Log($"Vị trí: {node.position}");
        Debug.Log($"Layer: {node.layerIndex}");
        Debug.Log($"Walkable: {node.isWalkable}");
        Debug.Log($"Stair: {node.isStair}");
        Debug.Log($"StairTargetNode: {(node.stairTargetNode != null ? node.stairTargetNode.position + ": " + node.stairTargetNode.layerIndex : "null")}");
        Debug.Log($"Neighbors ({node.neighbors.Count}):");
        foreach (var neighbor in node.neighbors)
        {
            Debug.Log($"   - {neighbor.position}");
        }
    }

    public void SetWalkableNode(Vector3Int position, int layerIndex, bool isWalkable)
    {
        layerGraphs.TryGetValue(layerIndex, out var layerGraph);
        if (layerGraph != null && layerGraph.nodes.TryGetValue(position, out var node))
        {
            node.isWalkable = isWalkable;
            //Debug.Log($"✅ Node FOUND: {position} @Layer {layerIndex} -> Walkable: {isWalkable}");
        }
        else
        {
            //Debug.Log($"⚠️ Node NOT FOUND: {position} @Layer {layerIndex}");
        }
    }


    public void ResetAllNodes()
    {
        foreach (var layerGraph in layerGraphs.Values)
        {
            foreach (var node in layerGraph.nodes.Values)
            {
                node.gCost = 0;
                node.hCost = 0;
                node.parent = null;
            }
        }
    }

    #endregion

    #region Method

    public Node GetRandomWalkableNode()
    {
        if (!isGraphBuilt || layerGraphs.Count == 0)
        {
            Debug.LogWarning("Graph chưa được build hoặc không có dữ liệu tầng!");
            return null;
        }

        var walkableNodes = new List<Node>();

        foreach (var graphKvp in layerGraphs)
        {
            var graph = graphKvp.Value;

            foreach (var nodeKvp in graph.nodes)
            {
                var node = nodeKvp.Value;

                if (node != null && node.isWalkable) walkableNodes.Add(node);
            }
        }

        if (walkableNodes.Count > 0)
        {
            var randomIndex = Random.Range(0, walkableNodes.Count);
            return walkableNodes[randomIndex];
        }

        Debug.LogWarning("Không tìm thấy node walkable nào trong toàn bộ các layer!");
        return null;
    }

    /// <summary>
    ///     Tìm một node ngẫu nhiên có độ thông thoáng tốt nhất (ưu tiên 9 ô walkable, rồi giảm dần 8, 7...)
    /// </summary>
    public Node GetBestWalkableNodeArea()
    {
        if (!isGraphBuilt || layerGraphs.Count == 0)
        {
            Debug.LogWarning("Graph chưa được build hoặc không có dữ liệu tầng!");
            return null;
        }

        var scoreGroups = new Dictionary<int, List<Node>>();
        for (var i = 0; i <= 9; i++) scoreGroups[i] = new List<Node>();

        var directions8 = new[]
        {
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(1, -1, 0)
        };

        foreach (var graphKvp in layerGraphs)
        {
            var graph = graphKvp.Value;

            foreach (var nodeKvp in graph.nodes)
            {
                var centerNode = nodeKvp.Value;

                if (centerNode == null || !centerNode.isWalkable) continue;

                var walkableCount = 1;

                foreach (var dir in directions8)
                {
                    var neighborPos = centerNode.position + dir;

                    if (graph.nodes.TryGetValue(neighborPos, out var neighborNode))
                        if (neighborNode != null && neighborNode.isWalkable)
                            walkableCount++;
                }

                scoreGroups[walkableCount].Add(centerNode);
            }
        }

        for (var targetScore = 9; targetScore >= 1; targetScore--)
            if (scoreGroups[targetScore].Count > 0)
            {
                var randomIndex = Random.Range(0, scoreGroups[targetScore].Count);
                return scoreGroups[targetScore][randomIndex];
            }

        Debug.LogWarning("Không tìm thấy bất kỳ node walkable nào trên bản đồ!");
        return null;
    }

    public bool GetNodeWorldData(Node node, out Vector3 worldPosition, out int layerIndex)
    {
        worldPosition = Vector3.zero;
        layerIndex = 0;

        if (node == null)
        {
            Debug.LogWarning("[GraphNode] Node truyền vào bị null!");
            return false;
        }

        layerIndex = node.layerIndex;

        if (layerIndex >= 0 && layerIndex < layerDatas.Length)
        {
            var layerData = layerDatas[layerIndex];

            if (layerData.walkableTilemap != null && layerData.walkableTilemap.Length > 0)
            {
                var targetTilemap = layerData.walkableTilemap[0];
                if (targetTilemap != null)
                {
                    worldPosition = targetTilemap.GetCellCenterWorld(node.position);
                    return true;
                }
            }
        }

        worldPosition = new Vector3(node.position.x + 0.5f, node.position.y + 0.5f, 0);
        return true;
    }

    public bool IsWalkableNode(Vector3Int targetPosition, int layerIndex)
    {
        var node = GetNode(targetPosition, layerIndex);
        return node != null && node.isWalkable;
    }

    #endregion
}
