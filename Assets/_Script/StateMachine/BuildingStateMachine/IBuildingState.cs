using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBuildingState 
{
    void OnEnter();
    void OnExit();
    void Update();
    void FixedUpdate();
}
