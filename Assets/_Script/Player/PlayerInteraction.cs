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
    
    [Header("Non-Alloc Arrays (Tối ưu Memory)")]
    // Khởi tạo sẵn các mảng với kích thước cố định (ví dụ 10 phần tử là quá đủ cho vùng gần player)
    private Collider2D[] interactResults = new Collider2D[10];
    private Collider2D[] playerResults = new Collider2D[10];
    private RaycastHit2D[] raycastResults = new RaycastHit2D[5];

    public System.Action<GameObject, InteractButtonState> OnInteractButtonPressed;

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
        playerLayerIndex = transform.parent.Find("Player Movement").GetComponent<FloorAgent>().currentFloorIndex;
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
        if(GameManager.Instance.gameContext.InputManager.GetMovementInput() != Vector2.zero)
            direction = GameManager.Instance.gameContext.InputManager.GetMovementInput();

        if (direction != Vector2.zero)
        {
            Vector2 origin = transform.position;
            float rayDistance = interactionCollider.radius * 0.65f;

            // Dùng RaycastNonAlloc thay vì RaycastAll
            int hitCount = Physics2D.RaycastNonAlloc(origin, direction, raycastResults, rayDistance, LayerMask.GetMask("Default"));

            // Dùng vòng lặp for dựa trên hitCount
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = raycastResults[i];
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

        #region Raycast Other Objects (Đã tối ưu NonAlloc)
        if (currentObject == null)
        {
            // Trả về số lượng object thực sự chạm vào mảng
            int interactCount = Physics2D.OverlapCircleNonAlloc(transform.position, interactionCollider.radius, interactResults);
            int playerCount = Physics2D.OverlapCircleNonAlloc(transform.position, playerCollider.radius * 1.25f, playerResults);

            // Bắt buộc dùng vòng lặp for với biến đếm Count
            for (int i = 0; i < interactCount; i++)
            {
                Collider2D interactCol = interactResults[i];

                for (int j = 0; j < playerCount; j++)
                {
                    Collider2D pCol = playerResults[j];

                    if (interactCol != playerCollider && interactCol == pCol)
                    {
                        if (interactCol.CompareTag("Tree"))
                        {
                            currentObject = interactCol.gameObject;
                            var tree = currentObject.GetComponent<Tree>();
                            LookUpLayerIndex();
                            var task = tree.GetTask();

                            if (layerIndex == playerLayerIndex &&
                                (task == null || task.targetGameObject == null) && tree.treeState != TreeState.Chopped)
                            {
                                interactButtonScript.ChangeInteractButtonState(InteractButtonState.Cut);
                                interactButtonState = InteractButtonState.Cut;
                                break; // Break khỏi vòng lặp trong
                            }
                            else
                            {
                                currentObject = null;
                            }
                        }
                    }
                }

                // Nếu đã tìm thấy object, break khỏi vòng lặp ngoài luôn cho nhẹ máy
                if (currentObject != null)
                    break;
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
            interactButton = GameManager.Instance.gameContext.UIManager.GetUI(GameStateType.Playing, UINames.InteractButton);
            if(interactButton.activeInHierarchy)
                interactButton.SetActive(false);
            
            interactButtonScript = interactButton.GetComponent<InteractButton>();
            interactButtonRect = interactButton.GetComponent<RectTransform>();
        }
    }
}