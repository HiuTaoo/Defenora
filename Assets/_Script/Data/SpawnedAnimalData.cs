using UnityEngine;

[System.Serializable]
public class SpawnedAnimalData
{
    public Vector3 currentPosition;
    public int layerIndex;
    public int prefabIndex;

    public SpawnedAnimalData(SpawnedAnimal animal, int prefabIdx)
    {
        currentPosition = animal.animalObject.transform.position;
        layerIndex = animal.layerIndex;
        prefabIndex = prefabIdx;
    }
}