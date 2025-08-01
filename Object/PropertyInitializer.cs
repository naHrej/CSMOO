using LiteDB;
using CSMOO.Logging;
using CSMOO.Database;

namespace CSMOO.Object;

/// <summary>
/// Statistics for property loading operations
/// </summary>
public struct PropertyLoadStats
{
    public int Loaded { get; set; }
    public int Skipped { get; set; }
}

/// <summary>
/// Handles loading and initializing object properties from JSON definitions
/// </summary>
public static class PropertyInitializer
{
    private static readonly string PropertiesPath = GetResourcePath("properties");
    private static readonly string SystemPropertiesPath = Path.Combine(PropertiesPath, "system");
    private static readonly string ClassPropertiesPath = Path.Combine(PropertiesPath, "classes");
    private static readonly string InstancePropertiesPath = Path.Combine(PropertiesPath, "instances");

    /// <summary>
    /// Gets the absolute path to a resource directory, with fallback for development scenarios
    /// </summary>
    private static string GetResourcePath(string resourceName)
    {
        var possiblePaths = new List<string>();
        var workingDirectory = Directory.GetCurrentDirectory();

        // Strategy: Current working directory with explicit path
        possiblePaths.Add(Path.Combine(workingDirectory, "resources", resourceName));
        Logger.Debug($"Trying resource path: {possiblePaths.Last()}");
        
        // If none exist, return the first option (app directory based) for error reporting
        return possiblePaths[0];
    }

    /// <summary>
    /// Loads and sets all properties from JSON definitions
    /// </summary>
    public static void LoadAndSetProperties()
    {
        Logger.Info("Loading property definitions from JSON files...");

        var systemStats = LoadSystemProperties();
        var classStats = LoadClassProperties();
        var instanceStats = LoadInstanceProperties();

        var totalLoaded = systemStats.Loaded + classStats.Loaded + instanceStats.Loaded;
        var totalSkipped = systemStats.Skipped + classStats.Skipped + instanceStats.Skipped;

        Logger.Info($"Property definitions loaded successfully - Set: {totalLoaded}, Skipped: {totalSkipped}");
    }

    /// <summary>
    /// Hot reload all property definitions
    /// </summary>
    public static void ReloadProperties()
    {
        Logger.Info("Hot reloading property definitions...");

        // For properties, we don't clear existing ones as they might be modified by players
        // Instead, we only update properties that have overwrite=true in their definition
        
        var systemStats = LoadSystemProperties();
        var classStats = LoadClassProperties();
        var instanceStats = LoadInstanceProperties();

        var totalLoaded = systemStats.Loaded + classStats.Loaded + instanceStats.Loaded;
        var totalSkipped = systemStats.Skipped + classStats.Skipped + instanceStats.Skipped;

        Logger.Info($"Property definitions reloaded successfully - Set: {totalLoaded}, Skipped: {totalSkipped}");
    }

