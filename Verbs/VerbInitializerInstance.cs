using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Init;

namespace CSMOO.Verbs;

/// <summary>
/// Instance-based verb initializer implementation for dependency injection
/// </summary>
public class VerbInitializerInstance : IVerbInitializer
{
    private readonly IDbProvider _dbProvider;
    private readonly ILogger _logger;
    private readonly IObjectManager _objectManager;
    private readonly string _resourcesPath;
    
    public VerbInitializerInstance(IDbProvider dbProvider, ILogger logger, IObjectManager objectManager)
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
    /// Loads and creates all verbs from C# class definitions
    /// </summary>
    public void LoadAndCreateVerbs()
    {
        _logger.Info("Loading verb definitions from C# files...");

        var stats = LoadVerbs();

        _logger.Info($"Verb definitions loaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}");
    }

    /// <summary>
    /// Hot reload all verb definitions (removes old, loads new)
    /// Only removes verbs that were created from resources folder (CreatedBy == "system")
    /// </summary>
    public void ReloadVerbs()
    {
        _logger.Info("Hot reloading verb definitions...");

        // Only clear verb definitions that were loaded from resources
        var allVerbs = _dbProvider.FindAll<Verb>("verbs").ToList();
        var systemVerbs = allVerbs.Where(v => v.CreatedBy == "system").ToList();
        var inGameVerbs = allVerbs.Where(v => v.CreatedBy != "system").ToList();
        
        var countBefore = allVerbs.Count;
        
        // Only delete system/resource verbs
        foreach (var v in systemVerbs) 
        {
            _dbProvider.Delete<Verb>("verbs", v.Id);
        }
        
        var countAfterDelete = _dbProvider.FindAll<Verb>("verbs").Count();

        // Reload all verbs from C# files
        var stats = LoadVerbs();

        var countAfterReload = _dbProvider.FindAll<Verb>("verbs").Count();

        _logger.Info($"Verb definitions reloaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Force reload ALL verb definitions (removes all verbs, loads new)
    /// Use with caution - this will delete in-game created verbs too!
    /// </summary>
    public void ForceReloadAllVerbs()
    {
        _logger.Warning("FORCE reloading ALL verb definitions (including in-game verbs)...");

        // Clear ALL verb definitions from database
        var allVerbs = _dbProvider.FindAll<Verb>("verbs").ToList();
        var countBefore = allVerbs.Count;
        foreach (var v in allVerbs) _dbProvider.Delete<Verb>("verbs", v.Id);
        var countAfterDelete = _dbProvider.FindAll<Verb>("verbs").Count();

        // Reload all verbs from C# files
        var stats = LoadVerbs();

        var countAfterReload = _dbProvider.FindAll<Verb>("verbs").Count();

        _logger.Info($"ALL verb definitions force reloaded - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Load verb definitions from all C# files in Resources directory
    /// </summary>
    private VerbLoadStats LoadVerbs()
    {
        var stats = new VerbLoadStats();
        
        if (!Directory.Exists(_resourcesPath))
        {
            return stats;
        }

        // Get all .cs files recursively from Resources directory
        var csFiles = Directory.GetFiles(_resourcesPath, "*.cs", SearchOption.AllDirectories);

        // First, parse HelpMetadata if it exists
        var helpMetadataFile = csFiles.FirstOrDefault(f => Path.GetFileName(f) == "HelpMetadata.cs");
        if (helpMetadataFile != null)
        {
            CodeDefinitionParser.ParseHelpMetadata(helpMetadataFile);
        }

        foreach (var file in csFiles)
        {
            try
            {
                var verbDefs = CodeDefinitionParser.ParseVerbs(file);
                
                foreach (var verbDef in verbDefs)
                {
                    if (string.IsNullOrEmpty(verbDef.Name))
                    {
                        _logger.Warning($"Invalid verb definition in {file}");
                        continue;
                    }

                    // Determine if this should be a system verb or class verb based on class name
                    VerbLoadStats verbStats;
                    if (verbDef.TargetClass?.ToLower() == "system")
                    {
                        var systemObjectId = GetOrCreateSystemObject();
                        if (systemObjectId != null)
                        {
                            verbStats = CreateSystemVerb(systemObjectId, verbDef);
                        }
                        else
                        {
                            _logger.Error("Failed to create system object for system verbs");
                            continue;
                        }
                    }
                    else
                    {
                        verbStats = CreateClassVerb(verbDef);
                    }
                    
                    stats.Loaded += verbStats.Loaded;
                    stats.Skipped += verbStats.Skipped;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading verb definition from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Create a verb on a class from a definition
    /// </summary>
    private VerbLoadStats CreateClassVerb(VerbDefinition verbDef)
    {
        var stats = new VerbLoadStats();
        
        var targetClass = _dbProvider.FindOne<ObjectClass>("objectclasses", c => c.Name == verbDef.TargetClass);
        if (targetClass == null)
        {
            _logger.Warning($"Target class '{verbDef.TargetClass}' not found for verb '{verbDef.Name}'");
            return stats;
        }

        var existingVerbs = _dbProvider.FindAll<Verb>("verbs").ToList();
        var existingVerb = existingVerbs.FirstOrDefault(v => v.ObjectId == targetClass.Id && v.Name == verbDef.Name);
        if (existingVerb == null)
        {
            // Create new verb
            var verb = VerbManager.CreateVerb(
                targetClass.Id, 
                verbDef.Name, 
                verbDef.Pattern, 
                verbDef.GetCodeString(), 
                "system"
            );
            // Set aliases if provided
            if (!string.IsNullOrEmpty(verbDef.Aliases))
            {
                verb.Aliases = verbDef.Aliases;
            }
            // Set description if provided
            if (!string.IsNullOrEmpty(verbDef.Description))
            {
                verb.Description = verbDef.Description;
            }
            // Set help metadata
            verb.Categories = string.Join(",", verbDef.Categories);
            verb.Topics = string.Join(",", verbDef.Topics);
            verb.Usage = verbDef.Usage;
            verb.HelpText = verbDef.HelpText;
            _dbProvider.Update("verbs", verb);
            stats.Loaded = 1;
        }
        else
        {
            // Update existing verb with new metadata (especially help metadata)
            existingVerb.Pattern = verbDef.Pattern;
            existingVerb.Code = verbDef.GetCodeString();
            if (!string.IsNullOrEmpty(verbDef.Aliases))
            {
                existingVerb.Aliases = verbDef.Aliases;
            }
            if (!string.IsNullOrEmpty(verbDef.Description))
            {
                existingVerb.Description = verbDef.Description;
            }
            // Always update help metadata (this is the key fix)
            existingVerb.Categories = string.Join(",", verbDef.Categories);
            existingVerb.Topics = string.Join(",", verbDef.Topics);
            existingVerb.Usage = verbDef.Usage;
            existingVerb.HelpText = verbDef.HelpText;
            _dbProvider.Update("verbs", existingVerb);
            stats.Skipped = 1; // Still count as skipped since we didn't create it
        }
        
        return stats;
    }

    /// <summary>
    /// Create a system verb from a definition
    /// </summary>
    private VerbLoadStats CreateSystemVerb(string systemObjectId, VerbDefinition verbDef)
    {
        var stats = new VerbLoadStats();
        var existingVerbs = _dbProvider.FindAll<Verb>("verbs").ToList();
        var existingVerb = existingVerbs.FirstOrDefault(v => v.ObjectId == systemObjectId && v.Name == verbDef.Name);
        if (existingVerb == null)
        {
            // Create new verb
            var verb = VerbManager.CreateVerb(
                systemObjectId, 
                verbDef.Name, 
                verbDef.Pattern, 
                verbDef.GetCodeString(), 
                "system"
            );
            // Set aliases if provided
            if (!string.IsNullOrEmpty(verbDef.Aliases))
            {
                verb.Aliases = verbDef.Aliases;
            }
            // Set description if provided
            if (!string.IsNullOrEmpty(verbDef.Description))
            {
                verb.Description = verbDef.Description;
            }
            // Set help metadata
            verb.Categories = string.Join(",", verbDef.Categories);
            verb.Topics = string.Join(",", verbDef.Topics);
            verb.Usage = verbDef.Usage;
            verb.HelpText = verbDef.HelpText;
            _dbProvider.Update("verbs", verb);
            stats.Loaded = 1;
        }
        else
        {
            // Update existing verb with new metadata (especially help metadata)
            existingVerb.Pattern = verbDef.Pattern;
            existingVerb.Code = verbDef.GetCodeString();
            if (!string.IsNullOrEmpty(verbDef.Aliases))
            {
                existingVerb.Aliases = verbDef.Aliases;
            }
            if (!string.IsNullOrEmpty(verbDef.Description))
            {
                existingVerb.Description = verbDef.Description;
            }
            // Always update help metadata (this is the key fix)
            existingVerb.Categories = string.Join(",", verbDef.Categories);
            existingVerb.Topics = string.Join(",", verbDef.Topics);
            existingVerb.Usage = verbDef.Usage;
            existingVerb.HelpText = verbDef.HelpText;
            _dbProvider.Update("verbs", existingVerb);
            stats.Skipped = 1; // Still count as skipped since we didn't create it
        }
        
        return stats;
    }

    /// <summary>
    /// Gets or creates the system object for holding global verbs
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
                _logger.Info($"Created system object with ID: {systemObj.Id}");
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
