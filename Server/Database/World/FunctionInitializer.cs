using System;
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
    private static readonly string FunctionsPath = GetResourcePath("functions");
    private static readonly string SystemFunctionsPath = Path.Combine(FunctionsPath, "system");
    private static readonly string ClassFunctionsPath = Path.Combine(FunctionsPath, "classes");

    /// <summary>
    /// Gets the absolute path to a resource directory, with fallback for development scenarios
    /// </summary>
    private static string GetResourcePath(string resourceName)
    {
        var possiblePaths = new List<string>();
        
        // Strategy 1: Application base directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        possiblePaths.Add(Path.Combine(appDirectory, "resources", resourceName));
        
        // Strategy 2: Current working directory
        var workingDirectory = Directory.GetCurrentDirectory();
        possiblePaths.Add(Path.Combine(workingDirectory, "resources", resourceName));
        
        // Strategy 3: Relative path from current directory
        possiblePaths.Add(Path.Combine("resources", resourceName));
        
        // Strategy 4: Check if we're in a subdirectory and need to go up
        var currentDir = Directory.GetCurrentDirectory();
        var parentDir = Directory.GetParent(currentDir);
        if (parentDir != null)
        {
            possiblePaths.Add(Path.Combine(parentDir.FullName, "resources", resourceName));
        }
        
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        
        // If none exist, return the first option (app directory based) for error reporting
        return possiblePaths[0];
    }

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
    /// Only removes functions that were created from resources folder (CreatedBy == "system")
    /// </summary>
    public static void ReloadFunctions()
    {
        Logger.Info("Hot reloading function definitions...");

        // Only clear function definitions that were loaded from resources
        var allFunctions = DbProvider.Instance.FindAll<Function>("functions").ToList();
        var systemFunctions = allFunctions.Where(f => f.CreatedBy == "system").ToList();
        var inGameFunctions = allFunctions.Where(f => f.CreatedBy != "system").ToList();
        
        var countBefore = allFunctions.Count;
        Logger.Debug($"Found {countBefore} total functions before deletion ({systemFunctions.Count} system, {inGameFunctions.Count} in-game)");
        
        // Only delete system/resource functions
        foreach (var f in systemFunctions) 
        {
            DbProvider.Instance.Delete<Function>("functions", f.Id);
        }
        
        var countAfterDelete = DbProvider.Instance.FindAll<Function>("functions").Count();
        Logger.Debug($"Cleared {systemFunctions.Count} system function definitions - {countAfterDelete} functions remaining (preserving {inGameFunctions.Count} in-game functions)");

        // Reload all functions from JSON files
        var classStats = LoadClassFunctions();
        var systemStats = LoadSystemFunctions();

        var totalLoaded = classStats.Loaded + systemStats.Loaded;
        var totalSkipped = classStats.Skipped + systemStats.Skipped;
        var countAfterReload = DbProvider.Instance.FindAll<Function>("functions").Count();

        Logger.Info($"Function definitions reloaded successfully - Created: {totalLoaded}, Skipped: {totalSkipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Force reload ALL function definitions (removes all functions, loads new)
    /// Use with caution - this will delete in-game created functions too!
    /// </summary>
    public static void ForceReloadAllFunctions()
    {
        Logger.Warning("FORCE reloading ALL function definitions (including in-game functions)...");

        // Clear ALL function definitions from database
        var allFunctions = DbProvider.Instance.FindAll<Function>("functions").ToList();
        var countBefore = allFunctions.Count;
        Logger.Debug($"Found {countBefore} functions before deletion");
        foreach (var f in allFunctions) DbProvider.Instance.Delete<Function>("functions", f.Id);
        var countAfterDelete = DbProvider.Instance.FindAll<Function>("functions").Count();
        Logger.Debug($"Cleared ALL function definitions - {countAfterDelete} functions remaining");

        // Reload all functions from JSON files
        var classStats = LoadClassFunctions();
        var systemStats = LoadSystemFunctions();

        var totalLoaded = classStats.Loaded + systemStats.Loaded;
        var totalSkipped = classStats.Skipped + systemStats.Skipped;
        var countAfterReload = DbProvider.Instance.FindAll<Function>("functions").Count();

        Logger.Info($"ALL function definitions force reloaded - Created: {totalLoaded}, Skipped: {totalSkipped}, Total in DB: {countAfterReload}");
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
                    var csFile = TryFindCodeFile(file);
                    if (!string.IsNullOrEmpty(csFile) && File.Exists(csFile))
                    {
                        functionDef.Code = new[] { File.ReadAllText(csFile) };
                        Logger.Debug($"Loaded code from {csFile} for function {functionDef.Name}");
                    }
                    else
                    {
                        Logger.Debug($"No .cs file found for {file}");
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
                    var csFile = TryFindCodeFile(file);
                    if (!string.IsNullOrEmpty(csFile) && File.Exists(csFile))
                    {
                        functionDef.Code = new[] { File.ReadAllText(csFile) };
                        Logger.Debug($"Loaded code from {csFile} for function {functionDef.Name}");
                    }
                    else
                    {
                        Logger.Debug($"No .cs file found for {file}");
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
        
        var targetClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == functionDef.TargetClass);
        if (targetClass == null)
        {
            Logger.Warning($"Target class '{functionDef.TargetClass}' not found for function '{functionDef.Name}'");
            return stats;
        }

        var existingFunctions = DbProvider.Instance.FindAll<Function>("functions").ToList();
        // Only create if it doesn't already exist
        if (!existingFunctions.Any(f => f.ObjectId == targetClass.Id && f.Name == functionDef.Name))
        {
            // Create function directly since FunctionManager.CreateFunction expects GameObject
            // but we have an ObjectClass. We'll create the Function object directly.
            var function = new Function
            {
                ObjectId = targetClass.Id,
                Name = functionDef.Name,
                ParameterTypes = functionDef.Parameters,
                ParameterNames = functionDef.ParameterNames,
                ReturnType = functionDef.ReturnType,
                Code = functionDef.GetCodeString(),
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            DbProvider.Instance.Insert("functions", function);
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

    /// <summary>
    /// Try to find a code file (.cs) that corresponds to a JSON definition file
    /// Handles case sensitivity issues on Linux by trying multiple variations
    /// </summary>
    private static string? TryFindCodeFile(string jsonFile)
    {
        var directory = Path.GetDirectoryName(jsonFile);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jsonFile);
        
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileNameWithoutExtension))
            return null;

        // First try the exact case match (Path.ChangeExtension approach)
        var exactMatch = Path.ChangeExtension(jsonFile, ".cs");
        if (File.Exists(exactMatch))
        {
            Logger.Debug($"Found exact match: {exactMatch}");
            return exactMatch;
        }

        // On case-sensitive file systems (Linux), try different case variations
        var possibleFiles = new[]
        {
            Path.Combine(directory, fileNameWithoutExtension + ".cs"),
            Path.Combine(directory, fileNameWithoutExtension + ".CS"),
            Path.Combine(directory, fileNameWithoutExtension.ToLower() + ".cs"),
            Path.Combine(directory, fileNameWithoutExtension.ToUpper() + ".cs")
        };

        foreach (var possibleFile in possibleFiles)
        {
            if (File.Exists(possibleFile))
            {
                Logger.Debug($"Found case variation: {possibleFile}");
                return possibleFile;
            }
        }

        // Last resort: scan the directory for any .cs file with matching base name (case-insensitive)
        try
        {
            var csFiles = Directory.GetFiles(directory, "*.cs");
            var matchingFile = csFiles.FirstOrDefault(f => 
                string.Equals(Path.GetFileNameWithoutExtension(f), fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase));
            
            if (matchingFile != null)
            {
                Logger.Debug($"Found by directory scan: {matchingFile}");
                return matchingFile;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Error scanning directory for .cs files: {ex.Message}");
        }

        Logger.Debug($"No .cs file found for {jsonFile}");
        return null;
    }
}
