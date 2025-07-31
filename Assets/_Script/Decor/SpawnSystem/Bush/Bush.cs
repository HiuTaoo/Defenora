using UnityEngine;

public class Bush : DecorObject
{
    public Vector3Int positionInGrid;
    public bool isDecorative = true;

    private Animator animator;

    private void Awake()
    {
        base.Awake();
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