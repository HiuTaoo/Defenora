using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    private int layerIndex = 0;

    [SerializeField]
    private GameObject[] ribbons;

    private Vector3[] originalPositions;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        originalPositions = new Vector3[ribbons.Length];
        for (int i = 0; i < ribbons.Length; i++)
        {
            originalPositions[i] = ribbons[i].transform.position;
        }
        MoveRibbonToLeft(0);
    }

    private void Update()
    {
        Show(); 
    }

    public void ChangeLayer()
    {
        MenuTilesController.Instance.selectedLayerIndex = layerIndex;
        MenuTilesController.Instance.UpdateTilemapLayer();
    }

    public void Show()
    {
        if (Input.anyKeyDown)
        {
            switch (Input.inputString)
            {
                case "1":
                    layerIndex = 0;
                    MoveRibbonToLeft(0);
                    break;
                case "2":
                    layerIndex = 1;
                    MoveRibbonToLeft(1);
                    break;
                case "3":
                    layerIndex = 2;
                    MoveRibbonToLeft(2);
                    break;
            }

            ChangeLayer();
        }
    }

    private void MoveRibbonToLeft(int activeIndex)
    {
        for (int i = 0; i < ribbons.Length; i++)
        {
            if (i == activeIndex)
            {
                ribbons[i].transform.position = originalPositions[i] + new Vector3(-50f, 0f, 0f);
            }
            else
            {
                ribbons[i].transform.position = originalPositions[i];
            }
        }
    }
}
