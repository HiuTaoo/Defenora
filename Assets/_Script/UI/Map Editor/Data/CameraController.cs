using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Camera sceneCamera;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 20f;

    private Vector3 lastMousePosition;

    private void Update()
    {
        CameraDragging();
        CameraZoom();
        MouseIndicator();

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
            float targetSize = sceneCamera.orthographicSize - scroll * zoomSpeed;
            sceneCamera.orthographicSize = Mathf.Clamp(targetSize, minZoom, maxZoom);
        }
    }

    private void MouseIndicator()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            MenuTilesController.Instance.mouseIndicator.SetActive(false);
        }
        else
        {
            if (MenuTilesController.Instance.mouseIndicator.GetComponent<Image>().sprite != null)
            {
                MenuTilesController.Instance.mouseIndicator.SetActive(true);
                Vector3 offsetPosition = Input.mousePosition + new Vector3(0.5f, -0.5f, 0f);
                MenuTilesController.Instance.mouseIndicator.transform.position = offsetPosition;
            }
            else
            {
                MenuTilesController.Instance.mouseIndicator.SetActive(false);
            }
        }
    }
}
