using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameState 
{
    void Enter(GameStateContext context);
    void Exit(GameStateContext context);
    void Tick(GameStateContext context);

}
