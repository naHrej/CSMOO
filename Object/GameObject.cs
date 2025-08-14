using System.Dynamic;
using LiteDB;
using CSMOO.Functions;
using CSMOO.Database;

namespace CSMOO.Object;

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
    public string Name
    {
        get => Properties.ContainsKey("name") ? Properties["name"].AsString : string.Empty;
        set
        {
            Properties["name"] = value != null ? new BsonValue(value) : BsonValue.Null;


        }
    }

    public List<string> Aliases
    {
        get => Properties.ContainsKey("aliases") && Properties["aliases"].IsArray ?
            Properties["aliases"].AsArray.Select(v => v.AsString).ToList() : new List<string>();
        set => Properties["aliases"] = value != null ? new BsonArray(value.Select(s => new BsonValue(s))) : BsonValue.Null;
    }

    /// <summary>
    /// Numeric database reference (like #1, #2, #3, etc.) for easy user addressing
    /// </summary>
    public int DbRef
    {
        get => Properties.ContainsKey("dbref") ? Properties["dbref"].AsInt32 : 0;
        set => Properties["dbref"] = new BsonValue(value);
    }

    /// <summary>
    /// The class this object is an instance of
    /// </summary>
    public string ClassId
    {
        get => this.GetType().Name;
    }

    /// <summary>
    /// Instance-specific properties that override or extend the class defaults
    /// </summary>
    public BsonDocument Properties { get; set; } = new BsonDocument();

    /// <summary>
    /// Location of this object (ID of the room/container it's in)
    /// Null means it's not in the game world currently
    /// Preferred over using Location property directly
    /// </summary>
    public GameObject? Location
    {
        get
        {
            var loc = Properties.ContainsKey("location") ? Properties["location"].AsString : null;
            return loc != null ? ObjectManager.GetObject(loc) : null;
        }
        set => Properties["location"] = value?.Id != null ? new BsonValue(value.Id) : BsonValue.Null;

    }

    /// <summary>
    /// Objects contained within this object (inventory, room contents, etc.)
    /// </summary>
    public List<string> Contents
    {
        get => Properties.ContainsKey("contents") && Properties["contents"].IsArray ?
            Properties["contents"].AsArray.Select(v => v.AsString).ToList() : new List<string>();
        set => Properties["contents"] = value != null ? new BsonArray(value.Select(s => new BsonValue(s))) : BsonValue.Null;
    }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt
    {
        get => Properties.ContainsKey("createdat") ? Properties["createdat"].AsDateTime : DateTime.UtcNow;
        set => Properties["createdat"] = new BsonValue(value);
    }

    public GameObject? Owner
    {
        get => Properties.ContainsKey("owner") ? ObjectManager.GetObject(Properties["owner"].AsString) : null;
        set => Properties["owner"] = value != null ? new BsonValue(value.Id) : BsonValue.Null;
    }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt
    {
        get => Properties.ContainsKey("modifiedat") ? Properties["modifiedat"].AsDateTime : DateTime.UtcNow;
        set => Properties["modifiedat"] = new BsonValue(value);
    }

    // Dynamic property accessors for common properties to satisfy the C# compiler
    // These forward to the dynamic property system but provide compile-time visibility

    /// <summary>
    /// Short description of the object (forwarded to property system)
    /// </summary>
    public string? shortDescription
    {
        get => Properties.ContainsKey("shortDescription") ? Properties["shortDescription"].AsString : null;
        set => Properties["shortDescription"] = value != null ? new BsonValue(value) : BsonValue.Null;
    }

    /// <summary>
    /// Long description of the object (forwarded to property system)
    /// </summary>
    public string? longDescription
    {
        get => Properties.ContainsKey("longDescription") ? Properties["longDescription"].AsString : null;
        set => Properties["longDescription"] = value != null ? new BsonValue(value) : BsonValue.Null;
    }

    /// <summary>
    /// Description of the object (forwarded to property system)
    /// </summary>
    public string? description
    {
        get => Properties.ContainsKey("description") ? Properties["description"].AsString : null;
        set => Properties["description"] = value != null ? new BsonValue(value) : BsonValue.Null;
    }


    // Additional common properties that scripts might access

    /// <summary>
    /// Whether the object is gettable/takeable (forwarded to property system)
    /// </summary>
    public bool? gettable
    {
        get => Properties.ContainsKey("gettable") ? Properties["gettable"].AsBoolean : (bool?)null;
        set => Properties["gettable"] = value.HasValue ? new BsonValue(value.Value) : BsonValue.Null;
    }

    /// <summary>
    /// Whether the object is visible (forwarded to property system)
    /// </summary>
    public bool? visible
    {
        get => Properties.ContainsKey("visible") ? Properties["visible"].AsBoolean : (bool?)null;
        set => Properties["visible"] = value.HasValue ? new BsonValue(value.Value) : BsonValue.Null;
    }

    /// <summary>
    /// Object size/weight (forwarded to property system)
    /// </summary>
    public int? size
    {
        get => Properties.ContainsKey("size") ? Properties["size"].AsInt32 : (int?)null;
        set => Properties["size"] = value.HasValue ? new BsonValue(value.Value) : BsonValue.Null;
    }

    public List<string> Permissions
    {
        get
        {
            // Defensive: handle null or non-array values gracefully
            if (!Properties.ContainsKey("permissions") || Properties["permissions"].IsNull)
                return new List<string>();
            var arr = Properties["permissions"].AsArray;
            if (arr == null)
                return new List<string>();
            return arr.Select(v => v.AsString).ToList();
        }
        set => Properties["permissions"] = value != null ? new BsonArray(value.Select(s => new BsonValue(s))) : BsonValue.Null;
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
                    result = Properties.ContainsKey("classid") ? Properties["classid"].AsString : string.Empty;
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



            // Try to get the property from the instance's Properties dictionary only
            if (Properties.ContainsKey(propertyName))
            {
                var rawValue = Properties[propertyName].RawValue;
                // Try to resolve as a GameObject if it's a Guid string
                if (rawValue is string strValue && Guid.TryParse(strValue, out var guid))
                {
                    var obj = ObjectManager.GetObject( strValue);
                    if (obj != null)
                    {
                        result = obj as dynamic;
                        return true;
                    }
                    // If not found, return the string value
                    result = strValue;
                    return true;
                }
                result = rawValue;
                return true;
            }
            // Check if this might be a case sensitivity issue with built-in properties
            var suggestion = GetPropertySuggestion(propertyName);
            if (!string.IsNullOrEmpty(suggestion))
            {
                throw new InvalidOperationException($"Property '{propertyName}' not found on object {Id}. Did you mean '{suggestion}'? (Property names are case-sensitive)");
            }
            result = null;
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
                    throw new InvalidOperationException($"Property '{propertyName}' is read-only");
                case "aliases":
                    if (value is List<string> aliasesList)
                    {
                        Aliases = aliasesList;
                        DbProvider.Instance.Update("gameobjects", this);
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
                    DbProvider.Instance.Update("gameobjects", this);
                    return true;
                case "owner":
                    Owner = value as GameObject ?? null!;
                    DbProvider.Instance.Update("gameobjects", this);
                    return true;
            }

            // If value is a GameObject, store its Id instead
            BsonValue bsonValue = value switch
            {
                null => BsonValue.Null,
                GameObject go => new BsonValue(go.Id),
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
            Properties[propertyName] = bsonValue;
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
            var function = FunctionResolver.FindFunction(Id, methodName);
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
                    currentPlayer = ObjectManager.GetObject<Player>(playerGameObj.Id);
                }

                commandProcessor = Scripting.Builtins.UnifiedContext.CommandProcessor;
            }

            if (currentPlayer == null)
            {
                throw new InvalidOperationException($"Cannot execute function '{methodName}': no current player context available");
            }

            // Execute the function using the UnifiedScriptEngine
            var engine = new Scripting.ScriptEngine();
            result = engine.ExecuteFunction(function, args ?? new object[0],
                currentPlayer, commandProcessor, Id);
            return true;
        }
        catch
        {
            // Just re-throw the original exception to avoid verbose message wrapping
            throw;
        }
    }

    /// <summary>
    /// String representation
    /// </summary>
    public override string ToString()
    {
        var name = Properties.ContainsKey("name") ? Properties["name"].AsString : Name;
        var shortDesc = Properties.ContainsKey("shortDescription") ? Properties["shortDescription"].AsString : null;
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




