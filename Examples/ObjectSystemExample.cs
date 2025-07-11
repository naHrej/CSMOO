using System;
using CSMOO.Server.Database;

namespace CSMOO.Examples;

/// <summary>
/// Demonstrates the object-oriented inheritance system
/// </summary>
public static class ObjectSystemExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== Object-Oriented MUSH System Example ===\n");

        // Create a weapon class that inherits from Item
        var itemClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Item");
        var weaponClass = ObjectManager.CreateClass("Weapon", itemClass?.Id, "A weapon that can be wielded");
        
        // Add weapon-specific properties
        weaponClass.Properties["damage"] = 10;
        weaponClass.Properties["weaponType"] = "melee";
        weaponClass.Properties["durability"] = 100;
        weaponClass.Properties["maxDurability"] = 100;
        GameDatabase.Instance.ObjectClasses.Update(weaponClass);

        // Create a sword class that inherits from Weapon
        var swordClass = ObjectManager.CreateClass("Sword", weaponClass.Id, "A sharp blade for combat");
        swordClass.Properties["damage"] = 15; // Override weapon damage
        swordClass.Properties["weaponType"] = "sword";
        swordClass.Properties["longDescription"] = "A gleaming steel sword with a sharp edge.";
        GameDatabase.Instance.ObjectClasses.Update(swordClass);

        // Create an instance of a sword
        var sword = ObjectManager.CreateInstance(swordClass.Id);
        ObjectManager.SetProperty(sword, "name", "Excalibur");
        ObjectManager.SetProperty(sword, "shortDescription", "the legendary sword Excalibur");
        ObjectManager.SetProperty(sword, "damage", 25); // This specific sword does more damage
        ObjectManager.SetProperty(sword, "value", 1000);

        Console.WriteLine("Created inheritance chain: Object -> Item -> Weapon -> Sword");
        Console.WriteLine($"Sword instance: {ObjectManager.GetProperty(sword, "name")}");
        
        // Demonstrate inheritance - sword gets properties from all parent classes
        Console.WriteLine("\nInherited Properties:");
        Console.WriteLine($"- gettable (from Object): {ObjectManager.GetProperty(sword, "gettable")}");
        Console.WriteLine($"- weight (from Item): {ObjectManager.GetProperty(sword, "weight")}");
        Console.WriteLine($"- weaponType (from Weapon): {ObjectManager.GetProperty(sword, "weaponType")}");
        Console.WriteLine($"- damage (overridden): {ObjectManager.GetProperty(sword, "damage")}");
        Console.WriteLine($"- value (instance-specific): {ObjectManager.GetProperty(sword, "value")}");

        // Show how you can find all weapons (including swords)
        var allWeapons = ObjectManager.FindObjectsByClass(weaponClass.Id, includeSubclasses: true);
        Console.WriteLine($"\nFound {allWeapons.Count} weapon(s) in the world (including subclasses)");

        // Create a room and place the sword there
        var startingRoom = WorldManager.GetStartingRoom();
        if (startingRoom != null)
        {
            ObjectManager.MoveObject(sword.Id, startingRoom.Id);
            Console.WriteLine($"\nMoved {ObjectManager.GetProperty(sword, "name")} to {ObjectManager.GetProperty(startingRoom, "name")}");
            
            var roomContents = ObjectManager.GetObjectsInLocation(startingRoom.Id);
            Console.WriteLine($"Room now contains {roomContents.Count} objects");
        }

        Console.WriteLine("\n=== Example Complete ===");
    }
}
