using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    [Header("List ribbon")]
    [SerializeField] private GameObject[] ribbons;

    private int layerIndex = 0;
    private Vector3[] originalPositions;

    public System.Action<int> OnLayerIndexChange;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

    }
    private void Start()
    {
        GetOriginalPosition();
    }

    private void Update()
    {
        Show(); 
    }

    /*public void GetOriginalPosition()
    {
        originalPositions = new Vector3[ribbons.Length];
        for (int i = 0; i < ribbons.Length; i++)
        {
            originalPositions[i] = ribbons[i].transform.position;
        }
        MoveRibbonToLeft(0);

    }*/

    public void GetOriginalPosition()
    {
        originalPositions = new Vector3[ribbons.Length];

        for (int i = 0; i < ribbons.Length; i++)
        {
            RectTransform rect = ribbons[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                // Lấy vị trí LOCAL trong Canvas Space
                originalPositions[i] = rect.anchoredPosition3D;
            }
            else
            {
                // Fallback nếu không phải RectTransform (phòng trường hợp)
                originalPositions[i] = ribbons[i].transform.localPosition;
            }
        }

        MoveRibbonToLeft(0);
    }


    public void ChangeLayer()
    {
        MenuTilesController.Instance.selectedLayerIndex = layerIndex;
        MenuTilesController.Instance.UpdateTilemapLayer();
        OnLayerIndexChange?.Invoke(layerIndex);
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

    /*private void MoveRibbonToLeft(int activeIndex)
    {
        for (int i = 0; i < ribbons.Length; i++)
        {
            Vector3 original = originalPositions[i];

            if (i == activeIndex)
            {
                // Chỉ thay đổi trục X, giữ nguyên Y và Z
                ribbons[i].transform.position = new Vector3(
                    original.x - 50f, // sang trái
                    original.y,
                    original.z
                );
            }
            else
            {
                ribbons[i].transform.position = original;
            }
        }
    }*/
    private void MoveRibbonToLeft(int activeIndex)
    {
        for (int i = 0; i < ribbons.Length; i++)
        {
            Vector2 original = originalPositions[i];
            RectTransform rect = ribbons[i].GetComponent<RectTransform>();

            if (rect != null)
            {
                if (i == activeIndex)
                {
                    rect.anchoredPosition = new Vector2(
                        original.x - 50f, 
                        original.y      
                    );
                }
                else
                {
                    rect.anchoredPosition = original;
                }
            }
        }
    }


}
