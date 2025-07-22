using System;
using System.Collections.Generic;
using System.Dynamic;
using LiteDB;

namespace CSMOO.Server.Database;

/// <summary>
/// Base class definition that all game objects inherit from
/// This is the "template" or "prototype" that defines behavior and default properties
/// </summary>
public class ObjectClass
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name of this class (e.g., "Room", "Player", "Sword", "Door")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Parent class this inherits from (null for root classes)
    /// </summary>
    public string? ParentClassId { get; set; }
    
    /// <summary>
    /// Default properties that instances of this class will have
    /// These can be overridden in individual instances
    /// </summary>
    public BsonDocument Properties { get; set; } = new BsonDocument();
    
    /// <summary>
    /// Methods/functions defined on this class (stored as code strings)
    /// Could be C# code, script code, etc.
    /// </summary>
    public BsonDocument Methods { get; set; } = new BsonDocument();
    
    /// <summary>
    /// Description of what this class represents
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this class can be instantiated directly
    /// (abstract classes cannot be instantiated)
    /// </summary>
    public bool IsAbstract { get; set; } = false;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An actual instance of an ObjectClass - this is what exists in the game world
/// Now inherits from DynamicObject to provide natural property and method access
/// </summary>
public class GameObject : DynamicObject
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Objects name (human-readable identifier)
    /// Preferred over using name form properties collection
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public List<string> Aliases { get; set; } = new List<string>();

    
    /// <summary>
    /// Numeric database reference (like #1, #2, #3, etc.) for easy user addressing
    /// </summary>
    public int DbRef { get; set; } = 0;
    
    /// <summary>
    /// The class this object is an instance of
    /// </summary>
    public string ClassId { get; set; } = string.Empty;
    
    /// <summary>
    /// Instance-specific properties that override or extend the class defaults
    /// </summary>
    public BsonDocument Properties { get; set; } = new BsonDocument();
    
    /// <summary>
    /// Location of this object (ID of the room/container it's in)
    /// Null means it's not in the game world currently
    /// Preferred over using Location property directly
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// Objects contained within this object (inventory, room contents, etc.)
    /// </summary>
    public List<string> Contents { get; set; } = new List<string>();
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public GameObject Owner { get; set; } = null!; // Owner of this object, if applicable
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    // Dynamic property accessors for common properties to satisfy the C# compiler
    // These forward to the dynamic property system but provide compile-time visibility
    
    /// <summary>
    /// Short description of the object (forwarded to property system)
    /// </summary>
    public string? shortDescription
    {
        get => ObjectManager.GetProperty(this, "shortDescription")?.AsString;
        set => ObjectManager.SetProperty(this, "shortDescription", value != null ? new BsonValue(value) : BsonValue.Null);
    }
    
    /// <summary>
    /// Long description of the object (forwarded to property system)
    /// </summary>
    public string? longDescription
    {
        get => ObjectManager.GetProperty(this, "longDescription")?.AsString;
        set => ObjectManager.SetProperty(this, "longDescription", value != null ? new BsonValue(value) : BsonValue.Null);
    }
    
    /// <summary>
    /// Description of the object (forwarded to property system)
    /// </summary>
    public string? description
    {
        get => ObjectManager.GetProperty(this, "description")?.AsString;
        set => ObjectManager.SetProperty(this, "description", value != null ? new BsonValue(value) : BsonValue.Null);
    }
    
    /// <summary>
    /// Dynamic name property (forwarded to Name field but allows lowercase access)
    /// </summary>
    public string? name
    {
        get => !string.IsNullOrEmpty(Name) ? Name : ObjectManager.GetProperty(this, "name")?.AsString;
        set 
        {
            if (value != null)
            {
                Name = value;
                ObjectManager.SetProperty(this, "name", new BsonValue(value));
            }
            else
            {
                Name = "";
                ObjectManager.SetProperty(this, "name", BsonValue.Null);
            }
        }
    }
    
    // Additional common properties that scripts might access
    
    /// <summary>
    /// Whether the object is gettable/takeable (forwarded to property system)
    /// </summary>
    public bool? gettable
    {
        get => ObjectManager.GetProperty(this, "gettable")?.AsBoolean;
        set => ObjectManager.SetProperty(this, "gettable", value.HasValue ? new BsonValue(value.Value) : BsonValue.Null);
    }
    
    /// <summary>
    /// Whether the object is visible (forwarded to property system)
    /// </summary>
    public bool? visible
    {
        get => ObjectManager.GetProperty(this, "visible")?.AsBoolean;
        set => ObjectManager.SetProperty(this, "visible", value.HasValue ? new BsonValue(value.Value) : BsonValue.Null);
    }
    
    /// <summary>
    /// Object size/weight (forwarded to property system)
    /// </summary>
    public int? size
    {
        get => ObjectManager.GetProperty(this, "size")?.AsInt32;
        set => ObjectManager.SetProperty(this, "size", value.HasValue ? new BsonValue(value.Value) : BsonValue.Null);
    }

    /// <summary>
    /// Handles property getting: gameObject.propertyName
    /// </summary>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var propertyName = binder.Name;
        
        try
        {
            // Handle special GameObject properties first
            switch (propertyName.ToLower())
            {
                case "id":
                    result = Id;
                    return true;
                case "name":
                    result = Name;
                    return true;
                case "aliases":
                    result = Aliases;
                    return true;
                case "dbref":
                    result = DbRef;
                    return true;
                case "classid":
                    result = ClassId;
                    return true;
                case "location":
                    result = Location;
                    return true;
                case "contents":
                    result = Contents;
                    return true;
                case "createdat":
                    result = CreatedAt;
                    return true;
                case "modifiedat":
                    result = ModifiedAt;
                    return true;
                case "owner":
                    result = Owner;
                    return true;
            }

            // Check if this is a null object (missing from database)
            if (Properties.ContainsKey("_isNullObject") && 
                Properties["_isNullObject"].AsBoolean)
            {
                result = null;
                return true; // Return null for any property on a missing object
            }

            // Try to get the property from the object's property system
            var propertyValue = ObjectManager.GetProperty(this, propertyName);
            
            if (propertyValue == null)
            {
                // Check if this might be a case sensitivity issue with built-in properties
                var suggestion = GetPropertySuggestion(propertyName);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    throw new InvalidOperationException($"Property '{propertyName}' not found on object {Id}. Did you mean '{suggestion}'? (Property names are case-sensitive)");
                }
                
                result = null;
                return true; // Return true but with null result - property doesn't exist
            }
            
            // Convert BsonValue to appropriate C# type
            result = propertyValue.RawValue;
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error accessing property '{propertyName}' on object {Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles property setting: gameObject.propertyName = "value"
    /// </summary>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        var propertyName = binder.Name;
        
        try
        {
            // Check if this is a null object (missing from database)
            if (Properties.ContainsKey("_isNullObject") && 
                Properties["_isNullObject"].AsBoolean)
            {
                throw new InvalidOperationException($"Cannot set property '{propertyName}' on missing object {Id}");
            }

            // Prevent setting read-only GameObject properties
            switch (propertyName.ToLower())
            {
                case "id":
                case "dbref":
                case "classid":
                case "createdat":
                    throw new InvalidOperationException($"Property '{propertyName}' is read-only");
                case "name":
                    Name = value?.ToString() ?? "";
                    GameDatabase.Instance.GameObjects.Update(this);
                    return true;
                case "aliases":
                    if (value is List<string> aliasesList)
                    {
                        Aliases = aliasesList;
                        GameDatabase.Instance.GameObjects.Update(this);
                    }
                    return true;
                case "location":
                    // Special handling for location changes
                    ObjectManager.MoveObject(Id, value?.ToString());
                    return true;
                case "contents":
                    throw new InvalidOperationException("Contents property cannot be set directly. Use object movement commands instead.");
                case "modifiedat":
                    ModifiedAt = value is DateTime dt ? dt : DateTime.UtcNow;
                    GameDatabase.Instance.GameObjects.Update(this);
                    return true;
                case "owner":
                    Owner = value as GameObject ?? null!;
                    GameDatabase.Instance.GameObjects.Update(this);
                    return true;
            }

            // Convert value to BsonValue for property storage
            BsonValue bsonValue = value switch
            {
                null => BsonValue.Null,
                string s => new BsonValue(s),
                int i => new BsonValue(i),
                long l => new BsonValue(l),
                double d => new BsonValue(d),
                float f => new BsonValue((double)f),
                bool b => new BsonValue(b),
                DateTime dt => new BsonValue(dt),
                BsonValue bv => bv,
                _ => new BsonValue(value.ToString() ?? "")
            };

            ObjectManager.SetProperty(this, propertyName, bsonValue);
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error setting property '{propertyName}' on object {Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles method calls (functions): gameObject.functionName(args)
    /// </summary>
    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        var methodName = binder.Name;
        
        try
        {
            // Check if this is a null object (missing from database)
            if (Properties.ContainsKey("_isNullObject") && 
                Properties["_isNullObject"].AsBoolean)
            {
                throw new InvalidOperationException($"Cannot call method '{methodName}' on missing object {Id}");
            }

            // Find the function on this object using the FunctionResolver
            var function = Scripting.FunctionResolver.FindFunction(Id, methodName);
            if (function == null)
            {
                // Check if this might be a case sensitivity issue
                var suggestion = GetMethodSuggestion(methodName);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    throw new ArgumentException($"Function '{methodName}' not found on object {Id}. Did you mean '{suggestion}'? (Function names are case-sensitive)");
                }
                
                throw new ArgumentException($"Function '{methodName}' not found on object {Id}. Check function name and ensure it's defined on this object or its class.");
            }

            // Get the current player from the UnifiedContext if available
            Player? currentPlayer = null;
            Commands.CommandProcessor? commandProcessor = null;
            
            if (Scripting.Builtins.UnifiedContext != null)
            {
                // Try to get Database.Player from the dynamic Player object
                var playerObj = Scripting.Builtins.UnifiedContext.Player;
                if (playerObj is GameObject playerGameObj)
                {
                    currentPlayer = GameDatabase.Instance.Players.FindById(playerGameObj.Id);
                }
                
                commandProcessor = Scripting.Builtins.UnifiedContext.CommandProcessor;
            }

            if (currentPlayer == null)
            {
                throw new InvalidOperationException($"Cannot execute function '{methodName}': no current player context available");
            }

            // Execute the function using the UnifiedScriptEngine
            var engine = new Scripting.UnifiedScriptEngine();
            result = engine.ExecuteFunction(function, args ?? new object[0], 
                currentPlayer, commandProcessor, Id);
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error calling function '{methodName}' on object {Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// String representation
    /// </summary>
    public override string ToString()
    {
        var nameProperty = ObjectManager.GetProperty(this, "name");
        var shortDescProperty = ObjectManager.GetProperty(this, "shortDescription");
        
        var name = nameProperty?.AsString ?? Name;
        var shortDesc = shortDescProperty?.AsString;
        
        return name ?? shortDesc ?? $"Object #{DbRef}";
    }

    /// <summary>
    /// Helper method to suggest correct property names for case sensitivity issues
    /// </summary>
    private string? GetPropertySuggestion(string propertyName)
    {
        var lowerProperty = propertyName.ToLower();
        
        // Check against built-in GameObject properties
        return lowerProperty switch
        {
            "id" => "Id",
            "name" => "Name", 
            "aliases" => "Aliases",
            "dbref" => "DbRef",
            "classid" => "ClassId",
            "location" => "Location",
            "contents" => "Contents",
            "createdat" => "CreatedAt",
            "modifiedat" => "ModifiedAt",
            "owner" => "Owner",
            "shortdescription" => "shortDescription",
            "longdescription" => "longDescription",
            "description" => "description",
            "gettable" => "gettable",
            "visible" => "visible",
            "size" => "size",
            _ => null
        };
    }

    /// <summary>
    /// Helper method to suggest correct method names for case sensitivity issues
    /// </summary>
    private string? GetMethodSuggestion(string methodName)
    {
        var lowerMethod = methodName.ToLower();
        
        // Check against common method names that might have case issues
        return lowerMethod switch
        {
            "styleddesc" => "StyledDesc",
            "getdescription" => "GetDescription",
            "getname" => "GetName",
            "getlocation" => "GetLocation",
            _ => null
        };
    }

    /// <summary>
    /// Static helper method to provide better error messages for common collection iteration mistakes
    /// </summary>
    public static string GetCollectionIterationHelp()
    {
        return @"
Common iteration mistakes:
1. If iterating over Contents: use GetObjectsInLocation(objectId) instead of obj.Contents
2. If iterating over Properties: use ObjectManager.GetProperty() for individual properties
3. If iterating over BsonDocument: KeyValuePair objects don't have .Name - access .Key and .Value instead
4. If expecting GameObjects: ensure you're using methods that return GameObject collections
";
    }
}

