using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorEnvironmentObject : MonoBehaviour
{
    [SerializeField] private int floorIndex;

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            FloorCollisionManager.Instance.RegisterCollider(col, floorIndex);
        }
    }
}
