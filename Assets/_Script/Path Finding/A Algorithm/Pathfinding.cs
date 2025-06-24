using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinding
{
    public List<PathSegment> segments = new List<PathSegment>();
    public bool isValid = false;

    public void PrintPath()
    {
        if (!isValid)
        {
            Debug.Log("Đường đi không hợp lệ!");
            return;
        }

        Debug.Log("=== ĐƯỜNG ĐI ĐA TẦNG ===");
        for (int i = 0; i < segments.Count; i++)
        {
            PathSegment segment = segments[i];
            Debug.Log($"Tầng {segment.layerIndex}: {segment.description}");

            string pathString = "";
            for (int j = 0; j < segment.positions.Count; j++)
            {
                pathString += segment.positions[j].ToString();
                if (j < segment.positions.Count - 1)
                    pathString += " -> ";
                PathfindingAlgorithm.Instance.HoverPath(segment.positions[j]);
            }
            Debug.Log($"  Path: {pathString}");
        }
    }
}