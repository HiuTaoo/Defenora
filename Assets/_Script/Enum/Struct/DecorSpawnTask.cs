public struct DecorSpawnTask
{
    public DecorType type;
    public int layerIndex;
    public object dataReference;

    public DecorSpawnTask(DecorType type, int layerIndex, object dataReference)
    {
        this.type = type;
        this.layerIndex = layerIndex;
        this.dataReference = dataReference;
    }
}