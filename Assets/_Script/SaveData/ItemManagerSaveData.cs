using System;
using System.Collections.Generic;

[Serializable]
public class ItemManagerSaveData
{
    public List<ItemSaveData> items = new List<ItemSaveData>();
    public List<CoinSaveData> coins = new();
}