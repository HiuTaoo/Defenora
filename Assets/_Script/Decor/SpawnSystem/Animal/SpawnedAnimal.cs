using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnedAnimal
{
    public GameObject animalObject;
    public Vector3Int gridPosition;
    public int layerIndex;
    public Animal animalComponent;

    public SpawnedAnimal(GameObject animalObj, Vector3Int gridPos)
    {
        animalObject = animalObj;
        gridPosition = gridPos;
        animalComponent = animalObj.GetComponent<Animal>();
        layerIndex = animalComponent.layerIndex;
    }
}