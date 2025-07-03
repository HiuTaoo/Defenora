using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager
{
    private Camera mainCamera;
    private CinemachineVirtualCamera virtualCamera;

    private Transform playerTransform;
    private Dictionary<GameStateType, CameraConfig> cameraConfigs;
    private Coroutine currentTransition;

    // Camera follow variables
    private bool isFollowingPlayer = false;
    private Vector3 followOffset = new Vector3(0, 2, -5);
    private float followSpeed = 5f;

    public CameraManager(Camera camera)
    {
        mainCamera = camera;
        virtualCamera = camera.GetComponentInChildren<CinemachineVirtualCamera>();
        cameraConfigs = new Dictionary<GameStateType, CameraConfig>();
    }

    public void RegisterCameraConfig(GameStateType stateType, CameraConfig config)
    {
        cameraConfigs[stateType] = config;
    }

    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }

    public void ApplyCameraSettings(GameStateType stateType)
    {
        Debug.Log($"CameraManager: Applying camera settings for {stateType}");

        if (cameraConfigs.ContainsKey(stateType))
        {
            var config = cameraConfigs[stateType];

            // Stop current transition if any
            if (currentTransition != null)
            {
                GameLoop.Instance.StopCoroutine(currentTransition);
            }

            if (config.SmoothTransition)
            {
                currentTransition = GameLoop.Instance.StartCoroutine(TransitionToConfig(config));
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
                //virtualCamera.Follow = config.FollowPlayer ? playerTransform : null;
                virtualCamera.Follow = null;
                virtualCamera.ForceCameraPosition(
                    virtualCamera.transform.position,
                    virtualCamera.transform.rotation);
                virtualCamera.PreviousStateIsValid = false;

            }
            if (config.FollowPlayer)
            {
                virtualCamera.Follow = playerTransform;
            }
        }
        

        mainCamera.orthographic = config.IsOrthographic;

        if (config.IsOrthographic)
        {
            mainCamera.orthographicSize = config.OrthographicSize;
        }

        virtualCamera.m_Lens.OrthographicSize = config.OrthographicSize;

    }


    private IEnumerator TransitionToConfig(CameraConfig config)
    {
        Debug.Log("CameraManager: Transitioning to config...");

        // Stop follow during transition if needed
        bool wasFollowing = isFollowingPlayer;
        isFollowingPlayer = false;

        Vector3 startPos, targetPos;
        Quaternion startRot, targetRot;

        if (virtualCamera != null && virtualCamera.Follow == null)
        {
            startPos = virtualCamera.transform.position;
            startRot = virtualCamera.transform.rotation;

            targetPos = config.Position;
            targetRot = Quaternion.Euler(config.Rotation);
        }
        else
        {
            startPos = mainCamera.transform.position;
            startRot = mainCamera.transform.rotation;

            targetPos = config.Position;
            targetRot = Quaternion.Euler(config.Rotation);
        }

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

        // Resume follow if needed
        isFollowingPlayer = config.FollowPlayer;

        Debug.Log("CameraManager: Transition completed.");
    }


    public void Update()
    {
   
    }
    

    public void SetFollowOffset(Vector3 offset)
    {
        followOffset = offset;
    }

    public void SetFollowSpeed(float speed)
    {
        followSpeed = speed;
    }
}