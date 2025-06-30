using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIConfig 
{
    public bool FadeIn { get; set; } = false;
    public Vector3 Scale { get; set; } = Vector3.one;
    public float TransitionDuration { get; set; } = 0.3f;
    public bool BlockInput { get; set; } = false;
}
