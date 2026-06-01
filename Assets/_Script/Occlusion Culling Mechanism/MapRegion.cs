using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapRegion
{
    public string regionName;
    public Bounds bounds;
    public List<GameObject> objectsInRegion = new List<GameObject>();
    public bool isActive = true;

    public MapRegion(string name, Vector2 center, Vector2 size)
    {
        regionName = name;
        bounds = new Bounds(center, size);
    }

    public void SetActive(bool active)
    {
        if (isActive == active) return;

        isActive = active;
        foreach (var obj in objectsInRegion)
        {
            if (obj != null)
            {
                var spriteRenderer = obj.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = active;
                }

                var animator = obj.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = active;
                }

                var customAnim = obj.GetComponent<SimpleSpriteAnimator>();
                if (customAnim != null)
                {
                    customAnim.enabled = active;
                    if (active) customAnim.Play();
                    else customAnim.Stop();
                }
            }
        }
    }

    public void AddObject(GameObject obj)
    {
        if (!objectsInRegion.Contains(obj))
        {
            objectsInRegion.Add(obj);
        }
    }

    public void RemoveObject(GameObject obj)
    {
        objectsInRegion.Remove(obj);
    }
}