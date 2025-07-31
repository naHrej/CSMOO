using System.Text.Json.Serialization;

namespace CSMOO.Database;

/// <summary>
/// Represents a function definition loaded from JSON
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// The name of the function
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter types in order
    /// </summary>
    [JsonPropertyName("parameters")]
    public string[] Parameters { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Parameter names for documentation
    /// </summary>
    [JsonPropertyName("parameterNames")]
    public string[] ParameterNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Return type of the function
    /// </summary>
    [JsonPropertyName("returnType")]
    public string ReturnType { get; set; } = "void";

    /// <summary>
    /// Description of what the function does
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Array of code lines that will be joined with newlines
    /// </summary>
    [JsonPropertyName("code")]
    public string[] Code { get; set; } = Array.Empty<string>();

    /// <summary>
    /// For class functions, the target class name
    /// </summary>
    [JsonPropertyName("targetClass")]
    public string? TargetClass { get; set; }

    /// <summary>
    /// Get the code as a single string
    /// </summary>
    public string GetCodeString() => string.Join("\n", Code);
}



