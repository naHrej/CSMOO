using LiteDB;
using CSMOO.Logging;
using CSMOO.Database;
using CSMOO.Parsing;

namespace CSMOO.Object;

/// <summary>
/// Handles loading and initializing object properties from JSON definitions and C# class definitions
/// </summary>
public static class PropertyInitializer
{
    private static readonly string ResourcesPath = GetResourcePath();

    /// <summary>
    /// Gets the absolute path to the resources directory
    /// </summary>
    private static string GetResourcePath()
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        var resourcesPath = Path.Combine(workingDirectory, "Resources");
        Logger.Debug($"Resources path: {resourcesPath}");
        return resourcesPath;
    }

    /// <summary>
    /// Loads and sets all properties from C# class definitions
    /// </summary>
    public static void LoadAndSetProperties()
    {
        Logger.Info("Loading property definitions from C# files...");

        var stats = LoadProperties();

        Logger.Info($"Property definitions loaded successfully - Set: {stats.Loaded}, Skipped: {stats.Skipped}");
    }

    /// <summary>
    /// Hot reload all property definitions
    /// </summary>
    public static void ReloadProperties()
    {
        Logger.Info("Hot reloading property definitions...");

        // For properties, we don't clear existing ones as they might be modified by players
        // Instead, we only update properties that have overwrite=true in their definition
        
        var stats = LoadProperties();

        Logger.Info($"Property definitions reloaded successfully - Set: {stats.Loaded}, Skipped: {stats.Skipped}");
    }

    /// <summary>
    /// Loads and creates properties from all C# class definitions in Resources directory
    /// </summary>
    public static (int Loaded, int Skipped) LoadProperties()
    {
        int loaded = 0;
        int skipped = 0;

        Logger.Debug($"Looking for properties in: {ResourcesPath}");
        
        if (!Directory.Exists(ResourcesPath))
        {
            Logger.Debug($"Resources directory not found: {ResourcesPath}");
            return (loaded, skipped);
        }

        // Load from C# files
        var csFiles = Directory.GetFiles(ResourcesPath, "*.cs", SearchOption.AllDirectories);
        Logger.Debug($"Found {csFiles.Length} C# files in Resources directory");

        foreach (var filePath in csFiles)
        {
            Logger.Debug($"Processing properties C# file: {filePath}");
            var (loadedCount, skippedCount) = LoadPropertiesFromCsFile(filePath);
            loaded += loadedCount;
            skipped += skippedCount;
        }

        Logger.Debug($"Properties - Loaded: {loaded}, Skipped: {skipped}");
        return (loaded, skipped);
    }

    /// <summary>
    /// Load properties from a C# file
    /// </summary>
    private static (int Loaded, int Skipped) LoadPropertiesFromCsFile(string filePath)
    {
        int loaded = 0;
        int skipped = 0;
        
        try
        {
            var propDefs = CodeDefinitionParser.ParseProperties(filePath);
            var baseDirectory = Path.GetDirectoryName(filePath) ?? "";

            foreach (var propDef in propDefs)
            {
                if (string.IsNullOrEmpty(propDef.Name))
                {
                    Logger.Warning($"Invalid property definition in {filePath}");
                    continue;
                }

                // Handle special file reference values
                if (propDef.Value != null)
                {
                    var valueStr = propDef.Value.ToString();
                    if (valueStr != null && valueStr.Contains("IsFileReference") && valueStr.Contains("Filename"))
                    {
                        // Extract filename from the anonymous object representation
                        var match = System.Text.RegularExpressions.Regex.Match(valueStr, @"Filename = ([^,}]+)");
                        if (match.Success)
                        {
                            propDef.Filename = match.Groups[1].Value.Trim();
                            propDef.Value = null; // Clear the value since we'll use filename
                        }
                    }
                }

                // Determine if this is a class property or system property
                if (propDef.TargetClass?.ToLower() == "system" || string.IsNullOrEmpty(propDef.TargetClass))
                {
                    // System property (global)
                    var systemObjectId = GetOrCreateSystemObject();
                    if (systemObjectId != null)
                    {
                        var systemObject = ObjectManager.GetObject(systemObjectId);
                        
                        if (systemObject != null)
                        {
                            systemObject.PropAccessors[propDef.Name] = propDef.Accessors ?? new List<Keyword> { Keyword.Public};
                            var (loadedCount, skippedCount) = SetSystemProperty(systemObject, propDef, baseDirectory);
                            loaded += loadedCount;
                            skipped += skippedCount;
                        }
                    }
                }
                else
                {
                    var (loadedCount, skippedCount) = SetClassProperty(propDef, baseDirectory);
                    loaded += loadedCount;
                    skipped += skippedCount;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading properties from C# file {filePath}: {ex.Message}");
        }
        
        return (loaded, skipped);
    }

    /// <summary>
    /// Set a property on the system object
    /// </summary>
    private static (int Loaded, int Skipped) SetSystemProperty(GameObject systemObject, PropertyDefinition propDef, string baseDirectory)
    {
        // Never delete properties, only add or overwrite if explicitly requested
        if (systemObject.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
            Logger.Debug($"System property '{propDef.Name}' already exists, skipping (overwrite=false)");
            return (0, 1);
        }

        try
        {
            var value = propDef.GetTypedValue(baseDirectory);
            systemObject.PropAccessors[propDef.Name] = propDef.Accessors ?? new List<Keyword> { Keyword.Public};
             // persist the change so the system object is saved to the DB
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
            return (1, 0);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting system property '{propDef.Name}': {ex.Message}");
            return (0, 1);
        }
    }

    /// <summary>
    /// Set a property on a class (affects all instances of that class)
    /// </summary>
    private static (int Loaded, int Skipped) SetClassProperty(PropertyDefinition propDef, string baseDirectory)
    {
        var targetClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == propDef.TargetClass);
        if (targetClass == null)
        {
            Logger.Warning($"Target class '{propDef.TargetClass}' not found for property '{propDef.Name}'");
            return (0, 0);
        }

        // Never delete properties, only add or overwrite if explicitly requested
        if (targetClass.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
            Logger.Debug($"Class property '{propDef.Name}' on {propDef.TargetClass} already exists, skipping (overwrite=false)");
            return (0, 1);
        }

        try
        {
            targetClass.PropAccessors[propDef.Name] = propDef.Accessors ?? new List<Keyword> { Keyword.Public };
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
            return (1, 0);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting class property '{propDef.Name}' on {propDef.TargetClass}: {ex.Message}");
            return (0, 1);
        }
    }

    /// <summary>
    /// Set a property on a specific object instance
    /// </summary>
    private static (int Loaded, int Skipped) SetInstanceProperty(PropertyDefinition propDef, string baseDirectory)
    {
        // Resolve the target object
        var targetObject = ResolveObject(propDef.TargetObject!);
        if (targetObject == null)
        {
            Logger.Warning($"Target object '{propDef.TargetObject}' not found for property '{propDef.Name}'");
            return (0, 0);
        }

        // Never delete properties, only add or overwrite if explicitly requested
        if (targetObject.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
            Logger.Debug($"Instance property '{propDef.Name}' on {propDef.TargetObject} already exists, skipping (overwrite=false)");
            return (0, 1);
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
            return (1, 0);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting instance property '{propDef.Name}' on {propDef.TargetObject}: {ex.Message}");
            return (0, 1);
        }
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
