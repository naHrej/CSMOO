using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Init;

namespace CSMOO.Functions;

/// <summary>
/// <summary>
/// Handles loading and initializing functions from C# class definitions
/// </summary>
public static class FunctionInitializer
{
    private static readonly string ResourcesPath = GetResourcePath();

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
    /// Loads and creates all functions from C# class definitions
    /// </summary>
    public static void LoadAndCreateFunctions()
    {
        Logger.Info("Loading function definitions from C# files...");

        var stats = LoadFunctions();

        Logger.Info($"Function definitions loaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}");
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
        
        // Only delete system/resource functions
        foreach (var f in systemFunctions) 
        {
            DbProvider.Instance.Delete<Function>("functions", f.Id);
        }
        
        var countAfterDelete = DbProvider.Instance.FindAll<Function>("functions").Count();

        // Reload all functions from C# files
        var stats = LoadFunctions();

        var countAfterReload = DbProvider.Instance.FindAll<Function>("functions").Count();

        Logger.Info($"Function definitions reloaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
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
        foreach (var f in allFunctions) DbProvider.Instance.Delete<Function>("functions", f.Id);
        var countAfterDelete = DbProvider.Instance.FindAll<Function>("functions").Count();

        // Reload all functions from C# files
        var stats = LoadFunctions();

        var countAfterReload = DbProvider.Instance.FindAll<Function>("functions").Count();

        Logger.Info($"ALL function definitions force reloaded - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Loads and creates functions from all C# definitions in Resources directory
    /// </summary>
    public static (int Loaded, int Skipped) LoadFunctions()
    {
        int loaded = 0;
        int skipped = 0;

        
        if (!Directory.Exists(ResourcesPath))
        {
            return (loaded, skipped);
        }

        var csFiles = Directory.GetFiles(ResourcesPath, "*.cs", SearchOption.AllDirectories);

        foreach (var filePath in csFiles)
        {
            var (loadedCount, skippedCount) = LoadFunctionsFromFile(filePath);
            loaded += loadedCount;
            skipped += skippedCount;
        }

        return (loaded, skipped);
    }

    /// <summary>
    /// Load functions from a specific C# file
    /// </summary>
    private static (int Loaded, int Skipped) LoadFunctionsFromFile(string filePath)
    {
        int loaded = 0;
        int skipped = 0;
        
        try
        {
            var functionDefs = CodeDefinitionParser.ParseFunctions(filePath);
            
            foreach (var functionDef in functionDefs)
            {
                if (string.IsNullOrEmpty(functionDef.Name))
                {
                    Logger.Warning($"Invalid function definition in {filePath}");
                    continue;
                }

                // Determine if this is a class function or system function
                if (functionDef.TargetClass?.ToLower() == "system" || string.IsNullOrEmpty(functionDef.TargetClass))
                {
                    // System function (global)
                    var systemObjectId = GetOrCreateSystemObject();
                    if (systemObjectId != null)
                    {
                        var (loadedCount, skippedCount) = CreateSystemFunction(systemObjectId, functionDef);
                        loaded += loadedCount;
                        skipped += skippedCount;
                    }
                }
                else
                {
                    var (loadedCount, skippedCount) = CreateClassFunction(functionDef);
                    loaded += loadedCount;
                    skipped += skippedCount;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading function definition from {filePath}: {ex.Message}");
        }
        
        return (loaded, skipped);
    }

    /// <summary>
    /// Create a function on a class from a definition
    /// </summary>
    private static (int Loaded, int Skipped) CreateClassFunction(FunctionDefinition functionDef)
    {
        var targetClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == functionDef.TargetClass);
        if (targetClass == null)
        {
            Logger.Warning($"Target class '{functionDef.TargetClass}' not found for function '{functionDef.Name}'");
            return (0, 0);
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
            return (1, 0);
        }
        else
        {
            return (0, 1);
        }
    }

    /// <summary>
    /// Create a system function from a definition
    /// </summary>
    private static (int Loaded, int Skipped) CreateSystemFunction(string systemObjectId, FunctionDefinition functionDef)
    {
        var existingFunctions = DbProvider.Instance.FindAll<Function>("functions").ToList();
        // Only create if it doesn't already exist
        if (!existingFunctions.Any(f => f.ObjectId == systemObjectId && f.Name == functionDef.Name))
        {
            var systemObject = ObjectManager.GetObject(systemObjectId);
            if (systemObject == null)
            {
                Logger.Error($"System object with ID {systemObjectId} not found for function '{functionDef.Name}'");
                return (0, 0);
            }
            // Find the system object GameObject
            var function = FunctionManager.CreateFunction(
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
            return (1, 0);
        }
        else
        {
            return (0, 1);
        }
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
                return matchingFile;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Error scanning directory for .cs files: {ex.Message}");
        }

        return null;
    }
}



