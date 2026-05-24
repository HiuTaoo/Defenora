using System.Collections.Generic;
using UnityEngine;

public class DuckSpawner : MonoBehaviour
{
    public static DuckSpawner Instance { get; private set; }

    [Header("Cài đặt Pool & Số lượng")]
    [SerializeField] private GameObject duckPrefab;
    [SerializeField] private int maxDucksOnScreen = 10;
    
    [Tooltip("Số lượng vịt có sẵn trên màn hình lúc mới vào game")]
    [SerializeField] private int initialDuckCount = 4;
    
    [SerializeField] private float spawnInterval = 2f;

    [Header("Khoảng cách Spawn (Từ trên xuống)")]
    [Tooltip("Khoảng cách dư ra ngoài cạnh trên camera để vịt không bị giật cục khi xuất hiện")]
    [SerializeField] private float offScreenOffset = 0.15f; 

    private Queue<GameObject> duckPool = new Queue<GameObject>();
    private int currentActiveDucks = 0;
    private float spawnTimer;
    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCam = Camera.main;
    }

    private void Start()
    {
        // 1. Khởi tạo sẵn các Object vào Pool (Ẩn đi)
        for (int i = 0; i < maxDucksOnScreen; i++)
        {
            GameObject duck = Instantiate(duckPrefab, transform);
            duck.SetActive(false);
            duckPool.Enqueue(duck);
        }

        // 2. Rải vịt trực tiếp lên màn hình ngay khi game bắt đầu
        SpawnInitialDucks();
    }

    private void Update()
    {
        if (currentActiveDucks < maxDucksOnScreen)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnDuckFromTop();
                spawnTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Rải vịt ngẫu nhiên BÊN TRONG khung hình khi vừa bật game
    /// </summary>
    private void SpawnInitialDucks()
    {
        // Đảm bảo không đẻ lố số lượng tối đa cho phép
        int spawnCount = Mathf.Min(initialDuckCount, maxDucksOnScreen);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject duck = GetDuckFromPool();
            if (duck == null) continue;

            // Rải ngẫu nhiên toàn bộ mặt màn hình (X từ 0.1 đến 0.9, Y từ 0.1 đến 0.9)
            float randomX = Random.Range(0.1f, 0.9f);
            float randomY = Random.Range(0.1f, 0.9f);
            Vector3 viewportPos = new Vector3(randomX, randomY, 0);

            // Tung đồng xu chọn hướng: Phải (0) hoặc Trái (1)
            Vector2 moveDirection = Random.Range(0, 2) == 0 
                ? new Vector2(1f, -1f).normalized 
                : new Vector2(-1f, -1f).normalized;

            ActivateDuck(duck, viewportPos, moveDirection);
        }
    }

    /// <summary>
    /// Sinh vịt từ cạnh trên màn hình rơi xuống (Dùng liên tục trong Update)
    /// </summary>
    private void SpawnDuckFromTop()
    {
        GameObject duck = GetDuckFromPool();
        if (duck == null) return;

        // Vị trí X ngẫu nhiên, Y cố định ở trên cùng + offset
        float randomViewportX = Random.Range(0f, 1f);
        Vector3 spawnViewportPos = new Vector3(randomViewportX, 1f + offScreenOffset, 0);

        // Hướng đi chéo 45 độ xuống
        Vector2 moveDirection = Random.Range(0, 2) == 0 
            ? new Vector2(1f, -1f).normalized 
            : new Vector2(-1f, -1f).normalized;

        ActivateDuck(duck, spawnViewportPos, moveDirection);
    }

    /// <summary>
    /// Hàm xử lý chung để đưa con vịt ra game (Tránh lặp lại code)
    /// </summary>
    private void ActivateDuck(GameObject duck, Vector3 viewportPos, Vector2 moveDirection)
    {
        // Chuyển đổi tọa độ Viewport sang tọa độ WorldPoint của Game 2D
        Vector3 spawnWorldPos = mainCam.ViewportToWorldPoint(viewportPos);
        spawnWorldPos.z = 0; 
        
        // Khởi động đưa con vịt ra trận
        duck.transform.position = spawnWorldPos;
        duck.SetActive(true);
        currentActiveDucks++;

        // Truyền hướng đi
        duck.GetComponent<DuckMovement>().SetDirection(moveDirection);
    }

    private GameObject GetDuckFromPool()
    {
        if (duckPool.Count > 0)
        {
            return duckPool.Dequeue();
        }
        // Fallback phòng trường hợp lỗi đếm (hiếm khi xảy ra vì đã chặn currentActiveDucks)
        return Instantiate(duckPrefab, transform);
    }

    public void ReturnDuckToPool(GameObject duck)
    {
        duck.SetActive(false);
        duckPool.Enqueue(duck);
        currentActiveDucks--;
    }
}