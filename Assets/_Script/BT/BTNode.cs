using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BTNode
{
    public abstract BTStatus Tick();
    public virtual void ClearState() {} 
}
