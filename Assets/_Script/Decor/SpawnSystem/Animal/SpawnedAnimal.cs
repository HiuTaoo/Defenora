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

    public SpawnedAnimal(GameObject animalObj, Vector3Int gridPos, int layer)
    {
        animalObject = animalObj;
        gridPosition = gridPos;
        layerIndex = layer;
        animalComponent = animalObj.GetComponent<Animal>();
    }
}