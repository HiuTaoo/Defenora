using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock : MonoBehaviour
{
    [Header("Rock Info")]
    public int layerIndex = 0;
    public Vector3Int positionInGrid;
    public bool isMinable = true;
    public float durability = 100f;
    public float currentDurability;

    private void Start()
    {
        currentDurability = durability;
    }

    public void TakeDamage(float damage)
    {
        currentDurability -= damage;
        if (currentDurability <= 0f)
        {
            DestroyRock();
        }
    }

    private void DestroyRock()
    {
        // Notify spawner that this rock was destroyed
        if (ObjectSpawner.Instance != null)
        {
            ObjectSpawner.Instance.RemoveDestroyedObject(positionInGrid, layerIndex, RespawnType.Rock);

            // Try to respawn after some time
            ObjectSpawner.Instance.StartCoroutine(DelayedRespawn());
        }

        // Set walkable if rocks block movement
        if (ObjectSpawner.Instance.spawnSettings.rocksBlockMovement)
        {
            GraphNode.Instance.SetWalkableNode(positionInGrid, layerIndex, true);
        }

        Destroy(gameObject);
    }

    private System.Collections.IEnumerator DelayedRespawn()
    {
        yield return new WaitForSeconds(ObjectSpawner.Instance.spawnSettings.rockRespawnDelay);

        // Try to respawn in nearby positions
        List<Vector3Int> nearbyPositions = GetNearbyPositions();
        foreach (var pos in nearbyPositions)
        {
            //ObjectSpawner.Instance.TryRespawnAtPosition(pos, layerIndex, RespawnType.Rock);
        }
    }

    private List<Vector3Int> GetNearbyPositions()
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        int radius = 3;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0) continue;
                positions.Add(positionInGrid + new Vector3Int(x, y, 0));
            }
        }

        return positions;
    }
}
