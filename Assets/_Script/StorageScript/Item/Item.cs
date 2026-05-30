using _Script.ItemScript;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemData itemData;
    public int amount;
    public int layerIndex;
    public Builder assignBuilder;

    private Vector3 startPos;
    private Vector3 endPos;

    private float duration;
    private float height;

    private float elapsed;
    public bool isDropping;

    public void StartDrop(Vector3 start, Vector3 target, float dropDuration = 0.6f, float arcHeight = 1.5f)
    {
        startPos = start;
        endPos = (start + target) / 2f; 

        duration = Mathf.Max(0.01f, dropDuration);
        height = arcHeight;

        elapsed = 0f;
        isDropping = true;

        transform.position = startPos;
    }

    private void Update()
    {
        if (!isDropping) return;

        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);

        Vector3 pos = Vector3.Lerp(startPos, endPos, t);
        
        float arc = height * 4f * (t - t * t);
        pos.y += arc;

        transform.position = pos;

        if (t >= 1f)
        {
            transform.position = endPos;
            isDropping = false;
            ItemManager.Instance.RegisterItem(this);
        }
    }

    public bool TryJoin(Builder builder)
    {
        if (assignBuilder == null)
        {
            assignBuilder = builder;
            return true;
        }

        return false;
    }
    
    public bool IsAvailableFor(Builder builder)
    {
        return assignBuilder == null || assignBuilder == builder;
    }

    public void ReserveFor(Builder builder)
    {
        assignBuilder = builder;
    }
}