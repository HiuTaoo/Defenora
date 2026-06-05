using _Script.ItemScript;
using UnityEngine;
using Random = UnityEngine.Random;

public class Coin : MonoBehaviour
{
    [Header("Drop Settings")] private Vector3 _startPos;
    private Vector3 _endPos;
    private float _dropDuration;
    private float _arcHeight;
    private float _elapsed;
    public bool _isDropping;
    private float _originalZ;

    [Header("Coin Data")] [SerializeField] private int coinValue = 1;
    public int layerIndex;
    public bool _isCollected;

    [Header("Grid Search Settings")] [SerializeField]
    private int maxSearchRadius = 5;

    public void StartDrop(Vector3 start, int currentLayerIndex, float dropDuration = 0.6f, float arcHeight = 1.2f)
    {
        _startPos = start;
        _originalZ = start.z;
        layerIndex = currentLayerIndex;

        var randomOffset = Random.insideUnitCircle * 2f;
        var rawTargetPos = new Vector3(_startPos.x + randomOffset.x, _startPos.y + randomOffset.y, _originalZ);

        var targetGridPos = Vector3Int.FloorToInt(rawTargetPos);

        var walkableGridPos = FindWalkableCellExpanding(targetGridPos, layerIndex);

        _endPos = new Vector3(walkableGridPos.x + 0.5f, walkableGridPos.y + 0.5f, _originalZ);

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

    private Vector3Int FindWalkableCellExpanding(Vector3Int centerGridPos, int targetLayerIndex)
    {
        if (GraphNode.Instance == null) return centerGridPos;

        var centerNode = GraphNode.Instance.GetNode(centerGridPos, targetLayerIndex);
        if (centerNode != null && centerNode.isWalkable) return centerGridPos;

        for (var radius = 1; radius <= maxSearchRadius; radius++)
        {
            var bestCell = centerGridPos;
            var minDistance = Mathf.Infinity;
            var foundInThisRadius = false;

            for (var x = -radius; x <= radius; x++)
            {
                if (CheckAndEvaluateNode(centerGridPos + new Vector3Int(x, radius, 0), targetLayerIndex, ref bestCell,
                        ref minDistance)) foundInThisRadius = true;
                if (CheckAndEvaluateNode(centerGridPos + new Vector3Int(x, -radius, 0), targetLayerIndex, ref bestCell,
                        ref minDistance)) foundInThisRadius = true;
            }

            for (var y = -radius + 1; y < radius; y++)
            {
                if (CheckAndEvaluateNode(centerGridPos + new Vector3Int(radius, y, 0), targetLayerIndex, ref bestCell,
                        ref minDistance)) foundInThisRadius = true;
                if (CheckAndEvaluateNode(centerGridPos + new Vector3Int(-radius, y, 0), targetLayerIndex, ref bestCell,
                        ref minDistance)) foundInThisRadius = true;
            }

            if (foundInThisRadius) return bestCell;
        }

        return centerGridPos;
    }

    private bool CheckAndEvaluateNode(Vector3Int checkGridPos, int targetLayerIndex, ref Vector3Int bestCell,
        ref float minDistance)
    {
        var node = GraphNode.Instance.GetNode(checkGridPos, targetLayerIndex);

        if (node != null && node.isWalkable)
        {
            var distance = Vector2.Distance(_startPos, (Vector3)checkGridPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestCell = checkGridPos;
                return true;
            }
        }

        return false;
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
}