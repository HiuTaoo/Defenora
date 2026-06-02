using UnityEngine;

public class Bush : DecorObject
{
    public Vector3Int positionInGrid;
    public bool isDecorative = true;

    public override void OnChopped()
    {
        base.OnChopped();
        ObjectSpawner.Instance.RegisterHarvestedBush(this);
    }
}