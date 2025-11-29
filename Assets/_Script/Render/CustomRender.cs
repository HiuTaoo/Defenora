using UnityEngine;

public class CustomRender : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private GameObject currentCollideGameObject;

    public int layerIndex = -1;

    private void Awake()
    {
        spriteRenderer = GetComponentInParent<SpriteRenderer>();
    }

    private void OnDisable()
    {
        if (currentCollideGameObject == null)
            return;

        currentCollideGameObject = null;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        // Không xử lý trong Editor Mode
        if (GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Editor)
            return;

        // Không quan tâm NPC
        if (col.CompareTag("NPC"))
            return;

        // Chỉ xử lý Player
        if (!col.CompareTag("Player"))
            return;

        // Player có SpriteRenderer là đủ
        if (col.GetComponent<SpriteRenderer>() == null)
            return;

        SetAlpha(0.5f);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        // Chỉ cần check Player
        if (!col.CompareTag("Player"))
            return;

        SetAlpha(1f);
    }

    private void SetAlpha(float a)
    {
        if (spriteRenderer == null)
            return;

        Color c = spriteRenderer.color;
        c.a = a;
        spriteRenderer.color = c;
    }
}