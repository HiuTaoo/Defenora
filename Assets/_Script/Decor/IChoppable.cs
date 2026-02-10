using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IChoppable
{
    bool IsClaimed { get; }
    bool TryClaim(Builder builder);
    void Release(Builder builder);
    
    System.Action<IChoppable> OnChoppedObject { get; set; }
    void OnChopped();
    void HandleChopped();
}
