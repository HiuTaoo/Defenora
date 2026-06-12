using System;
using _Script.Task;
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

    [Header("Non-Alloc Arrays (Tối ưu Memory - Đã rút gọn)")]
    // Đã loại bỏ mảng playerResults vì không còn cần thiết nữa!
    public Collider2D[] interactResults = new Collider2D[10];
    private RaycastHit2D[] raycastResults = new RaycastHit2D[5];

    public Action<GameObject, InteractButtonState> OnInteractButtonPressed;

    public static PlayerInteraction Instance { get; private set; }

    [Header("UI Positioning")]
    public Vector3 buttonOffset = new Vector3(0, 0.25f, 0); 
    private Camera mainCamera;
    private RectTransform interactButtonRect;

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

    private void Start()
    {
        mainCamera = Camera.main; 
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
        playerLayerIndex = transform.parent.GetComponentInChildren<FloorAgent>().currentFloorIndex;
    }

    private void LateUpdate()
    {
        UpdateInteractButtonPosition();
    }

    private void UpdateInteractButtonPosition()
    {
        if (currentObject != null && interactButton != null && interactButton.activeSelf)
        {
            if (mainCamera == null) mainCamera = Camera.main; 

            Vector3 worldPosition = currentObject.transform.position + buttonOffset;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z < 0)
            {
                interactButtonRect.position = new Vector3(-9999, -9999, 0); 
            }
            else
            {
                interactButtonRect.position = screenPosition;
            }
        }
    }

    private void CheckCollideWithObject()
    {
        currentObject = null;

        #region Raycast Building (Đã tối ưu NonAlloc)

        if (GameManager.Instance.gameContext.InputManager.GetMovementInput() != Vector2.zero)
            direction = GameManager.Instance.gameContext.InputManager.GetMovementInput();

        if (direction != Vector2.zero)
        {
            Vector2 origin = transform.position;
            var rayDistance = interactionCollider.radius * 0.65f;

            var hitCount = Physics2D.RaycastNonAlloc(origin, direction, raycastResults, rayDistance,
                LayerMask.GetMask("Default"));

            for (var i = 0; i < hitCount; i++)
            {
                var hit = raycastResults[i];
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
                    {
                        currentObject = null;
                    }
                }
            }
        }

        #endregion

        #region Raycast Other Objects

        if (currentObject == null)
        {
            var interactCount =
                Physics2D.OverlapCircleNonAlloc(transform.position, interactionCollider.radius, interactResults);

            for (var i = 0; i < interactCount; i++)
            {
                var interactCol = interactResults[i];
                if (interactCol == null || interactCol == playerCollider) continue;

                if (interactionCollider.bounds.Intersects(interactCol.bounds))
                {
                    if (interactCol.CompareTag("SpawnPoint") &&
                        interactCol.gameObject.layer == LayerMask.NameToLayer("SpawnPoint"))
                    {
                        if(RaidManager.Instance.activeRaidTarget == interactCol.gameObject)
                            continue;
                        
                        currentObject = interactCol.gameObject;
                        LookUpLayerIndex();

                        if (layerIndex == playerLayerIndex)
                        {
                            interactButtonScript.ChangeInteractButtonState(InteractButtonState.Attack);
                            interactButtonState = InteractButtonState.Attack;
                            break;
                        }
                        else
                        {
                            currentObject = null;
                        }
                    }

                    if (interactCol.CompareTag("Tree"))
                    {
                        var candidateTreeGO = interactCol.gameObject;
                        var tree = candidateTreeGO.GetComponent<Tree>();

                        if (tree != null && tree.treeState != TreeState.Chopped)
                        {
                            currentObject = candidateTreeGO;
                            LookUpLayerIndex();

                            var task = tree.GetTask();

                            var isTaskAvailable = task == null
                                                  || task.targetGameObject == null
                                                  || task.taskStatus == TaskStatus.Completed;

                            if (layerIndex == playerLayerIndex && isTaskAvailable)
                            {
                                interactButtonScript.ChangeInteractButtonState(InteractButtonState.Cut);
                                interactButtonState = InteractButtonState.Cut;
                                break;
                            }
                            else
                            {
                                currentObject = null;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        interactButton?.SetActive(currentObject != null);
    }

    private void CheckFButtonPressed()
    {
        if(GameManager.Instance.gameContext.InputManager.GetKeyDown(KeyCode.F)
            && GameManager.Instance.StateMachine.CurrentStateType == GameStateType.Playing
            && currentObject != null && interactButton.activeInHierarchy)
        {
            OnInteractButtonPressed?.Invoke(currentObject, interactButtonState);
        }
    }

    private void LookUpLayerIndex()
    {
        if (currentObject == null) return;

        var building = currentObject.GetComponent<Building>();
        var tree = currentObject.GetComponent<Tree>();
        var spawnPoint = currentObject.GetComponent<SpawnPoint>();

        if (spawnPoint != null)
        {
            layerIndex = spawnPoint.layerIndex;
            return;
        }
        if (building != null && tree == null)
        {
            layerIndex = building.LayerIndex;
            return;
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
            interactButton = GameManager.Instance.gameContext.UIManager.GetUI(GameStateType.Playing, UINames.InteractButton);
            if(interactButton.activeInHierarchy)
                interactButton.SetActive(false);
            
            interactButtonScript = interactButton.GetComponent<InteractButton>();
            interactButtonRect = interactButton.GetComponent<RectTransform>();
        }
    }
}