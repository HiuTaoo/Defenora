using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IChoppable
{
    System.Action<IChoppable> OnChoppedObject { get; set; }
    void OnChopped();
    void HandleChopped();
}
