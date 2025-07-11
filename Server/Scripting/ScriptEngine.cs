using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using LiteDB;
using CSMOO.Server.Database;
using CSMOO.Server.Commands;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Executes C# scripts in a sandboxed environment with access to game objects
/// </summary>
public class ScriptEngine
{
    private readonly ScriptOptions _scriptOptions;

    public ScriptEngine()
    {
        // Set up script options with necessary references
        _scriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,                    // System.Object
                typeof(Console).Assembly,                   // System.Console
                typeof(Enumerable).Assembly,                // System.Linq
                typeof(GameObject).Assembly,                // Our game objects
                typeof(ObjectManager).Assembly,             // Our managers
                Assembly.GetExecutingAssembly()             // Current assembly
            )
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "CSMOO.Server.Database",
                "CSMOO.Server.Commands"
            );
    }

    /// <summary>
    /// Executes a C# script in the context of a player and command processor
    /// </summary>
    public string ExecuteScript(string code, Player? player, CommandProcessor commandProcessor)
    {
        try
        {
            // Create script globals that provide access to game systems
            var globals = new ScriptGlobals
            {
                Player = player,
                CommandProcessor = commandProcessor,
                ObjectManager = new ScriptObjectManager(),
                WorldManager = new ScriptWorldManager(),
                PlayerManager = new ScriptPlayerManager()
            };

            // Remove curly braces if present (for "script { code }" syntax)
            if (code.StartsWith("{") && code.EndsWith("}"))
            {
                code = code.Substring(1, code.Length - 2).Trim();
            }

            // Execute the script
            var script = CSharpScript.Create(code, _scriptOptions, typeof(ScriptGlobals));
            var result = script.RunAsync(globals).GetAwaiter().GetResult();

            return result.ReturnValue?.ToString() ?? "";
        }
        catch (CompilationErrorException ex)
        {
            return $"Compilation error: {string.Join(", ", ex.Diagnostics)}";
        }
        catch (Exception ex)
        {
            return $"Runtime error: {ex.Message}";
        }
    }
}

/// <summary>
/// Global variables and functions available to scripts
/// </summary>
public class ScriptGlobals
{
    public Player? Player { get; set; }
    public CommandProcessor? CommandProcessor { get; set; }
    public ScriptObjectManager ObjectManager { get; set; } = new ScriptObjectManager();
    public ScriptWorldManager WorldManager { get; set; } = new ScriptWorldManager();
    public ScriptPlayerManager PlayerManager { get; set; } = new ScriptPlayerManager();

    /// <summary>
    /// Send a message to the current player
    /// </summary>
    public void Say(string message)
    {
        CommandProcessor?.SendToPlayer(message);
    }

    /// <summary>
    /// Get a property from an object
    /// </summary>
    public object? GetProperty(string objectId, string propertyName)
    {
        var obj = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (obj == null) return null;
        
        return Database.ObjectManager.GetProperty(obj, propertyName)?.RawValue;
    }

    /// <summary>
    /// Set a property on an object
    /// </summary>
    public void SetProperty(string objectId, string propertyName, object value)
    {
        var obj = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (obj != null)
        {
            Database.ObjectManager.SetProperty(obj, propertyName, new BsonValue(value));
        }
    }

    /// <summary>
    /// Create a new object instance
    /// </summary>
    public string CreateObject(string className, string? location = null)
    {
        var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == className);
        if (objectClass == null) throw new ArgumentException($"Class '{className}' not found");
        
        var newObject = Database.ObjectManager.CreateInstance(objectClass.Id, location);
        return newObject.Id;
    }

    /// <summary>
    /// Find objects by class name
    /// </summary>
    public List<string> FindObjects(string className)
    {
        var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == className);
        if (objectClass == null) return new List<string>();
        
        return [.. Database.ObjectManager.FindObjectsByClass(objectClass.Id).Select(obj => obj.Id)];
    }
}

/// <summary>
/// Script-safe wrapper for ObjectManager
/// </summary>
public class ScriptObjectManager
{
    public object? GetProperty(string objectId, string propertyName)
    {
        var obj = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (obj == null) return null;
        
        return Database.ObjectManager.GetProperty(obj, propertyName)?.RawValue;
    }

    public void SetProperty(string objectId, string propertyName, object value)
    {
        var obj = GameDatabase.Instance.GameObjects.FindById(objectId);
        if (obj != null)
        {
            Database.ObjectManager.SetProperty(obj, propertyName, new BsonValue(value));
        }
    }

    public void MoveObject(string objectId, string? newLocation)
    {
        Database.ObjectManager.MoveObject(objectId, newLocation);
    }

    public List<string> GetObjectsInLocation(string locationId)
    {
        return [.. Database.ObjectManager.GetObjectsInLocation(locationId).Select(obj => obj.Id)];
    }
}

/// <summary>
/// Script-safe wrapper for WorldManager
/// </summary>
public class ScriptWorldManager
{
    public string? GetStartingRoom()
    {
        return Database.WorldManager.GetStartingRoom()?.Id;
    }

    public List<string> GetAllRooms()
    {
        return [.. Database.WorldManager.GetAllRooms().Select(room => room.Id)];
    }

    public void CreateExit(string fromRoomId, string toRoomId, string direction, string returnDirection)
    {
        Database.WorldManager.CreateExit(fromRoomId, toRoomId, direction, returnDirection);
    }

    public string CreateSimpleItem(string name, string shortDesc, string longDesc, string? locationId = null)
    {
        return Database.WorldManager.CreateSimpleItem(name, shortDesc, longDesc, locationId).Id;
    }
}

/// <summary>
/// Script-safe wrapper for PlayerManager
/// </summary>
public class ScriptPlayerManager
{
    public List<string> GetOnlinePlayerNames()
    {
        return [.. PlayerManager.GetOnlinePlayers().Select(p => p.Name)];
    }

    public string? GetPlayerLocation(string playerName)
    {
        var player = GameDatabase.Instance.Players.FindOne(p => p.Name == playerName);
        return player?.Location;
    }
}
