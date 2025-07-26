using System.Linq;
using System.IO;
using System.Text.Json;
using LiteDB;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database.World;

/// <summary>
/// Statistics for function loading operations
/// </summary>
public struct FunctionLoadStats
{
    public int Loaded { get; set; }
    public int Skipped { get; set; }
}

/// <summary>
/// Handles loading and initializing functions from JSON definitions
/// </summary>
public static class FunctionInitializer
{
    private static readonly string FunctionsPath = Path.Combine("resources", "functions");
    private static readonly string SystemFunctionsPath = Path.Combine(FunctionsPath, "system");
    private static readonly string ClassFunctionsPath = Path.Combine(FunctionsPath, "classes");

    /// <summary>
    /// Loads and creates all functions from JSON definitions
    /// </summary>
    public static void LoadAndCreateFunctions()
    {
        Logger.Info("Loading function definitions from JSON files...");

        var classStats = LoadClassFunctions();
        var systemStats = LoadSystemFunctions();

        var totalLoaded = classStats.Loaded + systemStats.Loaded;
        var totalSkipped = classStats.Skipped + systemStats.Skipped;

        Logger.Info($"Function definitions loaded successfully - Created: {totalLoaded}, Skipped: {totalSkipped}");
    }

    /// <summary>
    /// Hot reload all function definitions (removes old, loads new)
    /// </summary>
    public static void ReloadFunctions()
    {
        Logger.Info("Hot reloading function definitions...");

        // Clear existing function definitions from database
        var allFunctions = DbProvider.Instance.FindAll<Function>("functions").ToList();
        var countBefore = allFunctions.Count;
        Logger.Debug($"Found {countBefore} functions before deletion");
        foreach (var f in allFunctions) DbProvider.Instance.Delete<Function>("functions", f.Id);
        var countAfterDelete = DbProvider.Instance.FindAll<Function>("functions").Count();
        Logger.Debug($"Cleared existing function definitions - {countAfterDelete} functions remaining");

        // Reload all functions from JSON files
        var classStats = LoadClassFunctions();
        var systemStats = LoadSystemFunctions();

        var totalLoaded = classStats.Loaded + systemStats.Loaded;
        var totalSkipped = classStats.Skipped + systemStats.Skipped;
        var countAfterReload = DbProvider.Instance.FindAll<Function>("functions").Count();

        Logger.Info($"Function definitions reloaded successfully - Created: {totalLoaded}, Skipped: {totalSkipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Load class-based function definitions
    /// </summary>
    private static FunctionLoadStats LoadClassFunctions()
    {
        var stats = new FunctionLoadStats();
        
        if (!Directory.Exists(ClassFunctionsPath))
        {
            Logger.Debug($"Class functions directory not found: {ClassFunctionsPath}");
            return stats;
        }

        var jsonFiles = Directory.GetFiles(ClassFunctionsPath, "*.json");
        Logger.Debug($"Found {jsonFiles.Length} class function definition files");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var functionDef = System.Text.Json.JsonSerializer.Deserialize<FunctionDefinition>(json);

                if (functionDef == null || string.IsNullOrEmpty(functionDef.Name) || string.IsNullOrEmpty(functionDef.TargetClass))
                {
                    Logger.Warning($"Invalid function definition in {file}");
                    continue;
                }

                // If code is missing or empty, look for a .cs file with the same base name
                if (functionDef.Code == null || functionDef.Code.Length == 0)
                {
                    var csFile = Path.ChangeExtension(file, ".cs");
                    if (File.Exists(csFile))
                    {
                        functionDef.Code = new[] { File.ReadAllText(csFile) };
                        Logger.Debug($"Loaded code from {csFile} for function {functionDef.Name}");
                    }
                }

                var functionStats = CreateClassFunction(functionDef);
                stats.Loaded += functionStats.Loaded;
                stats.Skipped += functionStats.Skipped;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading function definition from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Load system function definitions
    /// </summary>
    private static FunctionLoadStats LoadSystemFunctions()
    {
        var stats = new FunctionLoadStats();
        
        if (!Directory.Exists(SystemFunctionsPath))
        {
            Logger.Debug($"System functions directory not found: {SystemFunctionsPath}");
            return stats;
        }

        var systemObjectId = GetOrCreateSystemObject();
        if (systemObjectId == null)
        {
            Logger.Error("Failed to create system object for system functions");
            return stats;
        }

        var jsonFiles = Directory.GetFiles(SystemFunctionsPath, "*.json");
        Logger.Debug($"Found {jsonFiles.Length} system function definition files");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var functionDef = System.Text.Json.JsonSerializer.Deserialize<FunctionDefinition>(json);

                if (functionDef == null || string.IsNullOrEmpty(functionDef.Name))
                {
                    Logger.Warning($"Invalid function definition in {file}");
                    continue;
                }

                // If code is missing or empty, look for a .cs file with the same base name
                if (functionDef.Code == null || functionDef.Code.Length == 0)
                {
                    var csFile = Path.ChangeExtension(file, ".cs");
                    if (File.Exists(csFile))
                    {
                        functionDef.Code = new[] { File.ReadAllText(csFile) };
                        Logger.Debug($"Loaded code from {csFile} for function {functionDef.Name}");
                    }
                }

                var functionStats = CreateSystemFunction(systemObjectId, functionDef);
                stats.Loaded += functionStats.Loaded;
                stats.Skipped += functionStats.Skipped;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading function definition from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Create a function on a class from a definition
    /// </summary>
    private static FunctionLoadStats CreateClassFunction(FunctionDefinition functionDef)
    {
        var stats = new FunctionLoadStats();
        
        var targetClass = DbProvider.Instance.FindOne<GameObject>("objectclasses", c => c.Name == functionDef.TargetClass);
        if (targetClass == null)
        {
            Logger.Warning($"Target class '{functionDef.TargetClass}' not found for function '{functionDef.Name}'");
            return stats;
        }

        var existingFunctions = DbProvider.Instance.FindAll<Function>("functions").ToList();
        // Only create if it doesn't already exist
        if (!existingFunctions.Any(f => f.ObjectId == targetClass.Id && f.Name == functionDef.Name))
        {
            var function = Scripting.FunctionManager.CreateFunction(
                targetClass, 
                functionDef.Name, 
                functionDef.Parameters,
                functionDef.ParameterNames,
                functionDef.ReturnType,
                functionDef.GetCodeString(), 
                "system"
            );
            // Set description if provided
            if (!string.IsNullOrEmpty(functionDef.Description))
            {
                function.Description = functionDef.Description;
                DbProvider.Instance.Update("functions", function);
            }
            Logger.Debug($"Created class function '{functionDef.Name}' on {functionDef.TargetClass}");
            stats.Loaded = 1;
        }
        else
        {
            Logger.Debug($"Class function '{functionDef.Name}' on {functionDef.TargetClass} already exists, skipping");
            stats.Skipped = 1;
        }
        
        return stats;
    }

    /// <summary>
    /// Create a system function from a definition
    /// </summary>
    private static FunctionLoadStats CreateSystemFunction(string systemObjectId, FunctionDefinition functionDef)
    {
        var stats = new FunctionLoadStats();
        var existingFunctions = DbProvider.Instance.FindAll<Function>("functions").ToList();
        // Only create if it doesn't already exist
        if (!existingFunctions.Any(f => f.ObjectId == systemObjectId && f.Name == functionDef.Name))
        {
            var systemObject = ObjectManager.GetObject(systemObjectId);
            if (systemObject == null)
            {
                Logger.Error($"System object with ID {systemObjectId} not found for function '{functionDef.Name}'");
                return stats;
            }
            // Find the system object GameObject
            var function = Scripting.FunctionManager.CreateFunction(
                systemObject, 
                functionDef.Name, 
                functionDef.Parameters,
                functionDef.ParameterNames,
                functionDef.ReturnType,
                functionDef.GetCodeString(), 
                "system"
            );
            // Set description if provided
            if (!string.IsNullOrEmpty(functionDef.Description))
            {
                function.Description = functionDef.Description;
                DbProvider.Instance.Update("functions", function);
            }
            Logger.Debug($"Created system function '{functionDef.Name}'");
            stats.Loaded = 1;
        }
        else
        {
            Logger.Debug($"System function '{functionDef.Name}' already exists, skipping");
            stats.Skipped = 1;
        }
        
        return stats;
    }

    /// <summary>
    /// Gets or creates the system object for holding global functions
    /// </summary>
    private static string? GetOrCreateSystemObject()
    {
        const string systemObjectName = "system";
        
        // Try to find existing system object
        var gameObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        
        // Get all objects and search in memory since LiteDB doesn't support ContainsKey in expressions
        var systemObject = gameObjects.FirstOrDefault(obj => 
            (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == systemObjectName) ||
            (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
        
        if (systemObject != null)
        {
            return systemObject.Id;
        }
        
        // Create new system object
        try
        {
            systemObject = new GameObject
            {
                Id = Guid.NewGuid().ToString(),
                Properties = new BsonDocument
                {
                    ["name"] = systemObjectName,
                    ["shortDescription"] = "The system object",
                    ["longDescription"] = "This is the system object that holds global functions and system-level functionality.",
                    ["isSystemObject"] = true
                },
                Location = null // System object has no location
            };
            
            DbProvider.Instance.Insert("gameobjects", systemObject);
            Logger.Debug("Created system object for global functions");
            return systemObject.Id;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create system object: {ex.Message}");
            return null;
        }
    }
}
