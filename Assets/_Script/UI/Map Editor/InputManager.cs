using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField]
    private Camera _sceneCamera;
    private Vector2 lastPosition;
    [SerializeField]
    private LayerMask placementLayerMask;
}
