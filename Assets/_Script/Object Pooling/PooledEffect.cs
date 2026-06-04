using UnityEngine;

public class PooledEffect : MonoBehaviour, IPoolable
{
    private SimpleSpriteAnimator _animator;

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

    // =================================================================
    // LÕI INTERFACE IPOOLABLE (Đồng bộ theo cơ chế Object Pool của game ông)
    // =================================================================

    public void OnSpawned()
    {
        if (_animator != null) _animator.RestartAnimation();
    }

    public void OnDespawned()
    {
        if (_animator != null) _animator.Stop();
    }
}