using System;
using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Object;

namespace CSMOO.Examples;

/// <summary>
/// Demonstrates the object-oriented inheritance system
/// </summary>
public static class ObjectSystemExample
{
    public static void RunExample()
    {
        Logger.Info("=== Object-Oriented MUSH System Example ===\n");

        // Create a weapon class that inherits from Item
        var itemClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Item");
        var weaponClass = ObjectManager.CreateClass("Weapon", itemClass?.Id, "A weapon that can be wielded");
        
        // Add weapon-specific properties
        weaponClass.Properties["damage"] = 10;
        weaponClass.Properties["weaponType"] = "melee";
        weaponClass.Properties["durability"] = 100;
        weaponClass.Properties["maxDurability"] = 100;
        DbProvider.Instance.Update<ObjectClass>("objectclasses", weaponClass);

        // Create a sword class that inherits from Weapon
        var swordClass = ObjectManager.CreateClass("Sword", weaponClass.Id, "A sharp blade for combat");
        swordClass.Properties["damage"] = 15; // Override weapon damage
        swordClass.Properties["weaponType"] = "sword";
        swordClass.Properties["longDescription"] = "A gleaming steel sword with a sharp edge.";
        DbProvider.Instance.Update<ObjectClass>("objectclasses", swordClass);

        // Create an instance of a sword
        var sword = ObjectManager.CreateInstance(swordClass.Id);
        ObjectManager.SetProperty(sword, "name", "Excalibur");
        ObjectManager.SetProperty(sword, "shortDescription", "the legendary sword Excalibur");
        ObjectManager.SetProperty(sword, "damage", 25); // This specific sword does more damage
        ObjectManager.SetProperty(sword, "value", 1000);

        Logger.Info("Created inheritance chain: Object -> Item -> Weapon -> Sword");
        Logger.Info($"Sword instance: {ObjectManager.GetProperty(sword, "name")}");
        
        // Demonstrate inheritance - sword gets properties from all parent classes
        Logger.Info("\nInherited Properties:");
        Logger.Info($"- gettable (from Object): {ObjectManager.GetProperty(sword, "gettable")}");
        Logger.Info($"- weight (from Item): {ObjectManager.GetProperty(sword, "weight")}");
        Logger.Info($"- weaponType (from Weapon): {ObjectManager.GetProperty(sword, "weaponType")}");
        Logger.Info($"- damage (overridden): {ObjectManager.GetProperty(sword, "damage")}");
        Logger.Info($"- value (instance-specific): {ObjectManager.GetProperty(sword, "value")}");

        // Show how you can find all weapons (including swords)
        var allWeapons = ObjectManager.FindObjectsByClass(weaponClass.Id, includeSubclasses: true);
        Logger.Info($"\nFound {allWeapons.Count} weapon(s) in the world (including subclasses)");

        // Create a room and place the sword there
        var startingRoom = WorldManager.GetStartingRoom();
        if (startingRoom != null)
        {
            ObjectManager.MoveObject(sword.Id, startingRoom.Id);
            Logger.Info($"\nMoved {ObjectManager.GetProperty(sword, "name")} to {ObjectManager.GetProperty(startingRoom, "name")}");
            
            var roomContents = ObjectManager.GetObjectsInLocation(startingRoom.Id);
            Logger.Info($"Room now contains {roomContents.Count} objects");
        }

        Logger.Info("\n=== Example Complete ===");
    }
}

