using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GraphNode : MonoBehaviour
{
    [SerializeField] private LayerData[] layerDatas;
    public Dictionary<int, PathfindingGraph> layerGraphs = new Dictionary<int, PathfindingGraph>();

    private bool isGraphBuilt = false;

    private void Awake()
    {
        BuildAllLayerGraphs();
        //PrintNodeInfo(new Vector3Int(-19, -8, 0), 2);
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

                    //Kiểm tra xem ở vị trí đó đã có node chưa
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
                                    break; // Nếu bị block bởi bất kỳ obstacle nào thì dừng
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
                                isStair = layerData.stairTilemap != null &&
                                          layerData.stairTilemap.HasTile(position),
                            };

                            Debug.Log($"Node tại {position}, tầng {layerData.layerIndex}, isStair: {node.isStair}");
                            graph.nodes[position] = node;
                        }
                    }
                }
            }
        }

        Debug.Log($"Tầng {layerData.layerIndex} có tổng {graph.nodes.Count} nodes");
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
                    currentNode.stairDirection = StairDirection.Up;
                    targetNode.stairDirection = StairDirection.Down;
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
                        stairTargetNode = currentNode,
                        stairDirection = StairDirection.Down,
                    };

                    LinkNeighBor(layerData, graph);
                    LinkNeighBor(layerDatas[targetLayerIndex], targetGraph);

                    targetGraph.nodes[position] = node;
                    currentNode.stairTargetNode = node;
                    currentNode.stairDirection = StairDirection.Up;
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

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborPos = node.position + direction;

                if (!graph.nodes.ContainsKey(neighborPos))
                    continue;

                Node neighbor = graph.nodes[neighborPos];

                if (node.isStair)
                {
                    // Nếu stair thì luôn có thể link với stair khác
                    if (neighbor.isStair)
                    {
                        node.neighbors.Add(neighbor);
                    }
                    else if ((direction == Vector3Int.up || direction == Vector3Int.down) && !neighbor.isStair)
                    {
                        // Link với node thường chỉ khi ở trên hoặc dưới
                        node.neighbors.Add(neighbor);
                    }
                }
                else
                {
                    // Nếu node thường thì chỉ được link với stair nếu stair ở trên hoặc dưới
                    if (neighbor.isStair && (direction == Vector3Int.up || direction == Vector3Int.down))
                    {
                        node.neighbors.Add(neighbor);
                    }
                    else if (!neighbor.isStair)
                    {
                        // Node thường link với thường như bình thường
                        node.neighbors.Add(neighbor);
                    }
                }
            }
        }

        //Debug.Log($"Built graph for layer {layerData.layerIndex} with {graph.nodes.Count} nodes");
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

        Pathfinding.Instance.HoverPath(position);

        Debug.Log($"--- THÔNG TIN NODE ---");
        Debug.Log($"Vị trí: {node.position}");
        Debug.Log($"Layer: {node.layerIndex}");
        Debug.Log($"Walkable: {node.isWalkable}");
        Debug.Log($"Stair: {node.isStair}");
        Debug.Log($"StairTargetNode: {(node.stairTargetNode != null ? node.stairTargetNode.position + ": " + node.stairTargetNode.layerIndex : "null")}");
        Debug.Log($"Stair Direction: {node.stairDirection}");

        Debug.Log($"Neighbors ({node.neighbors.Count}):");
        foreach (var neighbor in node.neighbors)
        {
            Debug.Log($"   - {neighbor.position}");
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
}
