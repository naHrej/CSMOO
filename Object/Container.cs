using CSMOO.Logging;

namespace CSMOO.Object;

public class Container : GameObject
{
    public List<Item> Items { get; set; } = new List<Item>();
    public int MaxCapacity { get; set; } = 10;

    public Container(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public bool AddItem(Item item)
    {
        if (Items.Count >= MaxCapacity)
        {
            Logger.Warning($"Cannot add item '{item.Name}' to container '{Name}': capacity reached.");
            return false;
        }
        Items.Add(item);
        Logger.Info($"Item '{item.Name}' added to container '{Name}'.");
        return true;
    }

    public bool RemoveItem(Item item)
    {
        if (Items.Remove(item))
        {
            Logger.Info($"Item '{item.Name}' removed from container '{Name}'.");
            return true;
        }
        Logger.Warning($"Item '{item.Name}' not found in container '{Name}'.");
        return false;
    }
}