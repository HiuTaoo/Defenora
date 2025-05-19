using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class OceanGridDrawer : MonoBehaviour
{
    public Color gridColor = new Color(0f, 0.5f, 1f, 0.5f); // xanh dương nhạt
    private Tilemap tilemap;
    private Material lineMaterial;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();

        // Material mặc định dùng GL
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;

        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    void OnPostRender()
    {
        if (!tilemap || !Camera.main) return;

        Vector3 camMin = Camera.main.ViewportToWorldPoint(new Vector3(0, 0));
        Vector3 camMax = Camera.main.ViewportToWorldPoint(new Vector3(1, 1));

        Vector3Int min = tilemap.WorldToCell(camMin) - Vector3Int.one * 2;
        Vector3Int max = tilemap.WorldToCell(camMax) + Vector3Int.one * 2;

        Vector3 cellSize = tilemap.cellSize;

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
        GL.modelview = Camera.main.worldToCameraMatrix;

        GL.Begin(GL.LINES);
        GL.Color(gridColor);

        // Vẽ đường dọc (theo Y)
        for (int x = min.x; x <= max.x + 1; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector3 start = tilemap.CellToWorld(new Vector3Int(x, y, 0));
                Vector3 end = start + new Vector3(0, cellSize.y, 0);
                GL.Vertex(start);
                GL.Vertex(end);
            }
        }

        // Vẽ đường ngang (theo X)
        for (int y = min.y; y <= max.y + 1; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                Vector3 start = tilemap.CellToWorld(new Vector3Int(x, y, 0));
                Vector3 end = start + new Vector3(cellSize.x, 0, 0);
                GL.Vertex(start);
                GL.Vertex(end);
            }
        }

        GL.End();
        GL.PopMatrix();
    }

}
