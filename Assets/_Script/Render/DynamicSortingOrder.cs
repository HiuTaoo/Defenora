using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSortingYX : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("yFactor là hệ số chính. xFactor = yFactor / 10.")]
    public float yFactor = 10f;
    private float xFactor => yFactor / 10f;

    public int baseOrder = 0;

    public bool isStaticDecor = false;    

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sortingLayerName == "Decor")
            isStaticDecor = true;
    }

    void Start()
    {
        if (isStaticDecor)
            UpdateSortingOrder();
    }

    void LateUpdate()
    {
        if (!isStaticDecor)
            UpdateSortingOrder();
    }

    void UpdateSortingOrder()
    {
        Vector3 pos = transform.position;

        int yOrder = -(int)(pos.y * yFactor);
        int xOrder = (int)(pos.x * xFactor);

        sr.sortingOrder = baseOrder + yOrder + xOrder;
    }
}