    /// <summary>
    /// Load system object properties
    /// </summary>
    private static PropertyLoadStats LoadSystemProperties()
    {
        var stats = new PropertyLoadStats();
        
        if (!Directory.Exists(SystemPropertiesPath))
        {
            Logger.Debug($"System properties directory not found: {SystemPropertiesPath}");
            return stats;
        }

        var systemObjectId = GetOrCreateSystemObject();
        if (systemObjectId == null)
        {
            Logger.Error("Failed to get system object for system properties");
            return stats;
        }

        var systemObject = ObjectManager.GetObject(systemObjectId);
        if (systemObject == null)
        {
            Logger.Error("Failed to retrieve system object for properties");
            return stats;
        }

        var jsonFiles = Directory.GetFiles(SystemPropertiesPath, "*.json");
        Logger.Debug($"Found {jsonFiles.Length} system property definition files");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var propertyDefs = System.Text.Json.JsonSerializer.Deserialize<PropertyDefinition[]>(json);

                if (propertyDefs == null)
                {
                    Logger.Warning($"Invalid property definitions in {file}");
                    continue;
                }

                foreach (var propDef in propertyDefs)
                {
                    if (string.IsNullOrEmpty(propDef.Name))
                    {
                        Logger.Warning($"Property definition missing name in {file}");
                        continue;
                    }

                    var baseDirectory = Path.GetDirectoryName(file);
                    var propStats = SetSystemProperty(systemObject, propDef, baseDirectory!);
                    stats.Loaded += propStats.Loaded;
                    stats.Skipped += propStats.Skipped;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading property definitions from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Load class-based property definitions
    /// </summary>
    private static PropertyLoadStats LoadClassProperties()
    {
        var stats = new PropertyLoadStats();
        
        if (!Directory.Exists(ClassPropertiesPath))
        {
            Logger.Debug($"Class properties directory not found: {ClassPropertiesPath}");
            return stats;
        }

        var jsonFiles = Directory.GetFiles(ClassPropertiesPath, "*.json");
        Logger.Debug($"Found {jsonFiles.Length} class property definition files");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var propertyDefs = System.Text.Json.JsonSerializer.Deserialize<PropertyDefinition[]>(json);

                if (propertyDefs == null)
                {
                    Logger.Warning($"Invalid property definitions in {file}");
                    continue;
                }

                foreach (var propDef in propertyDefs)
                {
                    if (string.IsNullOrEmpty(propDef.Name) || string.IsNullOrEmpty(propDef.TargetClass))
                    {
                        Logger.Warning($"Property definition missing name or targetClass in {file}");
                        continue;
                    }

                    var baseDirectory = Path.GetDirectoryName(file);
                    var propStats = SetClassProperty(propDef, baseDirectory!);
                    stats.Loaded += propStats.Loaded;
                    stats.Skipped += propStats.Skipped;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading property definitions from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Load instance-based property definitions
    /// </summary>
    private static PropertyLoadStats LoadInstanceProperties()
    {
        var stats = new PropertyLoadStats();
        
        if (!Directory.Exists(InstancePropertiesPath))
        {
            Logger.Debug($"Instance properties directory not found: {InstancePropertiesPath}");
            return stats;
        }

        var jsonFiles = Directory.GetFiles(InstancePropertiesPath, "*.json");
        Logger.Debug($"Found {jsonFiles.Length} instance property definition files");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var propertyDefs = System.Text.Json.JsonSerializer.Deserialize<PropertyDefinition[]>(json);

                if (propertyDefs == null)
                {
                    Logger.Warning($"Invalid property definitions in {file}");
                    continue;
                }

                foreach (var propDef in propertyDefs)
                {
                    if (string.IsNullOrEmpty(propDef.Name) || string.IsNullOrEmpty(propDef.TargetObject))
                    {
                        Logger.Warning($"Property definition missing name or targetObject in {file}");
                        continue;
                    }

                    var baseDirectory = Path.GetDirectoryName(file);
                    var propStats = SetInstanceProperty(propDef, baseDirectory!);
                    stats.Loaded += propStats.Loaded;
                    stats.Skipped += propStats.Skipped;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading property definitions from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Set a property on the system object
    /// </summary>
    private static PropertyLoadStats SetSystemProperty(GameObject systemObject, PropertyDefinition propDef, string baseDirectory)
    {
        var stats = new PropertyLoadStats();
        
        // Never delete properties, only add or overwrite if explicitly requested
        if (systemObject.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
            Logger.Debug($"System property '{propDef.Name}' already exists, skipping (overwrite=false)");
            stats.Skipped = 1;
            return stats;
        }

        try
        {
            var value = propDef.GetTypedValue(baseDirectory);
            if (value is string[] lines)
            {
                // Handle multiline properties as BsonArray
                var bsonArray = new BsonArray(lines.Select(l => new BsonValue(l)));
                ObjectManager.SetProperty(systemObject, propDef.Name, bsonArray);
            }
            else
            {
                ObjectManager.SetProperty(systemObject, propDef.Name, new BsonValue(value));
            }
            
            Logger.Debug($"Set system property '{propDef.Name}' = {value}");
            stats.Loaded = 1;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting system property '{propDef.Name}': {ex.Message}");
        }
        
        return stats;
    }

    /// <summary>
    /// Set a property on a class (affects all instances of that class)
    /// </summary>
    private static PropertyLoadStats SetClassProperty(PropertyDefinition propDef, string baseDirectory)
    {
        var stats = new PropertyLoadStats();
        
        var targetClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == propDef.TargetClass);
        if (targetClass == null)
        {
            Logger.Warning($"Target class '{propDef.TargetClass}' not found for property '{propDef.Name}'");
            return stats;
        }

        // Never delete properties, only add or overwrite if explicitly requested
        if (targetClass.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
            Logger.Debug($"Class property '{propDef.Name}' on {propDef.TargetClass} already exists, skipping (overwrite=false)");
            stats.Skipped = 1;
            return stats;
        }

        try
        {
            var value = propDef.GetTypedValue(baseDirectory);
            if (value is string[] lines)
            {
                // Handle multiline properties as BsonArray
                targetClass.Properties[propDef.Name] = new BsonArray(lines.Select(l => new BsonValue(l)));
            }
            else
            {
                targetClass.Properties[propDef.Name] = new BsonValue(value);
            }
            
            DbProvider.Instance.Update("objectclasses", targetClass);
            Logger.Debug($"Set class property '{propDef.Name}' on {propDef.TargetClass} = {value}");
            stats.Loaded = 1;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting class property '{propDef.Name}' on {propDef.TargetClass}: {ex.Message}");
        }
        
        return stats;
    }

    /// <summary>
    /// Set a property on a specific object instance
    /// </summary>
    private static PropertyLoadStats SetInstanceProperty(PropertyDefinition propDef, string baseDirectory)
    {
        var stats = new PropertyLoadStats();
        
        // Resolve the target object
        var targetObject = ResolveObject(propDef.TargetObject!);
        if (targetObject == null)
        {
            Logger.Warning($"Target object '{propDef.TargetObject}' not found for property '{propDef.Name}'");
            return stats;
        }

        // Never delete properties, only add or overwrite if explicitly requested
        if (targetObject.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
            Logger.Debug($"Instance property '{propDef.Name}' on {propDef.TargetObject} already exists, skipping (overwrite=false)");
            stats.Skipped = 1;
            return stats;
        }

        try
        {
            var value = propDef.GetTypedValue(baseDirectory);
            if (value is string[] lines)
            {
                // Handle multiline properties as BsonArray
                var bsonArray = new BsonArray(lines.Select(l => new BsonValue(l)));
                ObjectManager.SetProperty(targetObject, propDef.Name, bsonArray);
            }
            else
            {
                ObjectManager.SetProperty(targetObject, propDef.Name, new BsonValue(value));
            }
            
            Logger.Debug($"Set instance property '{propDef.Name}' on {propDef.TargetObject} = {value}");
            stats.Loaded = 1;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting instance property '{propDef.Name}' on {propDef.TargetObject}: {ex.Message}");
        }
        
        return stats;
    }

    /// <summary>
    /// Resolve object names to GameObject instances
    /// </summary>
    private static GameObject? ResolveObject(string objectName)
    {
        // Check for special keywords
        switch (objectName.ToLower())
        {
            case "system":
                var systemId = GetOrCreateSystemObject();
                return systemId != null ? ObjectManager.GetObject(systemId) : null;
        }

        // Check if it's a DBREF (starts with # followed by digits)
        if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
        {
            return DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.DbRef == dbref);
        }

        // Try to find by name
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        return allObjects.FirstOrDefault(obj =>
        {
            var name = ObjectManager.GetProperty(obj, "name")?.AsString;
            return name != null && name.Equals(objectName, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Gets or creates the system object for holding global properties
    /// </summary>
    private static string? GetOrCreateSystemObject()
    {
        // Get all objects and filter in memory (LiteDB doesn't support ContainsKey in expressions)
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects").ToList();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        
        if (systemObj == null)
        {
            // System object doesn't exist, create it
            Logger.Debug("System object not found, creating it...");
            // Use Container class instead of abstract Object class
            var containerClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Container");
            if (containerClass != null)
            {
                systemObj = ObjectManager.CreateInstance(containerClass.Id);
                ObjectManager.SetProperty(systemObj, "name", "System");
                ObjectManager.SetProperty(systemObj, "shortDescription", "the system object");
                ObjectManager.SetProperty(systemObj, "longDescription", "This is the system object that holds global verbs and functions.");
                ObjectManager.SetProperty(systemObj, "isSystemObject", true);
                ObjectManager.SetProperty(systemObj, "gettable", false); // Don't allow players to pick up the system
                Logger.Debug($"Created system object with ID: {systemObj.Id}");
            }
            else
            {
                Logger.Error("Could not find Container class to create system object!");
                return null;
            }
        }
        
        Logger.Debug($"System object ID: {systemObj?.Id}");
        return systemObj?.Id;
    }
}
