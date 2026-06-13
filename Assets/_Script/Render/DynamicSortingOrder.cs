using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSortingYX : MonoBehaviour
{
    private SpriteRenderer sr;
    private Transform myTransform; 

    [Header("yFactor là hệ số chính. xFactor = yFactor / 10.")]
    public float yFactor = 10f;
    private float xFactor => yFactor / 10f;

    public int baseOrder = 0;
    public bool isStaticDecor = false;    

    private Vector3 lastPosition; 

    private float tagZOffset = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        myTransform = transform; 

        if (sr.sortingLayerName == "Decor")
            isStaticDecor = true;
        string currentTag = gameObject.tag;
        
        if (currentTag == "NPC" || currentTag == "Enemy")
        {
            tagZOffset = 0.3f;    
        }
        else if (currentTag == "Building")
        {
            tagZOffset = 0.2f;    
        }
        else if (currentTag == "Animal")
        {
            tagZOffset = 0.1f;    
        }
        else
        {
            tagZOffset = 0f;      
        }
    }

    void Start()
    {
        if (isStaticDecor)
        {
            UpdateSortingOrderAndZ();
            enabled = false; 
        }
        else
        {
            lastPosition = myTransform.position;
            UpdateSortingOrderAndZ();
        }
    }

    void LateUpdate()
    {
        if (myTransform.position != lastPosition)
        {
            UpdateSortingOrderAndZ();
            lastPosition = myTransform.position;
        }
    }

    void UpdateSortingOrderAndZ()
    {
        Vector3 pos = myTransform.position;

        int yOrder = -(int)(pos.y * yFactor);
        int xOrder = (int)(pos.x * xFactor);
        sr.sortingOrder = baseOrder + yOrder + xOrder;

        float uniqueBias = (gameObject.GetInstanceID() % 10) * 0.1f;

        float calculatedZ = ((pos.y * 0.01f) + (pos.x * 0.001f)) + uniqueBias + tagZOffset;

        calculatedZ = Mathf.Clamp(calculatedZ, -2f, 2f);

        myTransform.position = new Vector3(pos.x, pos.y, calculatedZ);
    }
}