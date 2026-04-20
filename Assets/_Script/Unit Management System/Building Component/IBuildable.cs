using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBuildable 
{
    System.Action<IBuildable> OnBuiltObject { get; set; }
    void OnBuild();
    void HandleBuilt(float workRate);
}
