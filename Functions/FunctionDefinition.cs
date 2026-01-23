using System.Text.Json.Serialization;
using CSMOO.Object;

namespace CSMOO.Functions;

/// <summary>
/// Represents a function definition loaded from C# class definitions
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

    [JsonPropertyName("accessors")]
    public List<Keyword> Accessors { get; set; } = new List<Keyword> { Keyword.Public };

    /// <summary>
    /// Help categories this function belongs to (from XML &lt;category&gt; tags)
    /// </summary>
    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new List<string>();

    /// <summary>
    /// Help topics this function is associated with (from XML &lt;topic&gt; tags)
    /// </summary>
    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = new List<string>();

    /// <summary>
    /// Usage example for this function (from XML &lt;usage&gt; tag)
    /// </summary>
    [JsonPropertyName("usage")]
    public string? Usage { get; set; }

    /// <summary>
    /// Detailed help text for this function (from XML &lt;help&gt; tag)
    /// </summary>
    [JsonPropertyName("helpText")]
    public string? HelpText { get; set; }

    /// <summary>
    /// Get the code as a single string
    /// </summary>
    public string GetCodeString() => string.Join("\n", Code);
}



