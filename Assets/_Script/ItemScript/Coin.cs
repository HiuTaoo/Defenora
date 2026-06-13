using _Script.ItemScript;
using UnityEngine;
using Random = UnityEngine.Random;

public class Coin : MonoBehaviour, IPoolable
{
    [Header("Drop Settings")] 
    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _dropDuration;
    private float _arcHeight;
    private float _elapsed;
    public bool _isDropping;
    private float _originalZ;

    [Header("Coin Data")] 
    [SerializeField] private int coinValue = 1;
    public int layerIndex;
    public bool _isCollected;

    public void StartDrop(Vector3 start, Vector3 targetPos, int currentLayerIndex, float dropDuration = 0.6f, float arcHeight = 1.2f)
    {
        _startPos = start;
        _originalZ = start.z;
        layerIndex = currentLayerIndex;

        Vector2 smallOffset = Random.insideUnitCircle * 0.3f;
        
        _endPos = new Vector3(targetPos.x + smallOffset.x, targetPos.y + smallOffset.y, _originalZ);

        _dropDuration = Mathf.Max(0.01f, dropDuration);
        _arcHeight = arcHeight;

        _elapsed = 0f;
        _isDropping = true;

        transform.position = _startPos;
    }

    private void Update()
    {
        if (_isDropping) UpdateDrop();
    }

    private void UpdateDrop()
    {
        _elapsed += Time.deltaTime;
        var t = Mathf.Clamp01(_elapsed / _dropDuration);

        var pos = Vector3.Lerp(_startPos, _endPos, t);
        var arc = _arcHeight * 4f * (t - t * t);
        pos.y += arc;
        pos.z = _originalZ;

        transform.position = pos;

        if (t >= 1f)
        {
            transform.position = _endPos;
            _isDropping = false;
            if (ItemManager.Instance != null) ItemManager.Instance.RegisterCoin(this);
        }
    }

    public void Collect()
    {
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.AddCoins(coinValue);
            ItemManager.Instance.UnregisterCoin(this);
        }

        if (PoolManager.Instance != null)
            PoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDropping) return;
        if (_isCollected) return;

        if (other.CompareTag("Player"))
        {
            _isCollected = true;
            Collect();
        }
        else if (other.CompareTag("Enemy"))
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
            ItemManager.Instance.UnregisterCoin(this);
        }
    }

    public void SetCoinValue(int value)
    {
        coinValue = value;
    }

    public void OnSpawned()
    {
        _elapsed = 0f;
        _isDropping = false;
        _isCollected = false;
        enabled = true;
        coinValue = 1;
        layerIndex = 0;
        _startPos = Vector3.zero;
        _endPos = Vector3.zero;
    }

    public void OnDespawned()
    {
        _isDropping = false;
        if (ItemManager.Instance != null) ItemManager.Instance.UnregisterCoin(this);
    }
}