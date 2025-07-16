using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sheep : MonoBehaviour
{
    private CapsuleCollider2D collider2D;
    private Rigidbody rb;
    private Animator animator;

    public bool isDangerous = false;
    private System.Random random;

    private void Awake()
    {
        collider2D = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        random = new System.Random();
    }

    private void Start()
    {
        animator.Play("Idle");  
    }

    private void Update()
    {
        if (!isDangerous)
        {

        }
    }

}
