using _Script.ItemScript;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemData itemData;
    public int amount;
    public int layerIndex;
    public Builder assignBuilder;

    [Header("Reservation Timeout")]
    [Tooltip("Thời gian tối đa (giây) giữ chỗ cho một Builder. Quá thời gian này sẽ tự hủy đặt chỗ.")]
    [SerializeField] private float reservationTimeoutDuration = 30f; 
    private float reservationTimer;

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
        if (isDropping)
        {
            UpdateDrop();
            return; 
        }

        if (assignBuilder != null)
        {
            reservationTimer += Time.deltaTime;
            if (reservationTimer >= reservationTimeoutDuration)
            {
                CancelReservation();
            }
        }
    }

    private void UpdateDrop()
    {
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
            ReserveFor(builder);
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
        reservationTimer = 0f; 
    }

    /// <summary>
    /// Hủy đặt chỗ từ Builder hiện tại
    /// </summary>
    public void CancelReservation()
    {
        if (assignBuilder != null)
        {
            Debug.Log($"[Item] {gameObject.name} hủy đặt chỗ của Builder do quá thời gian chờ.");
            
            // Nếu script Builder của bạn có logic cần xóa Item mục tiêu khi bị hủy, 
            // bạn có thể gọi nó ở đây. Ví dụ: assignBuilder.ClearTargetItem();
            
            assignBuilder = null;
        }
        reservationTimer = 0f;
    }
}