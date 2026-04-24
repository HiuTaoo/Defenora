using System;

public interface IStorage
{
    event Action OnContentChanged;
    
    bool CanStore(ResourceType type, int amount);
    int Add(ResourceType type, int amount);
    int Remove(ResourceType type, int amount);
    int GetAmount(ResourceType type);
}