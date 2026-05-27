using System;
using System.Collections.Generic;

public interface IStorage
{
    event Action OnContentChanged;
    
    bool CanStore(ItemData itemData, int amount);
    int Add(ItemData itemData, int amount);
    int Remove(ItemData itemData, int amount);
    int GetAmount(ItemData itemData);
    
    Dictionary<ItemData, int> GetAllItems();
}