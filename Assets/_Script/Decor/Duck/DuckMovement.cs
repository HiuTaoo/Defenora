using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class DuckMovement : MonoBehaviour
{
    [Header("Cài đặt Di chuyển")]
    [SerializeField] private float speed = 3.5f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Camera mainCam;

    // Chỉ lưu hướng ngang: 1 = phải, -1 = trái
    private int horizontalDirection = 1;

    private Vector2 currentVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        /*
         * Quan trọng:
         * Nếu dùng Trigger để đổi hướng khi chạm đá,
         * nên để Rigidbody ở Kinematic để không bị lực va chạm làm chậm/lệch.
         */
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    /// <summary>
    /// Hàm gọi từ Spawner để khởi tạo hướng ban đầu.
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        // Chỉ lấy hướng ngang ban đầu.
        horizontalDirection = direction.x >= 0 ? 1 : -1;

        UpdateVelocity45Degree();
        FaceDirection();
    }

    private void FixedUpdate()
    {
        /*
         * Ép vận tốc liên tục trong FixedUpdate.
         * Điều này đảm bảo vịt không bị chậm lại hoặc lệch khỏi góc 45 độ.
         */
        rb.velocity = currentVelocity;
    }

    /// <summary>
    /// Cập nhật vận tốc đúng góc 45 độ.
    /// </summary>
    private void UpdateVelocity45Degree()
    {
        Vector2 direction45 = new Vector2(horizontalDirection, -1f).normalized;
        currentVelocity = direction45 * speed;
    }

    private void FaceDirection()
    {
        if (horizontalDirection > 0)
            spriteRenderer.flipX = false;
        else
            spriteRenderer.flipX = true;
    }

    private void Update()
    {
        CheckOutOfBounds();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Rock"))
        {
            // Đảo hướng ngang: phải -> trái, trái -> phải
            horizontalDirection *= -1;

            // Tạo lại vận tốc 45 độ chuẩn
            UpdateVelocity45Degree();

            // Áp ngay vận tốc mới
            rb.velocity = currentVelocity;

            FaceDirection();
        }
    }

    private void CheckOutOfBounds()
    {
        Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);

        if (viewportPos.x < -0.1f || viewportPos.x > 1.1f ||
            viewportPos.y < -0.1f || viewportPos.y > 1.1f)
        {
            rb.velocity = Vector2.zero;
            currentVelocity = Vector2.zero;

            if (DuckSpawner.Instance != null)
            {
                DuckSpawner.Instance.ReturnDuckToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}