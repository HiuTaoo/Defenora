using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Windows;

public class Sheep : Animal
{
    protected void Start()
    {
        animator.Play("Idle");
        animalType = AnimalType.Sheep;
        checkDangerCoroutine = StartCoroutine(CheckDangerLoop());
    }

    protected void Update()
    {
        if (!isDangerous)
        {
            if (randomAnimationCoroutine == null)
            {
                randomAnimationCoroutine = StartCoroutine(RandomAnimationLoop());
            }
        }

        layerIndex = floorAgent.currentFloorIndex;
        HandleFlipDirection();
    }

    protected void FixedUpdate()
    {
        if (isDangerous)
        {
            if (randomAnimationCoroutine != null)
            {
                StopCoroutine(randomAnimationCoroutine);
                randomAnimationCoroutine = null;
            }

            if (panicCoroutine == null)
            {
                StartPanicRun();
            }

            PanicMove();
        }
        else
        {
            rb.velocity = Vector2.zero;

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
            {
                animator.Play("Idle");
            }

            if (randomAnimationCoroutine == null)
            {
                randomAnimationCoroutine = StartCoroutine(RandomAnimationLoop());
            }
        }
    }


    #region Random Animation Loop
    protected override IEnumerator RandomAnimationLoop()
    {
        while (!isDangerous)
        {
            int waitTime = random.Next(3, maxDuration);
            yield return new WaitForSeconds(waitTime);

            int nextState = random.Next(0, 2);
            switch (nextState)
            {
                case 0:
                    animator.Play("Idle");
                    break;
                case 1:
                    animator.Play("Eat");
                    break;
            }
        }

        randomAnimationCoroutine = null;
    }
    #endregion
}
