using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomRender : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public int layerIndex = -1;

    private void Awake()
    {
        spriteRenderer = GetComponentInParent<SpriteRenderer>();

    }

    private void LookUpLayerIndex()
    {
        if(layerIndex >= 0)
        {
            var building = gameObject.GetComponentInParent<Building>();
            var tree = gameObject.GetComponentInParent<Tree>();
            if (building != null && tree == null)
            {
                layerIndex = building.LayerIndex;
            }
            if (tree != null && building == null)
            {
                layerIndex = tree.layerIndex;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GameLoop.Instance.StateMachine.CurrentStateType != GameStateType.Editor)
        {
            if (collision != null && collision.gameObject.GetComponent<SpriteRenderer>() != null )
            {
                var floorAgent = collision.gameObject.GetComponentInChildren<FloorAgent>();
                LookUpLayerIndex();

                if (layerIndex == floorAgent.currentFloorIndex)
                {
                    collision.gameObject.GetComponent<SpriteRenderer>().sortingOrder = spriteRenderer.sortingOrder - 1;
                }

                if (collision.CompareTag("Player"))
                {
                    Color c = spriteRenderer.color;
                    c.a = 0.5f;
                    spriteRenderer.color = c;
                }
                

            }
        }
        
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.GetComponent<SpriteRenderer>() != null && collision.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<FloorAgent>().UpdateVisualElements();

            if (collision.CompareTag("Player"))
            {
                Color c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }
            
        }
    }
}
