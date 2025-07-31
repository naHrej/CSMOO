using System;
using CSMOO.Database;
using CSMOO.Commands;

namespace CSMOO.Scripting;

/// <summary>
/// Factory for creating ScriptObject instances with natural syntax support
/// </summary>
public class ScriptObjectFactory
{
    private readonly Player _currentPlayer;
    private readonly CommandProcessor _commandProcessor;
    private readonly ScriptHelpers _helpers;

    public ScriptObjectFactory(Player currentPlayer, CommandProcessor commandProcessor, ScriptHelpers helpers)
    {
        _currentPlayer = currentPlayer;
        _commandProcessor = commandProcessor;
        _helpers = helpers;
    }

    /// <summary>
    /// Create a ScriptObject for the given object reference
    /// Supports: "me", "here", "system", "#123", object names, etc.
    /// </summary>
    public dynamic? GetObject(string objectReference)
    {
        var objectId = _helpers.ResolveObject(objectReference);
        if (objectId == null) return null;
        
        return new ScriptObject(objectId, _currentPlayer, _commandProcessor, _helpers);
    }

    /// <summary>
    /// Create a ScriptObject for a direct object ID
    /// </summary>
    public dynamic? GetObjectById(string objectId)
    {
        var obj = ObjectManager.GetObject(objectId);
        if (obj == null) return null;
        return new ScriptObject(objectId, _currentPlayer, _commandProcessor, _helpers);
    }
}



