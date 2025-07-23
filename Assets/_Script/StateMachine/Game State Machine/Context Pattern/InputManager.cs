using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public bool isBlockMovementInput = false;
    public bool GetKeyDown(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }

    public bool GetKeyUp(KeyCode key)
    {
        return Input.GetKeyUp(key);
    }

    public bool GetKey(KeyCode key)
    {
        return Input.GetKey(key);
    }

    public Vector2 GetMovementInput()
    {
        if(!isBlockMovementInput)
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        else
            return Vector2.zero;
    }
}
