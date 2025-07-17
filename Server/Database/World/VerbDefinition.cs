using System.Text.Json.Serialization;

namespace CSMOO.Server.Database.World;

/// <summary>
/// Represents a verb definition loaded from JSON
/// </summary>
public class VerbDefinition
{
    /// <summary>
    /// The name of the verb
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Space-separated aliases for the verb
    /// </summary>
    [JsonPropertyName("aliases")]
    public string Aliases { get; set; } = string.Empty;

    /// <summary>
    /// Pattern for argument matching
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the verb does
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Array of code lines that will be joined with newlines
    /// </summary>
    [JsonPropertyName("code")]
    public string[] Code { get; set; } = Array.Empty<string>();

    /// <summary>
    /// For class verbs, the target class name
    /// </summary>
    [JsonPropertyName("targetClass")]
    public string? TargetClass { get; set; }

    /// <summary>
    /// Get the code as a single string
    /// </summary>
    public string GetCodeString() => string.Join("\n", Code);
}
