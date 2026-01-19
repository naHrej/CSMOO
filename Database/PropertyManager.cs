using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Configuration;

namespace CSMOO.Database;

/// <summary>
/// Static wrapper for PropertyManager (backward compatibility)
/// Delegates to PropertyManagerInstance for dependency injection support
/// </summary>
public static class PropertyManager
{
    private static IPropertyManager? _instance;
    
    /// <summary>
    /// Sets the property manager instance for static methods to delegate to
    /// </summary>
    public static void SetInstance(IPropertyManager instance)
    {
        _instance = instance;
    }
    
    private static IPropertyManager Instance => _instance ?? throw new InvalidOperationException("PropertyManager instance not set. Call PropertyManager.SetInstance() first. Static access is no longer supported - use dependency injection.");
    /// <summary>
    /// Gets a property value from an object, checking inheritance chain if not found on instance
    /// </summary>
    public static BsonValue? GetProperty(GameObject gameObject, string propertyName)
    {
        return Instance.GetProperty(gameObject, propertyName);
    }

    /// <summary>
    /// Sets a property value on an object instance
    /// </summary>
    public static void SetProperty(GameObject gameObject, string propertyName, BsonValue value)
    {
        Instance.SetProperty(gameObject, propertyName, value);
    }

    /// <summary>
    /// Sets a property value by object ID
    /// </summary>
    public static bool SetProperty(string objectId, string propertyName, BsonValue value)
    {
        return Instance.SetProperty(objectId, propertyName, value);
    }

    /// <summary>
    /// Removes a property from an object instance
    /// </summary>
    public static bool RemoveProperty(GameObject gameObject, string propertyName)
    {
        return Instance.RemoveProperty(gameObject, propertyName);
    }

    /// <summary>
    /// Removes a property by object ID
    /// </summary>
    public static bool RemoveProperty(string objectId, string propertyName)
    {
        return Instance.RemoveProperty(objectId, propertyName);
    }

    /// <summary>
    /// Checks if an object has a property (including inherited properties)
    /// </summary>
    public static bool HasProperty(GameObject gameObject, string propertyName)
    {
        return Instance.HasProperty(gameObject, propertyName);
    }

    /// <summary>
    /// Gets all property names from an object (including inherited properties)
    /// </summary>
    public static string[] GetAllPropertyNames(GameObject gameObject)
    {
        return Instance.GetAllPropertyNames(gameObject);
    }

    /// <summary>
    /// Merges properties from source into target, with source taking priority
    /// </summary>
    public static void MergeProperties(BsonDocument target, BsonDocument source)
    {
        Instance.MergeProperties(target, source);
    }

    /// <summary>
    /// Copies all properties from one object to another
    /// </summary>
    public static void CopyProperties(GameObject source, GameObject target, bool overwriteExisting = true)
    {
        Instance.CopyProperties(source, target, overwriteExisting);
    }

    /// <summary>
    /// Gets the effective value of a property (resolved through inheritance)
    /// </summary>
    public static T? GetPropertyValue<T>(GameObject gameObject, string propertyName, T? defaultValue = default)
    {
        return Instance.GetPropertyValue(gameObject, propertyName, defaultValue);
    }

    /// <summary>
    /// Sets a strongly-typed property value
    /// </summary>
    public static void SetPropertyValue<T>(GameObject gameObject, string propertyName, T value)
    {
        Instance.SetPropertyValue(gameObject, propertyName, value);
    }
}



