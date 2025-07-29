using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraConfig 
{
    public bool FollowPlayer { get; set; } = false;
    public Vector3 Position { get; set; } = Vector3.zero;
    public Vector3 Rotation { get; set; } = Vector3.zero;
    public bool IsOrthographic { get; set; } = true;
    public float OrthographicSize { get; set; } = 5f;
    public bool SmoothTransition { get; set; } = true;
    public float TransitionDuration { get; set; } = 1f;
}
