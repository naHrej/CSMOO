namespace CSMOO.Object;

class Item : GameObject
{
    public string Description { get; set; } = "An item in the game world.";
    public int Weight { get; set; } = 1;

    public Item(string id, string name)
    {
        Id = id;
        Name = name;
    }
}