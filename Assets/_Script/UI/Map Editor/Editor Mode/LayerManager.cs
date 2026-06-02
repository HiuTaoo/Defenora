using System;
using DG.Tweening;
using UnityEngine;

// Nhớ thêm thư viện DOTween

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    [Header("List ribbon")]
    [SerializeField] private GameObject[] ribbons;
    
    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.15f;

    public int layerIndex = 0;
    private Vector3[] originalPositions;

    public Action<int> OnLayerIndexChange;

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

    public void GetOriginalPosition()
    {
        originalPositions = new Vector3[ribbons.Length];

        for (int i = 0; i < ribbons.Length; i++)
        {
            RectTransform rect = ribbons[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                originalPositions[i] = rect.anchoredPosition3D;
            }
            else
            {
                originalPositions[i] = ribbons[i].transform.localPosition;
            }
        }

        MoveRibbonToLeft(0);
    }

    public void ChangeLayer()
    {
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

    public void SwitchToNextLayer()
    {
        if (ribbons == null || ribbons.Length == 0) return;

        layerIndex = (layerIndex + 1) % ribbons.Length;

        MoveRibbonToLeft(layerIndex);
        ChangeLayer();
    }

    public void MoveRibbonToLeft(int activeIndex)
    {
        for (int i = 0; i < ribbons.Length; i++)
        {
            ribbons[i].transform.SetSiblingIndex(ribbons.Length - i);
        }
        
        ribbons[activeIndex].transform.SetSiblingIndex(ribbons.Length);

        for (int i = 0; i < ribbons.Length; i++)
        {
            Vector3 original = originalPositions[i];
            
            ribbons[i].transform.DOKill();

            RectTransform rect = ribbons[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                if (i == activeIndex)
                {
                    Vector2 targetPos = new Vector2(original.x - 50f, original.y);
                    rect.DOAnchorPos(targetPos, moveDuration).SetEase(Ease.OutCubic);
                }
                else
                {
                    rect.DOAnchorPos(original, moveDuration).SetEase(Ease.OutCubic);
                }
            }
            else 
            {
                if (i == activeIndex)
                {
                    Vector3 targetPos = new Vector3(original.x - 50f, original.y, original.z);
                    ribbons[i].transform.DOLocalMove(targetPos, moveDuration).SetEase(Ease.OutCubic);
                }
                else
                {
                    ribbons[i].transform.DOLocalMove(original, moveDuration).SetEase(Ease.OutCubic);
                }
            }
        }
    }
}