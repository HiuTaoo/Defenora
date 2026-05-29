using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.UI.UI_Script;
using UnityEngine.UI;
using TMPro; 

public class TrainingWindowUI : MonoBehaviour
{
    public static TrainingWindowUI Instance;

    [Header("--- Sub Panels Reference ---")]
    public RectTransform rightPageContent; 
    public RectTransform trainingPageContent;

    [Header("--- Target Class Custom View ---")]
    [SerializeField] private RectTransform parentTargetClass; 
    [SerializeField] private RectTransform civilianParent; 
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button trainButton; 
    
    private TrainingBuilding currentActiveBuilding;
    private List<TrainingQueueSlotUI> activeSlotUIs = new List<TrainingQueueSlotUI>();
    
    private List<GameObject> targetClassSpawnedObjects = new List<GameObject>();
    private List<GameObject> civilianSpawnedObjects = new List<GameObject>();

    private int currentSelectedConfigIndex = 0;

    private void Awake()
    {
        Instance = this;
        trainingPageContent.gameObject.SetActive(false);
        
        trainButton.onClick.AddListener(TrainFirstAvailableCivilian);
    }

    public void OpenWindow(Building selectedBuilding)
    {
        if (selectedBuilding is TrainingBuilding trainingBuilding)
        {
            currentActiveBuilding = trainingBuilding;
            trainingPageContent.gameObject.SetActive(true);
            
            currentSelectedConfigIndex = 0;

            RefreshLeftPage();
            RefreshRightPage();
        }
    }

    private void OnPlusButtonClicked()
    {
        if (currentActiveBuilding == null) return;

        TrainingConfig[] configs = currentActiveBuilding.GetAvailableConfigs();
        if (configs == null || configs.Length <= 1) return;

        currentSelectedConfigIndex = (currentSelectedConfigIndex + 1) % configs.Length;

        RefreshLeftPage();
    }

