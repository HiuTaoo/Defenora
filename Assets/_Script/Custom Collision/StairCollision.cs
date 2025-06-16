using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StairCollision : MonoBehaviour
{
    private CircleCollider2D collider2D;

    private void Awake()
    {
        collider2D = GetComponent<CircleCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
    }
}
