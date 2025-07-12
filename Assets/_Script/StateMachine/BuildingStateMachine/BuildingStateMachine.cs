using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingStateMachine : MonoBehaviour
{
    private IBuildingState currentBuildingState;

    public IBuildingState CurrentBuildingState => currentBuildingState;

    public void ChangeBuildingState(IBuildingState newState)
    {
        if(newState == null || currentBuildingState == newState)
            return;

        currentBuildingState?.OnExit();
        currentBuildingState = newState;
        currentBuildingState.OnEnter();

    }

    public void Update() => currentBuildingState?.Update();
    public void FixedUpdate() => currentBuildingState?.FixedUpdate();

}
