using UnityEngine;

public class PooledEffect : MonoBehaviour, IPoolable
{
    private SimpleSpriteAnimator _animator;
    [SerializeField] private Vector3 defaultPrefabScale = new(0.65f, 0.65f, 0.65f);

    private void Awake()
    {
        _animator = GetComponent<SimpleSpriteAnimator>();
    }

    private void Update()
    {
        if (_animator != null && !_animator.IsPlaying) DespawnEffect();
    }

    private void DespawnEffect()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }


    public void OnSpawned()
    {
        transform.localScale = defaultPrefabScale;
        if (_animator != null) _animator.RestartAnimation();
    }

    public void OnDespawned()
    {
        transform.SetParent(null);

        transform.localScale = defaultPrefabScale;

        gameObject.SetActive(false);
        if (_animator != null) _animator.Stop();
    }
}