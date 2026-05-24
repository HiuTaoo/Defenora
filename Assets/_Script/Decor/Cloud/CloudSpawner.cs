using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    public static CloudSpawner Instance { get; private set; }

    [Header("Cài đặt Pool & Prefab")]
    [SerializeField] private GameObject[] cloudPrefabs; 
    [SerializeField] private int poolSize = 15; 

    [Header("Cài đặt Khi Vừa Bắt Đầu (Pre-warm)")]
    [Tooltip("Số lượng mây có sẵn trên màn hình lúc mới vào game")]
    [SerializeField] private int initialCloudCount = 5;

    [Header("Cài đặt Sinh Mây Từ Bên Ngoài")]
    [SerializeField] private float minSpawnTime = 1.5f;
    [SerializeField] private float maxSpawnTime = 4.0f;
    
    [Header("Thuộc tính Mây")]
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 1.5f;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.3f;

    private Queue<GameObject> cloudPool = new Queue<GameObject>();
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
        if (cloudPrefabs.Length == 0)
        {
            Debug.LogWarning("Chưa gắn Prefab mây vào CloudSpawner!");
            return;
        }

        // 1. Tạo sẵn toàn bộ mây vào Pool (Ẩn đi)
        for (int i = 0; i < poolSize; i++)
        {
            GameObject randomCloudPrefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
            GameObject cloud = Instantiate(randomCloudPrefab, transform);
            cloud.SetActive(false);
            cloudPool.Enqueue(cloud);
        }

        // 2. TÍNH NĂNG MỚI: Rải mây trực tiếp lên màn hình lúc vừa vào game
        SpawnInitialClouds();

        // 3. Khởi động bộ đếm thời gian cho đợt sinh mây từ ngoài vào
        SetNextSpawnTime();
    }

    private void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnCloudFromEdge();
            SetNextSpawnTime();
        }
    }

    private void SetNextSpawnTime()
    {
        spawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
    }

    /// <summary>
    /// Rải mây ngẫu nhiên BÊN TRONG khung hình khi vừa bật game
    /// </summary>
    private void SpawnInitialClouds()
    {
        // Đảm bảo không gọi quá số lượng mây có trong Pool
        int spawnCount = Mathf.Min(initialCloudCount, poolSize);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject cloud = GetCloudFromPool();
            if (cloud == null) continue;

            // X rải đều từ -0.1 đến 1.1 (để có những đám mây lấp ló ở rìa màn hình cho tự nhiên)
            float randomX = Random.Range(-0.1f, 1.1f);
            float randomY = Random.Range(0.1f, 0.9f);
            Vector3 viewportPos = new Vector3(randomX, randomY, 0);

            // Tung đồng xu chọn hướng bay ban đầu cho các đám mây này
            Vector2 direction = Random.Range(0, 2) == 0 ? Vector2.right : Vector2.left;

            // Chuyển tọa độ và hiển thị
            ActivateCloud(cloud, viewportPos, direction);
        }
    }

    /// <summary>
    /// Sinh mây TỪ BÊN NGOÀI lề bay vào (dùng trong Update)
    /// </summary>
    private void SpawnCloudFromEdge()
    {
        GameObject cloud = GetCloudFromPool();
        if (cloud == null) return;

        int randomSide = Random.Range(0, 2); 
        float randomViewportY = Random.Range(0.1f, 0.9f);

        Vector3 spawnViewportPos;
        Vector2 direction;

        if (randomSide == 0) 
        {
            spawnViewportPos = new Vector3(-0.2f, randomViewportY, 0); // Trái
            direction = Vector2.right; 
        }
        else 
        {
            spawnViewportPos = new Vector3(1.2f, randomViewportY, 0); // Phải
            direction = Vector2.left; 
        }

        ActivateCloud(cloud, spawnViewportPos, direction);
    }

    /// <summary>
    /// Hàm xử lý chung để đưa mây ra màn hình
    /// </summary>
    private void ActivateCloud(GameObject cloud, Vector3 viewportPos, Vector2 direction)
    {
        Vector3 spawnWorldPos = mainCam.ViewportToWorldPoint(viewportPos);
        spawnWorldPos.z = 0;

        cloud.transform.position = spawnWorldPos;
        cloud.SetActive(true);

        float randomSpeed = Random.Range(minSpeed, maxSpeed);
        float randomScale = Random.Range(minScale, maxScale);

        cloud.GetComponent<CloudMovement>().Initialize(direction, randomSpeed, randomScale);
    }

    private GameObject GetCloudFromPool()
    {
        if (cloudPool.Count > 0)
        {
            return cloudPool.Dequeue();
        }
        return null; // Trả về null nếu pool đang cạn
    }

    public void ReturnCloudToPool(GameObject cloud)
    {
        cloud.SetActive(false);
        cloudPool.Enqueue(cloud);
    }
}