    private void UpdateTargetClassDisplay(TrainingConfig[] configs)
    {
        ClearTargetClassView();

        if (configs == null || configs.Length == 0) return;

        TrainingConfig activeConfig = configs[currentSelectedConfigIndex];

        if (activeConfig.unitPrefab != null)
        {
            var unitComponent = activeConfig.unitPrefab.GetComponent<Unit>();
            
            if (unitComponent != null)
            {
                var unitIcon = PoolManager.Instance.Spawn(PrefabConfig.Instance.unitIconPrefab,
                    parentTargetClass.transform.position, Quaternion.identity);
                
                unitIcon.transform.SetParent(parentTargetClass);
                unitIcon.transform.localScale = Vector3.one;
                unitIcon.gameObject.SetActive(true); 
                targetClassSpawnedObjects.Add(unitIcon);

                descriptionText.text = $"Train Civilian to {unitComponent.unitType}";

                var unitIconComp = unitIcon.GetComponent<UnitSlotUI>();
                if (unitIconComp != null)
                {
                    unitIconComp.Setup(unitComponent, null); 
                    unitIconComp.OnPointerUp(null); 
                }

                var iconButton = unitIcon.GetComponent<Button>();
                if (iconButton == null)
                {
                    iconButton = unitIcon.GetComponentInChildren<Button>();
                }

                if (iconButton != null)
                {
                    iconButton.onClick.RemoveAllListeners();
                    iconButton.interactable = false; 
                }
            }
        }

        if (configs.Length > 1)
        {
            var addButton = PoolManager.Instance.Spawn(PrefabConfig.Instance.addUnitButtonPrefab,
                parentTargetClass.transform.position, Quaternion.identity);
            
            addButton.transform.SetParent(parentTargetClass);
            addButton.transform.localScale = Vector3.one;
            addButton.gameObject.SetActive(true);
            targetClassSpawnedObjects.Add(addButton);

            var button = addButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnPlusButtonClicked);
            }
        }
    }

    private void TrainFirstAvailableCivilian()
    {
        if (currentActiveBuilding == null) return;

        TrainingConfig[] configs = currentActiveBuilding.GetAvailableConfigs();
        if (configs == null || configs.Length == 0) return;

        UnitType dynamicTargetType = configs[currentSelectedConfigIndex].targetType;

        var firstCivilian = UnitManager.Instance.allUnits
            .FirstOrDefault(u => u.unitType == UnitType.Civilian && u.assignedBuilding == null);

        if (firstCivilian != null)
        {
            if (currentActiveBuilding.CanAddUnit(firstCivilian))
            {
                currentActiveBuilding.AddTraineeWithSelection(firstCivilian, dynamicTargetType);
                
                ForceRefreshFullWindow();
            }
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Civilian rảnh rỗi nào ngoài làng để huấn luyện!");
        }
    }

    private void ClearTargetClassView()
    {
        foreach (var obj in targetClassSpawnedObjects)
        {
            if (obj != null)
            {
                var btn = obj.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveAllListeners();

                PoolManager.Instance.Despawn(obj);
            }
        }
        targetClassSpawnedObjects.Clear();
    }

    private void ClearCivilianListView()
    {
        foreach (var obj in civilianSpawnedObjects)
        {
            if (obj != null)
            {
                var btn = obj.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveAllListeners();

                PoolManager.Instance.Despawn(obj);
            }
        }
        civilianSpawnedObjects.Clear();
    }

    public void ForceRefreshFullWindow()
    {
        RefreshLeftPage();
        RefreshRightPage();
        
        Canvas.ForceUpdateCanvases();
        if (civilianParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(civilianParent);
        }
    }

    public void RefreshLeftPage()
    {
        if (currentActiveBuilding == null) return;

        TrainingConfig[] configs = currentActiveBuilding.GetAvailableConfigs();
        
        UpdateTargetClassDisplay(configs);
        ClearCivilianListView();

        if (civilianParent == null) return;

        var freeCivilians = UnitManager.Instance.allUnits
            .Where(u => u.unitType == UnitType.Civilian && u.assignedBuilding == null);

        TrainingConfig selectedConfig = configs[currentSelectedConfigIndex];

        foreach (var civilian in freeCivilians)
        {
            var civilianIconObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.unitIconPrefab,
                civilianParent.transform.position, Quaternion.identity);

            civilianIconObj.transform.SetParent(civilianParent);
            civilianIconObj.transform.localScale = Vector3.one;
            civilianIconObj.gameObject.SetActive(true);
            
            civilianSpawnedObjects.Add(civilianIconObj);

            var unitSlotComp = civilianIconObj.GetComponent<UnitSlotUI>();
            if (unitSlotComp != null)
            {
                unitSlotComp.Setup(civilian, null);
                unitSlotComp.OnPointerUp(null);

                var civilianButton = civilianIconObj.GetComponent<Button>();
                if (civilianButton == null)
                {
                    civilianButton = civilianIconObj.GetComponentInChildren<Button>();
                }

                if (civilianButton != null)
                {
                    civilianButton.onClick.RemoveAllListeners();
                    civilianButton.interactable = false; 
                }
            }
        }
    }

    public void RefreshRightPage()
    {
        foreach (var slotUI in activeSlotUIs)
        {
            if (slotUI != null) PoolManager.Instance.Despawn(slotUI.gameObject);
        }
        activeSlotUIs.Clear();

        if (currentActiveBuilding == null) return;

        var activeTrainees = currentActiveBuilding.GetTraineesSaveData();
        foreach (var trainee in activeTrainees)
        {
            var slotObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.trainingQueueSlotUIPrefab,
                rightPageContent.transform.position, Quaternion.identity);
            
            slotObj.transform.SetParent(rightPageContent);
            slotObj.transform.localScale = Vector3.one; 
            slotObj.gameObject.SetActive(true);

            TrainingQueueSlotUI slotUI = slotObj.GetComponent<TrainingQueueSlotUI>();
            Unit unitEntity = UnitManager.Instance.allUnits.FirstOrDefault(u => u.GetId() == trainee.unitID);
            TrainingConfig config = currentActiveBuilding.GetAvailableConfigs().FirstOrDefault(c => c.targetType == trainee.targetType);

            if (unitEntity != null)
            {
                activeSlotUIs.Add(slotUI);
                slotUI.SetupSlot(unitEntity, config, currentActiveBuilding);
            }
        }
    }

    private void Update()
    {
        if (currentActiveBuilding == null || !trainingPageContent.gameObject.activeSelf) return;

        var currentDataList = currentActiveBuilding.GetTraineesSaveData();
        
        if (currentDataList.Count != activeSlotUIs.Count)
        {
            ForceRefreshFullWindow();
            return;
        }

        for (int i = 0; i < activeSlotUIs.Count; i++)
        {
            if (activeSlotUIs[i] == null) continue;
            activeSlotUIs[i].UpdateSliderProgress(currentDataList[i].currentTrainingHours);
        }
    }

    public void CloseWindow()
    {
        currentActiveBuilding = null;
        trainingPageContent.gameObject.SetActive(false);
        
        ClearTargetClassView();
        ClearCivilianListView();

        foreach (var slotUI in activeSlotUIs)
        {
            if (slotUI != null) PoolManager.Instance.Despawn(slotUI.gameObject);
        }
        activeSlotUIs.Clear();
    }
}