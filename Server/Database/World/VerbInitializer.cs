using System.Linq;
using System.IO;
using System.Text.Json;
using LiteDB;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database.World;

/// <summary>
/// Statistics for verb loading operations
/// </summary>
public struct VerbLoadStats
{
    public int Loaded { get; set; }
    public int Skipped { get; set; }
}

/// <summary>
/// Handles loading and initializing verbs from JSON definitions
/// </summary>
public static class VerbInitializer
{
    private static readonly string VerbsPath = Path.Combine("resources", "verbs");
    private static readonly string SystemVerbsPath = Path.Combine(VerbsPath, "system");
    private static readonly string ClassVerbsPath = Path.Combine(VerbsPath, "classes");

    /// <summary>
    /// Loads and creates all verbs from JSON definitions
    /// </summary>
    public static void LoadAndCreateVerbs()
    {
        Logger.Info("Loading verb definitions from JSON files...");

        var classStats = LoadClassVerbs();
        var systemStats = LoadSystemVerbs();

        var totalLoaded = classStats.Loaded + systemStats.Loaded;
        var totalSkipped = classStats.Skipped + systemStats.Skipped;

        Logger.Info($"Verb definitions loaded successfully - Created: {totalLoaded}, Skipped: {totalSkipped}");
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

        // Reload all verbs from JSON files
        var classStats = LoadClassVerbs();
        var systemStats = LoadSystemVerbs();

        var totalLoaded = classStats.Loaded + systemStats.Loaded;
        var totalSkipped = classStats.Skipped + systemStats.Skipped;
        var countAfterReload = DbProvider.Instance.FindAll<Verb>("verbs").Count();

        Logger.Info($"Verb definitions reloaded successfully - Created: {totalLoaded}, Skipped: {totalSkipped}, Total in DB: {countAfterReload}");
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

        // Reload all verbs from JSON files
        var classStats = LoadClassVerbs();
        var systemStats = LoadSystemVerbs();

        var totalLoaded = classStats.Loaded + systemStats.Loaded;
        var totalSkipped = classStats.Skipped + systemStats.Skipped;
        var countAfterReload = DbProvider.Instance.FindAll<Verb>("verbs").Count();

        Logger.Info($"ALL verb definitions force reloaded - Created: {totalLoaded}, Skipped: {totalSkipped}, Total in DB: {countAfterReload}");
    }

    /// <summary>
    /// Load class-based verb definitions
    /// </summary>
    private static VerbLoadStats LoadClassVerbs()
    {
        var stats = new VerbLoadStats();
        
        if (!Directory.Exists(ClassVerbsPath))
        {
            Logger.Debug($"Class verbs directory not found: {ClassVerbsPath}");
            return stats;
        }

        var jsonFiles = Directory.GetFiles(ClassVerbsPath, "*.json");
        Logger.Debug($"Found {jsonFiles.Length} class verb definition files");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var verbDef = System.Text.Json.JsonSerializer.Deserialize<VerbDefinition>(json);

                if (verbDef == null || string.IsNullOrEmpty(verbDef.Name) || string.IsNullOrEmpty(verbDef.TargetClass))
                {
                    Logger.Warning($"Invalid verb definition in {file}");
                    continue;
                }

                // If code is missing or empty, look for a .cs file with the same base name
                if (verbDef.Code == null || verbDef.Code.Length == 0)
                {
                    var csFile = Path.ChangeExtension(file, ".cs");
                    if (File.Exists(csFile))
                    {
                        verbDef.Code = new[] { File.ReadAllText(csFile) };
                        Logger.Debug($"Loaded code from {csFile} for verb {verbDef.Name}");
                    }
                }

                var verbStats = CreateClassVerb(verbDef);
                stats.Loaded += verbStats.Loaded;
                stats.Skipped += verbStats.Skipped;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading verb definition from {file}: {ex.Message}");
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Load system verb definitions
    /// </summary>
    private static VerbLoadStats LoadSystemVerbs()
    {
        var stats = new VerbLoadStats();
        
        if (!Directory.Exists(SystemVerbsPath))
        {
            Logger.Debug($"System verbs directory not found: {SystemVerbsPath}");
            return stats;
        }

        var systemObjectId = GetOrCreateSystemObject();
        if (systemObjectId == null)
        {
            Logger.Error("Failed to create system object for system verbs");
            return stats;
        }

        var jsonFiles = Directory.GetFiles(SystemVerbsPath, "*.json");
        Logger.Debug($"Found {jsonFiles.Length} system verb definition files");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var verbDef = System.Text.Json.JsonSerializer.Deserialize<VerbDefinition>(json);

                if (verbDef == null || string.IsNullOrEmpty(verbDef.Name))
                {
                    Logger.Warning($"Invalid verb definition in {file}");
                    continue;
                }

                // If code is missing or empty, look for a .cs file with the same base name
                if (verbDef.Code == null || verbDef.Code.Length == 0)
                {
                    var csFile = Path.ChangeExtension(file, ".cs");
                    if (File.Exists(csFile))
                    {
                        verbDef.Code = new[] { File.ReadAllText(csFile) };
                        Logger.Debug($"Loaded code from {csFile} for verb {verbDef.Name}");
                    }
                }

                var verbStats = CreateSystemVerb(systemObjectId, verbDef);
                stats.Loaded += verbStats.Loaded;
                stats.Skipped += verbStats.Skipped;
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
            var verb = Scripting.VerbManager.CreateVerb(
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
            var verb = Scripting.VerbManager.CreateVerb(
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
}
