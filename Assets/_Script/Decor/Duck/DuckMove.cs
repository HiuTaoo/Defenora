using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuckMove : MonoBehaviour
{
    [Header("Cài đặt Di chuyển")]
    [Tooltip("Tốc độ di chuyển của con vịt.")]
    [SerializeField] private float speed = 2.0f; 

    [Tooltip("Hướng chéo ban đầu. (1,1) là phải-lên, (-1,-1) là trái-xuống, v.v.")]
    [SerializeField] private Vector2 initialDirection = new Vector2(1.0f, 1.0f);

    private Vector2 moveDirection; // Hướng di chuyển thực tế đã chuẩn hóa
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Kiểm tra và thiết lập hướng ban đầu
        if (initialDirection.sqrMagnitude > 0)
        {
            // Chuẩn hóa hướng để đảm bảo tốc độ nhất quán ở mọi góc
            moveDirection = initialDirection.normalized;
            FaceDirection(); // Quay mặt ban đầu
        }
        else
        {
            // Cảnh báo nếu hướng ban đầu là không
            Debug.LogWarning("DiagonalMovement: Hướng ban đầu chưa được đặt (bằng không). Con vịt sẽ không di chuyển.", gameObject);
            moveDirection = Vector2.zero;
        }
    }

    private void Update()
    {
        // Nếu hướng là không, không làm gì cả
        if (moveDirection == Vector2.zero) return;

        // Tính toán khoảng cách di chuyển trong khung hình này
        // (Hướng * Tốc độ * Thời gian trôi qua)
        Vector3 movement = new Vector3(moveDirection.x, moveDirection.y, 0) * speed * Time.deltaTime;

        // Di chuyển vị trí transform
        transform.Translate(movement);
    }

    /// <summary>
    /// Lật sprite để nhìn về hướng di chuyển ngang (trái hoặc phải).
    /// </summary>
    private void FaceDirection()
    {
        if (spriteRenderer != null)
        {
            // Kiểm tra hướng ngang
            if (moveDirection.x > 0)
            {
                // Hướng sang phải (mặc định)
                spriteRenderer.flipX = false; 
            }
            else if (moveDirection.x < 0)
            {
                // Hướng sang trái -> lật sprite
                spriteRenderer.flipX = true; 
            }
        }
    }

    /// <summary>
    /// Hàm công khai để cho phép các script khác thay đổi hướng nếu cần.
    /// </summary>
    /// <param name="newDir">Vector2 hướng mới.</param>
    public void SetNewDirection(Vector2 newDir)
    {
        if (newDir.sqrMagnitude > 0)
        {
            moveDirection = newDir.normalized;
            FaceDirection();
        }
        else
        {
            moveDirection = Vector2.zero;
        }
    }
}
