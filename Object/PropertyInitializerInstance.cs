using LiteDB;
using CSMOO.Logging;
using CSMOO.Database;
using CSMOO.Init;

namespace CSMOO.Object;

/// <summary>
/// Instance-based property initializer implementation for dependency injection
/// </summary>
public class PropertyInitializerInstance : IPropertyInitializer
{
    private readonly IDbProvider _dbProvider;
    private readonly ILogger _logger;
    private readonly IObjectManager _objectManager;
    private readonly string _resourcesPath;
    
    public PropertyInitializerInstance(IDbProvider dbProvider, ILogger logger, IObjectManager objectManager)
    {
        _dbProvider = dbProvider;
        _logger = logger;
        _objectManager = objectManager;
        _resourcesPath = GetResourcePath();
    }
    
    /// <summary>
    /// Gets the absolute path to the resources directory
    /// </summary>
    private static string GetResourcePath()
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        var resourcesPath = Path.Combine(workingDirectory, "Resources");
        return resourcesPath;
    }
    
    /// <summary>
    /// Loads and sets all properties from C# class definitions
    /// </summary>
    public void LoadAndSetProperties()
    {
        _logger.Info("Loading property definitions from C# files...");

        var stats = LoadProperties();

        _logger.Info($"Property definitions loaded successfully - Set: {stats.Loaded}, Skipped: {stats.Skipped}");
    }

    /// <summary>
    /// Hot reload all property definitions
    /// </summary>
    public void ReloadProperties()
    {
        _logger.Info("Hot reloading property definitions...");

        // For properties, we don't clear existing ones as they might be modified by players
        // Instead, we only update properties that have overwrite=true in their definition
        
        var stats = LoadProperties();

        _logger.Info($"Property definitions reloaded successfully - Set: {stats.Loaded}, Skipped: {stats.Skipped}");
    }

    /// <summary>
    /// Loads and creates properties from all C# class definitions in Resources directory
    /// </summary>
    public (int Loaded, int Skipped) LoadProperties()
    {
        int loaded = 0;
        int skipped = 0;

        
        if (!Directory.Exists(_resourcesPath))
        {
            _logger.Warning($"Resources directory not found: {_resourcesPath}");
            return (loaded, skipped);
        }

        // Load from C# files
        var csFiles = Directory.GetFiles(_resourcesPath, "*.cs", SearchOption.AllDirectories);

        foreach (var filePath in csFiles)
        {
            var (loadedCount, skippedCount) = LoadPropertiesFromCsFile(filePath);
            loaded += loadedCount;
            skipped += skippedCount;
        }

        _logger.Info($"Properties - Loaded: {loaded}, Skipped: {skipped}");
        return (loaded, skipped);
    }

    /// <summary>
    /// Load properties from a C# file
    /// </summary>
    private (int Loaded, int Skipped) LoadPropertiesFromCsFile(string filePath)
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
                    _logger.Warning($"Invalid property definition in {filePath}");
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
                        var systemObject = _objectManager.GetObject(systemObjectId);
                        
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
            _logger.Error($"Error loading properties from C# file {filePath}: {ex.Message}");
        }
        
        return (loaded, skipped);
    }

    /// <summary>
    /// Set a property on the system object
    /// </summary>
    private (int Loaded, int Skipped) SetSystemProperty(GameObject systemObject, PropertyDefinition propDef, string baseDirectory)
    {
        // Never delete properties, only add or overwrite if explicitly requested
        if (systemObject.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
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
                _objectManager.SetProperty(systemObject, propDef.Name, bsonArray);
            }
            else
            {
                _objectManager.SetProperty(systemObject, propDef.Name, new BsonValue(value));
            }

            return (1, 0);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error setting system property '{propDef.Name}': {ex.Message}");
            return (0, 1);
        }
    }

    /// <summary>
    /// Set a property on a class (affects all instances of that class)
    /// </summary>
    private (int Loaded, int Skipped) SetClassProperty(PropertyDefinition propDef, string baseDirectory)
    {
        var targetClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == propDef.TargetClass);
        if (targetClass == null)
        {
            _logger.Warning($"Target class '{propDef.TargetClass}' not found for property '{propDef.Name}'");
            return (0, 0);
        }

        // Never delete properties, only add or overwrite if explicitly requested
        if (targetClass.Properties.ContainsKey(propDef.Name) && !propDef.Overwrite)
        {
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
            
            _dbProvider.Update("objectclasses", targetClass);
            return (1, 0);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error setting class property '{propDef.Name}' on {propDef.TargetClass}: {ex.Message}");
            return (0, 1);
        }
    }

    /// <summary>
    /// Gets or creates the system object for holding global properties
    /// </summary>
    private string? GetOrCreateSystemObject()
    {
        // Get all objects and filter in memory (LiteDB doesn't support ContainsKey in expressions)
        var allObjects = _dbProvider.FindAll<GameObject>("gameobjects").ToList();
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        
        if (systemObj == null)
        {
            // System object doesn't exist, create it
            _logger.Warning("System object not found, creating it...");
            // Use Container class instead of abstract Object class
            var containerClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == "Container");
            if (containerClass != null)
            {
                systemObj = _objectManager.CreateInstance(containerClass.Id);
                _objectManager.SetProperty(systemObj, "name", "System");
                _objectManager.SetProperty(systemObj, "shortDescription", "the system object");
                _objectManager.SetProperty(systemObj, "longDescription", "This is the system object that holds global verbs and functions.");
                _objectManager.SetProperty(systemObj, "isSystemObject", true);
                _objectManager.SetProperty(systemObj, "gettable", false); // Don't allow players to pick up the system
            }
            else
            {
                _logger.Error("Could not find Container class to create system object!");
                return null;
            }
        }
        return systemObj?.Id;
    }
}
