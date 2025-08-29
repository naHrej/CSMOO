using LiteDB;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Init;

namespace CSMOO.Verbs;

/// <summary>
/// Statistics for verb loading operations
/// </summary>
public struct VerbLoadStats
{
    public int Loaded { get; set; }
    public int Skipped { get; set; }
}

/// <summary>
/// Handles loading and initializing verbs from C# class definitions
/// </summary>
public static class VerbInitializer
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
    /// Loads and creates all verbs from C# class definitions
    /// </summary>
    public static void LoadAndCreateVerbs()
    {
        Logger.Info("Loading verb definitions from C# files...");

        var stats = LoadVerbs();

        Logger.Info($"Verb definitions loaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}");
    }

    /// <summary>
    /// Hot reload all verb definitions (removes old, loads new)
    /// Only removes verbs that were created from resources folder (CreatedBy == "system")
    /// </summary>
    public static void ReloadVerbs()
    {
        Logger.Info("Hot reloading verb definitions...");

        // Only clear verb definitions that were loaded from resources
        var allVerbs = DbProvider.Instance.FindAll<Verb>("verbs").ToList();
        var systemVerbs = allVerbs.Where(v => v.CreatedBy == "system").ToList();
        var inGameVerbs = allVerbs.Where(v => v.CreatedBy != "system").ToList();
        
        var countBefore = allVerbs.Count;
        Logger.Debug($"Found {countBefore} total verbs before deletion ({systemVerbs.Count} system, {inGameVerbs.Count} in-game)");
        
        // Only delete system/resource verbs
        foreach (var v in systemVerbs) 
        {
            DbProvider.Instance.Delete<Verb>("verbs", v.Id);
        }
        
        var countAfterDelete = DbProvider.Instance.FindAll<Verb>("verbs").Count();
        Logger.Debug($"Cleared {systemVerbs.Count} system verb definitions - {countAfterDelete} verbs remaining (preserving {inGameVerbs.Count} in-game verbs)");

        // Reload all verbs from C# files
        var stats = LoadVerbs();

        var countAfterReload = DbProvider.Instance.FindAll<Verb>("verbs").Count();

        Logger.Info($"Verb definitions reloaded successfully - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Force reload ALL verb definitions (removes all verbs, loads new)
    /// Use with caution - this will delete in-game created verbs too!
    /// </summary>
    public static void ForceReloadAllVerbs()
    {
        Logger.Warning("FORCE reloading ALL verb definitions (including in-game verbs)...");

        // Clear ALL verb definitions from database
        var allVerbs = DbProvider.Instance.FindAll<Verb>("verbs").ToList();
        var countBefore = allVerbs.Count;
        Logger.Debug($"Found {countBefore} verbs before deletion");
        foreach (var v in allVerbs) DbProvider.Instance.Delete<Verb>("verbs", v.Id);
        var countAfterDelete = DbProvider.Instance.FindAll<Verb>("verbs").Count();
        Logger.Debug($"Cleared ALL verb definitions - {countAfterDelete} verbs remaining");

        // Reload all verbs from C# files
        var stats = LoadVerbs();

        var countAfterReload = DbProvider.Instance.FindAll<Verb>("verbs").Count();

        Logger.Info($"ALL verb definitions force reloaded - Created: {stats.Loaded}, Skipped: {stats.Skipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Load verb definitions from all C# files in Resources directory
    /// </summary>
    private static VerbLoadStats LoadVerbs()
    {
        var stats = new VerbLoadStats();
        
        if (!Directory.Exists(ResourcesPath))
        {
            Logger.Debug($"Resources directory not found: {ResourcesPath}");
            return stats;
        }

        // Get all .cs files recursively from Resources directory
        var csFiles = Directory.GetFiles(ResourcesPath, "*.cs", SearchOption.AllDirectories);
        Logger.Debug($"Found {csFiles.Length} C# files in Resources directory");

        foreach (var file in csFiles)
        {
            try
            {
                var verbDefs = CodeDefinitionParser.ParseVerbs(file);
                
                foreach (var verbDef in verbDefs)
                {
                    if (string.IsNullOrEmpty(verbDef.Name))
                    {
                        Logger.Warning($"Invalid verb definition in {file}");
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
                            Logger.Error("Failed to create system object for system verbs");
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
                Logger.Error($"Error loading verb definition from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Create a verb on a class from a definition
    /// </summary>
    private static VerbLoadStats CreateClassVerb(VerbDefinition verbDef)
    {
        var stats = new VerbLoadStats();
        
        var targetClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == verbDef.TargetClass);
        if (targetClass == null)
        {
            Logger.Warning($"Target class '{verbDef.TargetClass}' not found for verb '{verbDef.Name}'");
            return stats;
        }

        var existingVerbs = DbProvider.Instance.FindAll<Verb>("verbs").ToList();
        // Only create if it doesn't already exist
        if (!existingVerbs.Any(v => v.ObjectId == targetClass.Id && v.Name == verbDef.Name))
        {
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
                DbProvider.Instance.Update("verbs", verb);
            }
            // Set description if provided
            if (!string.IsNullOrEmpty(verbDef.Description))
            {
                verb.Description = verbDef.Description;
                DbProvider.Instance.Update("verbs", verb);
            }
            Logger.Debug($"Created class verb '{verbDef.Name}' on {verbDef.TargetClass}");
            stats.Loaded = 1;
        }
        else
        {
            Logger.Debug($"Class verb '{verbDef.Name}' on {verbDef.TargetClass} already exists, skipping");
            stats.Skipped = 1;
        }
        
        return stats;
    }

    /// <summary>
    /// Create a system verb from a definition
    /// </summary>
    private static VerbLoadStats CreateSystemVerb(string systemObjectId, VerbDefinition verbDef)
    {
        var stats = new VerbLoadStats();
        var existingVerbs = DbProvider.Instance.FindAll<Verb>("verbs").ToList();
        // Only create if it doesn't already exist
        if (!existingVerbs.Any(v => v.ObjectId == systemObjectId && v.Name == verbDef.Name))
        {
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
                DbProvider.Instance.Update("verbs", verb);
            }
            // Set description if provided
            if (!string.IsNullOrEmpty(verbDef.Description))
            {
                verb.Description = verbDef.Description;
                DbProvider.Instance.Update("verbs", verb);
            }
            Logger.Debug($"Created system verb '{verbDef.Name}'");
            stats.Loaded = 1;
        }
        else
        {
            Logger.Debug($"System verb '{verbDef.Name}' already exists, skipping");
            stats.Skipped = 1;
        }
        
        return stats;
    }

    /// <summary>
    /// Gets or creates the system object for holding global verbs
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



