using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager
{
    public Camera mainCamera;
    public CinemachineVirtualCamera virtualCamera;

    private Transform playerTransform;
    private Dictionary<GameStateType, CameraConfig> cameraConfigs;
    public Coroutine currentTransition;

    public bool isFollowingPlayer = false;

    public CameraManager(Camera camera)
    {
        mainCamera = camera;
        virtualCamera = camera.GetComponentInChildren<CinemachineVirtualCamera>();
        cameraConfigs = new Dictionary<GameStateType, CameraConfig>();
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
                currentTransition = GameManager.Instance.StartCoroutine(TransitionToConfig(config));
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

    public void LerpToPosition(Vector3 position)
    {

    }

    #endregion
}