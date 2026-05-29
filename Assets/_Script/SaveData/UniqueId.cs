using UnityEngine;

[DisallowMultipleComponent]
public class UniqueId : MonoBehaviour
{
    [SerializeField] private string id;
    public string Id => id;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
        {
            id = string.Empty;
            return;
        }
#endif
        if (string.IsNullOrEmpty(id) && !gameObject.scene.name.Equals(null))
        {
            id = System.Guid.NewGuid().ToString();
        }
    }

    private void Awake()
    {
        GenerateNewIdIfEmpty();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            id = System.Guid.NewGuid().ToString();
        }
    }

    public void GenerateNewIdIfEmpty()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }
    }

    public void OverrideId(string savedId)
    {
        id = savedId;
    }

    public void SetStoredId(string savedId)
    {
        id = savedId;
    }
}