using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ToastItem : MonoBehaviour, IPoolable
{
    [SerializeField] private float lifeTime = 2.0f;
    [SerializeField] private float fadeDuration = 0.5f;

    private TextMeshProUGUI _textMesh;
    private Coroutine _lifeCoroutine;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    public void SetupToast(string text, Color textColor)
    {
        if (_textMesh == null) _textMesh = GetComponent<TextMeshProUGUI>();

        _textMesh.text = text;
        _textMesh.color = textColor;

        if (_lifeCoroutine != null) StopCoroutine(_lifeCoroutine);
        _lifeCoroutine = StartCoroutine(ToastLifecycle());
    }

    private IEnumerator ToastLifecycle()
    {
        yield return new WaitForSeconds(lifeTime);

        var elapsed = 0f;
        var originalColor = _textMesh.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            _textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        if (UINotificationManager.Instance != null) UINotificationManager.Instance.OnToastExpired();

        PoolManager.Instance.Despawn(gameObject);
    }

    public void OnSpawned()
    {
        if (_textMesh != null)
        {
            var c = _textMesh.color;
            c.a = 1f;
            _textMesh.color = c;
        }
    }

    public void OnDespawned()
    {
        if (_lifeCoroutine != null)
        {
            StopCoroutine(_lifeCoroutine);
            _lifeCoroutine = null;
        }
    }
}