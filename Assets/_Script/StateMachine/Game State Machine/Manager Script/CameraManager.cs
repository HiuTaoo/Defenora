using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager: MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("References")]
    public Camera mainCamera;
    public CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform playerTransform;

    private Dictionary<GameStateType, CameraConfig> cameraConfigs;
    public Coroutine currentTransition;
    public bool isFollowingPlayer = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cameraConfigs = new Dictionary<GameStateType, CameraConfig>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (virtualCamera == null) virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        RegisterCameraConFig();
    }

    private void Start()
    {
        if (SelectUnitSystem.Instance != null)
        {
            SelectUnitSystem.Instance.OnLerpToSelectedUnit += LerpToPosition;
        }
    }

    private void RegisterCameraConFig()
    {
        SetPlayerTransform(playerTransform);

        RegisterCameraConfig(GameStateType.Playing, new CameraConfig
        {
            FollowPlayer = true,
            SmoothTransition = true,
            OrthographicSize = 5,
            TransitionDuration = 1f
        });

        RegisterCameraConfig(GameStateType.Editor, new CameraConfig
        {
            FollowPlayer = false,
            SmoothTransition = true,
            TransitionDuration = 1.5f
        });

        RegisterCameraConfig(GameStateType.Paused, new CameraConfig
        {
            FollowPlayer = true, 
            SmoothTransition = false
        });
    }

    private void OnDestroy()
    {
        if (SelectUnitSystem.Instance != null)
        {
            SelectUnitSystem.Instance.OnLerpToSelectedUnit -= LerpToPosition;
        }
    }

    #region ApplyCameraSettings
    public void ApplyCameraSettings(GameStateType stateType)
    {
        Debug.Log($"CameraManager: Applying camera settings for {stateType}");

        if (cameraConfigs.ContainsKey(stateType))
        {
            var config = cameraConfigs[stateType];

            // Stop current transition if any
            if (currentTransition != null)
            {
                GameManager.Instance.StopCoroutine(currentTransition);
            }

            if (config.SmoothTransition)
            {
                currentTransition = StartCoroutine(TransitionToConfig(config));
            }
            else
            {
                ApplyConfigImmediate(config);
            }
        }
        else
        {
            Debug.LogWarning($"CameraManager: No camera config for state {stateType}");
        }
    }


    private void ApplyConfigImmediate(CameraConfig config)
    {
        isFollowingPlayer = config.FollowPlayer;

        if (virtualCamera != null)
        {
            if (!config.FollowPlayer)
            {
                Vector3 currentPosition = mainCamera.transform.position;
                Quaternion currentRotation = mainCamera.transform.rotation;

                virtualCamera.Follow = null;

                virtualCamera.ForceCameraPosition(currentPosition, currentRotation);
                virtualCamera.PreviousStateIsValid = false;

            }
            if (config.FollowPlayer)
            {
                virtualCamera.PreviousStateIsValid = false;
                virtualCamera.Follow = playerTransform;
            }
        }
        

        mainCamera.orthographic = config.IsOrthographic;

        if (config.IsOrthographic)
        {
            mainCamera.orthographicSize = config.OrthographicSize;
            virtualCamera.m_Lens.OrthographicSize = config.OrthographicSize;
        }
    }


    private IEnumerator TransitionToConfig(CameraConfig config)
    {
        Debug.Log("CameraManager: Transitioning to config...");

        isFollowingPlayer = false;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 targetPos = config.Position;
        Quaternion targetRot = Quaternion.Euler(config.Rotation);

        if (isFollowingPlayer)
        {
            virtualCamera.Follow = null;
            virtualCamera.ForceCameraPosition(startPos, startRot);
            virtualCamera.PreviousStateIsValid = false;
        }

        isFollowingPlayer = false;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, config.TransitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            if (!config.FollowPlayer)
            {
                if (virtualCamera != null && virtualCamera.Follow == null)
                {
                    virtualCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    virtualCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
                }
                else
                {
                    mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
                }
            }

            yield return null;
        }

        ApplyConfigImmediate(config);

        isFollowingPlayer = config.FollowPlayer;

        Debug.Log("CameraManager: Transition completed.");
    }
    #endregion

    #region Camera Method
    public void RegisterCameraConfig(GameStateType stateType, CameraConfig config)
    {
        cameraConfigs[stateType] = config;
    }

    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }

    // 4. Hàm này giờ có thể TỰ GỌI Coroutine vì nó là MonoBehaviour
    public void LerpToPosition(Vector3 position)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        currentTransition = StartCoroutine(LerpPositionWithDisabledVCam(position, 0.5f));
    }

    private IEnumerator LerpPositionWithDisabledVCam(Vector3 targetPosition, float duration)
    {
        // ... (Giữ nguyên nội dung hàm Lerp cũ của bạn, vì giờ nó tự gọi StartCoroutine được rồi) ...
        bool wasFollowing = virtualCamera.Follow != null;
        Transform originalFollow = virtualCamera.Follow;

        virtualCamera.gameObject.SetActive(false);
        isFollowingPlayer = false;

        yield return null;

        Vector3 startPos = mainCamera.transform.position;
        float elapsed = 0f;
        float originalZ = startPos.z;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 lerpedPosition = Vector3.Lerp(startPos, targetPosition, t);
            lerpedPosition.z = originalZ;
            mainCamera.transform.position = lerpedPosition;
            yield return null;
        }

        mainCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, originalZ);
        virtualCamera.gameObject.SetActive(true);

        if (wasFollowing)
        {
            virtualCamera.Follow = originalFollow;
            isFollowingPlayer = true;
        }

        currentTransition = null;
    }
    #endregion
}