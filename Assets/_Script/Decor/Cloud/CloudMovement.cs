using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    private float speed;
    private Vector2 moveDirection;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    /// <summary>
    /// Spawner sẽ gọi hàm này để truyền thông số cho mây khi nó xuất hiện
    /// </summary>
    public void Initialize(Vector2 direction, float cloudSpeed, float scale)
    {
        moveDirection = direction;
        speed = cloudSpeed;
        
        // Random kích thước mây để tạo cảm giác xa gần (Parallax 2D)
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void Update()
    {
        // Vì mây không va chạm vật lý, dùng Translate là tối ưu nhất
        transform.Translate(moveDirection * speed * Time.deltaTime);
        
        CheckOutOfBounds();
    }

    private void CheckOutOfBounds()
    {
        Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);

        // Nới rộng biên độ ra 0.3 để mây trôi khuất hẳn mới biến mất
        // Bay sang phải mà quá lề phải, HOẶC bay sang trái mà quá lề trái
        if ((moveDirection.x > 0 && viewportPos.x > 1.3f) || 
            (moveDirection.x < 0 && viewportPos.x < -0.3f))
        {
            if (CloudSpawner.Instance != null)
                CloudSpawner.Instance.ReturnCloudToPool(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}