using UnityEngine;

public static class GameObjectExtensions
{
    public static string GetId(this GameObject go)
    {
        var uniqueId = go.GetComponent<UniqueId>();
        if (uniqueId != null)
        {
            return uniqueId.Id;
        }
        
        uniqueId = go.AddComponent<UniqueId>();
        return uniqueId.Id;
    }

    public static void OverrideId(this GameObject go, string savedId)
    {
        var uniqueId = go.GetComponent<UniqueId>();
        if (uniqueId == null)
        {
            uniqueId = go.AddComponent<UniqueId>();
        }

        uniqueId.SetStoredId(savedId);
    }

    public static string GetId(this Component comp) => comp.gameObject.GetId();
    public static void OverrideId(this Component comp, string savedId) => comp.gameObject.OverrideId(savedId);
}