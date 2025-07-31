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
using CSMOO.Database;
using CSMOO.Commands;
using CSMOO.Verbs;
using CSMOO.Functions;

namespace CSMOO.Scripting;

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
    public ScriptHelpers? Helpers { get; set; }

    /// <summary>
    /// Send a message to the current player
    /// </summary>
    public void Say(string message)
    {
        if (Helpers != null)
        {
            Helpers.Say(message);
        }
        else
        {
            CommandProcessor?.SendToPlayer(message);
        }
    }

    /// <summary>
    /// Send a message to a specific player
    /// </summary>
    public void notify(Player targetPlayer, string message)
    {
        if (Helpers != null)
        {
            Helpers.notify(targetPlayer, message);
        }
        else
        {
            CommandProcessor?.SendToPlayer(message, targetPlayer.SessionGuid);
        }
    }

    /// <summary>
    /// Send a message to all players in the current room
    /// </summary>
    public void SayToRoom(string message, bool excludeSelf = true)
    {
        Helpers?.SayToRoom(message, excludeSelf);
    }

    /// <summary>
    /// Get a property from an object
    /// </summary>
    public object? GetProperty(string objectId, string propertyName)
    {
        return ObjectManager.GetProperty(objectId, propertyName);
    }

    /// <summary>
    /// Set a property on an object
    /// </summary>
    public void SetProperty(string objectId, string propertyName, object value)
    {
        ObjectManager.SetProperty(objectId, propertyName, value);
    }

    /// <summary>
    /// Move an object to a new location
    /// </summary>
    public void MoveObject(string objectId, string? newLocation)
    {
        ObjectManager.MoveObject(objectId, newLocation);
    }

    /// <summary>
    /// Get all objects in a location
    /// </summary>
    public List<string> GetObjectsInLocation(string locationId)
    {
        return ObjectManager.GetObjectsInLocation(locationId);
    }

    /// <summary>
    /// Find objects by name in the current location or inventory
    /// </summary>
    public List<string> FindObjects(string name)
    {
        if (Player?.Location == null) return new List<string>();

        var candidates = GetObjectsInLocation(Player.Location.Id)
            .Concat(GetObjectsInLocation(Player.Id))
            .ToList();

        return candidates.Where(objId =>
        {
            var obj = Database.ObjectManager.GetObject(objId);
            return obj != null && (
                obj.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) == true ||
                (obj.Properties.ContainsKey("aliases") &&
                 obj.Properties["aliases"].AsString?.Contains(name, StringComparison.OrdinalIgnoreCase) == true)
            );
        }).ToList();
    }

    /// <summary>
    /// Call a verb on an object
    /// </summary>
    public object? CallVerb(string objectId, string verbName, params object[] args)
    {
        if (Player == null)
            throw new InvalidOperationException("No player context available for verb calls.");

        var verb = VerbResolver.FindMatchingVerb(objectId, new[] { verbName });
        if (verb == null)
        {
            throw new ArgumentException($"Verb '{verbName}' not found on object '{objectId}'.");
        }

        var engine = new UnifiedScriptEngine();
        return engine.ExecuteVerb(verb, $"{verbName} {string.Join(" ", args)}", Player as Database.Player ?? throw new InvalidOperationException("Invalid player type"), CommandProcessor!, objectId);
    }

    /// <summary>
    /// Check if an object has a verb
    /// </summary>
    public bool HasVerb(string objectId, string verbName)
    {
        return VerbResolver.HasVerb(objectId, verbName);
    }

    /// <summary>
    /// Call a function (global or object-specific)
    /// </summary>
    public object? CallFunction(string functionName, params object[] parameters)
    {
        if (Player == null)
            throw new InvalidOperationException("No player context available for function calls.");

        var systemObjectId = Helpers?.GetSystemObjectId() ?? "system";
        return CallFunction(systemObjectId, functionName, parameters);
    }

    /// <summary>
    /// Call a function on a specific object
    /// </summary>
    public object? CallFunction(string objectId, string functionName, params object[] parameters)
    {
        if (Player == null)
            throw new InvalidOperationException("No player context available for function calls.");

        var function = FunctionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            throw new ArgumentException($"Function '{functionName}' not found on object '{objectId}'.");
        }

        var engine = new UnifiedScriptEngine();
        return engine.ExecuteFunction(function, parameters, Player as Database.Player ?? throw new InvalidOperationException("Invalid player type"), CommandProcessor!, objectId);
    }

    /// <summary>
    /// Call a function on a class
    /// </summary>
    public object? CallClassFunction(string className, string functionName, params object[] parameters)
    {
        return CallFunction($"class:{className}", functionName, parameters);
    }
}



