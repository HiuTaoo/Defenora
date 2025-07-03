using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    private Camera sceneCamera;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 20f;

    private Vector3 lastMousePosition;
    private CinemachineVirtualCamera virtualCamera;
    private void Awake()
    {
        sceneCamera = Camera.main;

        if (sceneCamera == null)
        {
            Debug.LogError("CameraController: No main Camera found!");
        }

        virtualCamera = sceneCamera.GetComponentInChildren<CinemachineVirtualCamera>();

        if (virtualCamera == null)
        {
            Debug.LogError("CameraController: No CinemachineVirtualCamera found!");
        }
    }


    private void Update()
    {
        if(GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Editor) {
            CameraDragging();
            CameraZoom();
        }
        

    }

    public void CameraDragging()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(1))
        {
            DragCamera();
        }
    }

    private void DragCamera()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 delta = sceneCamera.ScreenToWorldPoint(lastMousePosition) - sceneCamera.ScreenToWorldPoint(currentMousePosition);

        sceneCamera.transform.position += delta;
        lastMousePosition = currentMousePosition;
    }

    private void CameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            float targetSize = virtualCamera.m_Lens.OrthographicSize - scroll * zoomSpeed;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(targetSize, minZoom, maxZoom);
        }
    }


}
