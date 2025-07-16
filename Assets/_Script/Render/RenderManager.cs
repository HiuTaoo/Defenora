using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class RenderManager : MonoBehaviour
{
    public static RenderManager Instance;

    public List<RenderData> decorRender;
    public List<RenderData> characterRender;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public RenderData LookUpRenderDataByLayerIndex(List<RenderData> renderData, int layerIndex)
    {
        foreach (var data in renderData)
        {
            if (data.layerIndex == layerIndex)
            {
                return data;
            }
        }
        return null;
    }

    public void SetSortingOrderByIndex(List<RenderData> renderData, SpriteRenderer spriteRenderer, int layerIndex)
    {
        foreach (var data in renderData)
        {
            if (data.layerIndex == layerIndex)
            {
                spriteRenderer.sortingOrder = data.sortingOrder;
                break;
            }
        }
    }

    public void SetSortingOrderSubtractOneByIndex(List<RenderData> renderData, SpriteRenderer spriteRenderer, int layerIndex)
    {
        foreach (var data in renderData)
        {
            if (data.layerIndex == layerIndex)
            {
                spriteRenderer.sortingOrder = data.sortingOrder - 1;
                break;
            }
        }
    }
}
