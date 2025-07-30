using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private CircleCollider2D interactionCollider;
    private CircleCollider2D playerCollider;

    private GameObject interactButton;
    public GameObject currentObject;
    private InteractButton interactButtonScript;
    public InteractButtonState interactButtonState = InteractButtonState.Collect;

    public Vector2 direction;
    private int layerIndex = -1;
    private int playerLayerIndex = -1;

    public System.Action<GameObject, InteractButtonState> OnInteractButtonPressed;

    public static PlayerInteraction Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        interactionCollider = GetComponent<CircleCollider2D>();
        playerCollider = transform.parent.GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        if (interactButton == null)
            GetInteractButton();

        CheckFButtonPressed();
    }

    private void FixedUpdate()
    {
        CheckCollideWithObject();
        playerLayerIndex = transform.parent.Find("Player Movement").GetComponent<FloorAgent>().currentFloorIndex;
    }

    private void CheckCollideWithObject()
    {
        currentObject = null;

        #region Raycast Building
        if(GameLoop.Instance.gameContext.InputManager.GetMovementInput() != Vector2.zero)
            direction = GameLoop.Instance.gameContext.InputManager.GetMovementInput();

        if (direction != Vector2.zero)
        {
            Vector2 origin = transform.position;
            float rayDistance = interactionCollider.radius * 0.65f;

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, rayDistance, LayerMask.GetMask("Default"));

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject.CompareTag("Door"))
                {
                    currentObject = hit.collider.transform.parent?.gameObject ?? hit.collider.gameObject;
                    LookUpLayerIndex();
                    if (layerIndex == playerLayerIndex)
                    {
                        interactButtonScript.ChangeInteractButtonState(InteractButtonState.Enter);
                        interactButtonState = InteractButtonState.Enter;
                        break; 
                    }
                    else
                        currentObject = null;
                }
            }
        }
        #endregion

        #region Raycast Other Objects
        if (currentObject == null)
        {
            Collider2D[] interactColliders = Physics2D.OverlapCircleAll(transform.position, interactionCollider.radius);
            Collider2D[] playerColliders = Physics2D.OverlapCircleAll(transform.position, playerCollider.radius * 1.25f);

            foreach (Collider2D interactCollider in interactColliders)
            {
                foreach (Collider2D collider in playerColliders)
                {
                    if (interactCollider != playerCollider && interactCollider == collider)
                    {
                        if (interactCollider.CompareTag("Tree"))
                        {
                            currentObject = interactCollider.gameObject;
                            var tree = currentObject.GetComponent<Tree>();
                            LookUpLayerIndex();
                            if (layerIndex == playerLayerIndex && tree.currentTask.targetGameObject == null 
                                && tree.treeState != TreeState.Chopped)
                            {
                                interactButtonScript.ChangeInteractButtonState(InteractButtonState.Cut);
                                interactButtonState = InteractButtonState.Cut;
                                break;
                            }
                            else
                                currentObject = null;
                        }
                    }
                }

                if (currentObject != null)
                    break;
            }
        }
        #endregion

        interactButton?.SetActive(currentObject != null);
    }

    private void CheckFButtonPressed()
    {
        if(GameLoop.Instance.gameContext.InputManager.GetKeyDown(KeyCode.F)
            && GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Playing
            && currentObject != null && interactButton.activeInHierarchy)
        {
            OnInteractButtonPressed?.Invoke(currentObject, interactButtonState);
        }
    }

    private void LookUpLayerIndex()
    {
        var building = currentObject.GetComponent<Building>();
        var tree = currentObject.GetComponent<Tree>();
        if (building != null && tree == null)
        {
            layerIndex = building.LayerIndex;
        }
        if (tree != null && building == null)
        {
            layerIndex = tree.layerIndex;
        }
    }

    private void GetInteractButton()
    {
        if(interactButton == null)
        {
            interactButton = GameLoop.Instance.gameContext.UIManager.GetUI(GameStateType.Playing, "InteractButton");
            interactButton.SetActive(false);
            interactButtonScript = interactButton.GetComponent<InteractButton>();
        }
    }
}
