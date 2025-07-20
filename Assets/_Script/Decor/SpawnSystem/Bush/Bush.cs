using UnityEngine;

public class Bush : MonoBehaviour
{
    public int layerIndex;
    public Vector3Int positionInGrid;
    public bool isDecorative = true;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }
    private void Start()
    {
    }

    public void OnBushInteracted()
    {
    }
}