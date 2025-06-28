using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    private void Awake()
    {
        if (Instance == null )
            Instance = this;
        else
            Destroy(gameObject);
    }
}
