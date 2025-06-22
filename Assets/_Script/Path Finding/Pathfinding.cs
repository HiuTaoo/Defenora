using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] private LayerData[] layerDatas;
    private Dictionary<int, PathfindingGraph> layerGraphs = new Dictionary<int, PathfindingGraph>();

    private bool isGraphBuilt = false;

    private void Awake()
    {
        BuildAllLayerGraphs();
        //PrintNodeInfo(new Vector3Int(-21,8,0), 1);
    }
    #region Build Path Graph
    public void BuildAllLayerGraphs()
    {
        foreach (LayerData layerData in layerDatas)
        {
            BuildSingleLayerGraph(layerData);
        }
        foreach(LayerData layerData1 in layerDatas)
        {

            CreateStairConnection(layerData1);
        }
        isGraphBuilt = true;

    }

    public void BuildSingleLayerGraph(LayerData layerData)
    {
        PathfindingGraph graph = new PathfindingGraph();

        //Duyệt qua từng tilemap để tạo graph node 
        CreateNodeGraph(layerData, graph);

        //Kết nối Neighbors 
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
                    if (graph.nodes.ContainsKey(position))
                        continue;

                    // Kiểm tra có tile walkable không
                    if (tileMap.HasTile(position))
                    {
                        // Kiểm tra không bị obstacle block
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

                        // Chỉ tạo node khi KHÔNG bị block bởi bất kỳ obstacle nào
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
    }

    private void CreateStairConnection(LayerData layerData)
    {
        int currentLayerIndex = layerData.layerIndex;
        int targetLayerIndex = currentLayerIndex + 1;

        PathfindingGraph graph = layerGraphs[layerData.layerIndex];

        /*Debug.Log($"=== Tạo kết nối cầu thang: Tầng {currentLayerIndex} -> Tầng {targetLayerIndex} ===");
        Debug.Log($"Graph tầng {currentLayerIndex} có {graph.nodes.Count} nodes");*/

        // Kiểm tra xem tầng target có tồn tại không
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

                // Lấy node ở tầng kế tiếp tại cùng vị trí, nếu đã có node thì gán stairTargetNode 
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
                //Nếu chưa có node thì tạo node mới
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
                    targetGraph.nodes[position] = node;
                    currentNode.stairTargetNode = node;
                    currentNode.stairDirection = StairDirection.Up;
                    stairConnectionCount++;
                    //Debug.Log($"[StairLink] Đã tạo kết nối: {position} tầng {currentLayerIndex} <--> tầng {targetLayerIndex}");
                }
            }
        }

       /* Debug.Log($"=== KẾT QUẢ ===");
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

                if (graph.nodes.ContainsKey(neighborPos))
                {
                    node.neighbors.Add(graph.nodes[neighborPos]);
                }
            }
        }


        //Debug.Log($"Built graph for layer {layerData.layerIndex} with {graph.nodes.Count} nodes");
    }

    public Node GetNode(Vector3Int position, int layerIndex)
    {
        if (!isGraphBuilt)
        {
            Debug.LogWarning("Graph chưa được build!");
            return null;
        }

        if (layerGraphs.ContainsKey(layerIndex))
        {
            var graph = layerGraphs[layerIndex];
            if (graph.nodes.ContainsKey(position))
            {
                return graph.nodes[position];
            }
        }
        return null;
    }

    public void PrintNodeInfo(Vector3Int position, int layerIndex)
    {
        if (!layerGraphs.ContainsKey(layerIndex))
        {
            Debug.LogWarning($"Không tìm thấy layerIndex {layerIndex}");
            return;
        }

        var graph = layerGraphs[layerIndex];
        if (!graph.nodes.ContainsKey(position))
        {
            Debug.LogWarning($"Không tìm thấy node tại vị trí {position} ở layer {layerIndex}");
            return;
        }

        Node node = graph.nodes[position];

        Debug.Log($"--- THÔNG TIN NODE ---");
        Debug.Log($"Vị trí: {node.position}");
        Debug.Log($"Layer: {node.layerIndex}");
        Debug.Log($"Walkable: {node.isWalkable}");
        Debug.Log($"Stair: {node.isStair}");
        Debug.Log($"StairTargetNode: {(node.stairTargetNode != null ? node.stairTargetNode.position.ToString() + ": " + node.stairTargetNode.layerIndex.ToString() : "null")}");
        Debug.Log($"Stair Direction: {node.stairDirection}");

        Debug.Log($"Neighbors ({node.neighbors.Count}):");
        foreach (var neighbor in node.neighbors)
        {
            Debug.Log($"   - {neighbor.position}");
        }
    }
    #endregion

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

}
