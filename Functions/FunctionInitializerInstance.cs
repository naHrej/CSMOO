using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Init;

namespace CSMOO.Functions;

/// <summary>
/// Instance-based function initializer implementation for dependency injection
/// </summary>
public class FunctionInitializerInstance : IFunctionInitializer
{
    private readonly IDbProvider _dbProvider;
    private readonly ILogger _logger;
    private readonly IObjectManager _objectManager;
    private readonly IFunctionManager _functionManager;
    private readonly string _resourcesPath;
    
    public FunctionInitializerInstance(IDbProvider dbProvider, ILogger logger, IObjectManager objectManager, IFunctionManager functionManager)
    {
        _dbProvider = dbProvider;
        _logger = logger;
        _objectManager = objectManager;
        _functionManager = functionManager;
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
    /// Loads and creates all functions from C# class definitions
    /// </summary>
    public void LoadAndCreateFunctions()
    {
        _logger.Info("Loading function definitions from C# files...");

        var stats = LoadFunctions();

        _logger.Info($"Function definitions loaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}");
    }

    /// <summary>
    /// Hot reload all function definitions (removes old, loads new)
    /// Only removes functions that were created from resources folder (CreatedBy == "system")
    /// </summary>
    public void ReloadFunctions()
    {
        _logger.Info("Hot reloading function definitions...");

        // Only clear function definitions that were loaded from resources
        var allFunctions = _dbProvider.FindAll<Function>("functions").ToList();
        var systemFunctions = allFunctions.Where(f => f.CreatedBy == "system").ToList();
        var inGameFunctions = allFunctions.Where(f => f.CreatedBy != "system").ToList();
        
        var countBefore = allFunctions.Count;
        
        // Only delete system/resource functions
        foreach (var f in systemFunctions) 
        {
            _dbProvider.Delete<Function>("functions", f.Id);
        }
        
        var countAfterDelete = _dbProvider.FindAll<Function>("functions").Count();

        // Reload all functions from C# files
        var stats = LoadFunctions();

        var countAfterReload = _dbProvider.FindAll<Function>("functions").Count();

        _logger.Info($"Function definitions reloaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Force reload ALL function definitions (removes all functions, loads new)
    /// Use with caution - this will delete in-game created functions too!
    /// </summary>
    public void ForceReloadAllFunctions()
    {
        _logger.Warning("FORCE reloading ALL function definitions (including in-game functions)...");

        // Clear ALL function definitions from database
        var allFunctions = _dbProvider.FindAll<Function>("functions").ToList();
        var countBefore = allFunctions.Count;
        foreach (var f in allFunctions) _dbProvider.Delete<Function>("functions", f.Id);
        var countAfterDelete = _dbProvider.FindAll<Function>("functions").Count();

        // Reload all functions from C# files
        var stats = LoadFunctions();

        var countAfterReload = _dbProvider.FindAll<Function>("functions").Count();

        _logger.Info($"ALL function definitions force reloaded - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Loads and creates functions from all C# definitions in Resources directory
    /// </summary>
    public (int Loaded, int Skipped) LoadFunctions()
    {
        int loaded = 0;
        int skipped = 0;

        
        if (!Directory.Exists(_resourcesPath))
        {
            return (loaded, skipped);
        }

        var csFiles = Directory.GetFiles(_resourcesPath, "*.cs", SearchOption.AllDirectories);

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
    private (int Loaded, int Skipped) LoadFunctionsFromFile(string filePath)
    {
        int loaded = 0;
        int skipped = 0;
        
        try
        {
            _logger.Info($"Loading functions from file: {filePath}");
            var functionDefs = CodeDefinitionParser.ParseFunctions(filePath);
            _logger.Info($"Parsed {functionDefs.Count} function definition(s) from {filePath}");
            
            foreach (var functionDef in functionDefs)
            {
                if (string.IsNullOrEmpty(functionDef.Name))
                {
                    _logger.Warning($"Invalid function definition in {filePath}");
                    continue;
                }

                _logger.Info($"Processing function '{functionDef.Name}' from class '{functionDef.TargetClass}' in {filePath}");

                // Determine if this is a class function or system function
                if (functionDef.TargetClass?.ToLower() == "system" || string.IsNullOrEmpty(functionDef.TargetClass))
                {
                    // System function (global)
                    _logger.Info($"Function '{functionDef.Name}' is a system function, attaching to system object");
                    var systemObjectId = GetOrCreateSystemObject();
                    if (systemObjectId != null)
                    {
                        _logger.Info($"System object ID: {systemObjectId}");
                        var (loadedCount, skippedCount) = CreateSystemFunction(systemObjectId, functionDef);
                        loaded += loadedCount;
                        skipped += skippedCount;
                        if (loadedCount > 0)
                        {
                            _logger.Info($"Successfully created system function '{functionDef.Name}' on system object {systemObjectId}");
                        }
                        else if (skippedCount > 0)
                        {
                            _logger.Info($"Skipped system function '{functionDef.Name}' (already exists)");
                        }
                    }
                    else
                    {
                        _logger.Error($"Failed to get or create system object for function '{functionDef.Name}'");
                    }
                }
                else
                {
                    _logger.Info($"Function '{functionDef.Name}' is a class function for class '{functionDef.TargetClass}'");
                    var (loadedCount, skippedCount) = CreateClassFunction(functionDef);
                    loaded += loadedCount;
                    skipped += skippedCount;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading function definition from {filePath}: {ex.Message}");
            _logger.Error($"Stack trace: {ex.StackTrace}");
        }
        
        return (loaded, skipped);
    }

    /// <summary>
    /// Create a function on a class from a definition
    /// </summary>
    private (int Loaded, int Skipped) CreateClassFunction(FunctionDefinition functionDef)
    {
        var targetClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == functionDef.TargetClass);
        if (targetClass == null)
        {
            _logger.Warning($"Target class '{functionDef.TargetClass}' not found for function '{functionDef.Name}'");
            return (0, 0);
        }

        var existingFunctions = _dbProvider.FindAll<Function>("functions").ToList();
        var existingFunction = existingFunctions.FirstOrDefault(f => f.ObjectId == targetClass.Id && f.Name == functionDef.Name);
        
        if (existingFunction == null)
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
                ModifiedAt = DateTime.UtcNow,
                Description = functionDef.Description ?? string.Empty,
                Categories = string.Join(",", functionDef.Categories),
                Topics = string.Join(",", functionDef.Topics),
                Usage = functionDef.Usage,
                HelpText = functionDef.HelpText
            };

            _dbProvider.Insert("functions", function);
            return (1, 0);
        }
        else
        {
            // Update existing function if it was created by system (from resources)
            if (existingFunction.CreatedBy == "system")
            {
                existingFunction.ParameterTypes = functionDef.Parameters;
                existingFunction.ParameterNames = functionDef.ParameterNames;
                existingFunction.ReturnType = functionDef.ReturnType;
                existingFunction.Code = functionDef.GetCodeString();
                existingFunction.ModifiedAt = DateTime.UtcNow;
                existingFunction.Description = functionDef.Description ?? string.Empty;
                existingFunction.Categories = string.Join(",", functionDef.Categories);
                existingFunction.Topics = string.Join(",", functionDef.Topics);
                existingFunction.Usage = functionDef.Usage;
                existingFunction.HelpText = functionDef.HelpText;
                
                _dbProvider.Update("functions", existingFunction);
                _logger.Info($"Updated existing system function '{functionDef.Name}' on class '{functionDef.TargetClass}'");
                return (1, 0);
            }
            else
            {
                _logger.Info($"Skipped function '{functionDef.Name}' on class '{functionDef.TargetClass}' (created by user, not system)");
                return (0, 1);
            }
        }
    }

    /// <summary>
    /// Create a system function from a definition
    /// </summary>
    private (int Loaded, int Skipped) CreateSystemFunction(string systemObjectId, FunctionDefinition functionDef)
    {
        var existingFunctions = _dbProvider.FindAll<Function>("functions").ToList();
        // Check if function exists (case-insensitive for name changes like DisplayLogin -> display_login)
        var existingFunction = existingFunctions.FirstOrDefault(f => 
            f.ObjectId == systemObjectId && 
            f.Name.Equals(functionDef.Name, StringComparison.OrdinalIgnoreCase) &&
            f.CreatedBy == "system");
        
        if (existingFunction != null)
        {
            // If name changed (case difference), delete old and create new
            if (!existingFunction.Name.Equals(functionDef.Name, StringComparison.Ordinal))
            {
                _logger.Info($"Function name changed from '{existingFunction.Name}' to '{functionDef.Name}', updating...");
                _dbProvider.Delete<Function>("functions", existingFunction.Id);
            }
            else
            {
                // Function exists with same name - update it if it's a system function
                if (existingFunction.CreatedBy == "system")
                {
                    existingFunction.ParameterTypes = functionDef.Parameters;
                    existingFunction.ParameterNames = functionDef.ParameterNames;
                    existingFunction.ReturnType = functionDef.ReturnType;
                    existingFunction.Code = functionDef.GetCodeString();
                    existingFunction.ModifiedAt = DateTime.UtcNow;
                    existingFunction.Description = functionDef.Description ?? string.Empty;
                    existingFunction.Categories = string.Join(",", functionDef.Categories);
                    existingFunction.Topics = string.Join(",", functionDef.Topics);
                    existingFunction.Usage = functionDef.Usage;
                    existingFunction.HelpText = functionDef.HelpText;
                    
                    _dbProvider.Update("functions", existingFunction);
                    _logger.Info($"Updated existing system function '{functionDef.Name}' on system object");
                    return (1, 0);
                }
                else
                {
                    // Function exists but was created by user, skip
                    return (0, 1);
                }
            }
        }
        
        var systemObject = _objectManager.GetObject(systemObjectId);
        if (systemObject == null)
        {
            _logger.Error($"System object with ID {systemObjectId} not found for function '{functionDef.Name}'");
            return (0, 0);
        }
        // Create the function using the injected function manager
        var function = _functionManager.CreateFunction(
            systemObject, 
            functionDef.Name, 
            functionDef.Parameters,
            functionDef.ParameterNames,
            functionDef.ReturnType,
            functionDef.GetCodeString(), 
            "system"
        );
        // Set description and help metadata
        function.Description = functionDef.Description ?? string.Empty;
        function.Categories = string.Join(",", functionDef.Categories);
        function.Topics = string.Join(",", functionDef.Topics);
        function.Usage = functionDef.Usage;
        function.HelpText = functionDef.HelpText;
        _dbProvider.Update("functions", function);
        return (1, 0);
    }

    /// <summary>
    /// Gets or creates the system object for holding global functions
    /// </summary>
    private string? GetOrCreateSystemObject()
    {
        const string systemObjectName = "system";
        
        // Try to find existing system object
        var gameObjects = _dbProvider.FindAll<GameObject>("gameobjects");
        
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
            
            _dbProvider.Insert("gameobjects", systemObject);
            return systemObject.Id;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create system object: {ex.Message}");
            return null;
        }
    }
}
