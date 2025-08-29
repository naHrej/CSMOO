using System.Text.Json.Serialization;
using CSMOO.Scripting;

namespace CSMOO.Object;

/// <summary>
/// Represents a property definition loaded from JSON
/// </summary>
public class PropertyDefinition
{
    /// <summary>
    /// Name of the property
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Value of the property (can be string, number, boolean, or array)
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// Type hint for the property value (string, int, bool, array, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>
    /// Target class name (for class-based properties)
    /// </summary>
    [JsonPropertyName("targetClass")]
    public string? TargetClass { get; set; }

    /// <summary>
    /// Target object ID or name (for instance-based properties)
    /// </summary>
    [JsonPropertyName("targetObject")]
    public string? TargetObject { get; set; }

    /// <summary>
    /// Description of what this property does
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this property should be overwritten if it already exists
    /// </summary>
    [JsonPropertyName("overwrite")]
    public bool Overwrite { get; set; } = false;

    /// <summary>
    /// Array of string values (for multi-line properties)
    /// </summary>
    [JsonPropertyName("lines")]
    public string[]? Lines { get; set; }

    /// <summary>
    /// Filename to load content from (relative to property definition file)
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    public List<Keyword>? Accessors { get; set; } = new List<Keyword> { Keyword.Public };

    /// <summary>
    /// Gets the property value as the appropriate type
    /// </summary>
    public object? GetTypedValue(string? baseDirectory = null)
    {
        // If filename is specified, load content from file
        if (!string.IsNullOrEmpty(Filename))
        {
            try
            {
                string filePath;
                if (Path.IsPathRooted(Filename))
                {
                    filePath = Filename;
                }
                else if (!string.IsNullOrEmpty(baseDirectory))
                {
                    filePath = Path.Combine(baseDirectory, Filename);
                }
                else
                {
                    filePath = Filename;
                }

                if (File.Exists(filePath))
                {
                    var fileLines = File.ReadAllLines(filePath);
                    return fileLines; // Return as string array
                }
                else
                {
                    throw new FileNotFoundException($"Property file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load property file '{Filename}': {ex.Message}", ex);
            }
        }

        // If lines are specified, use them
        if (Lines != null && Lines.Length > 0)
        {
            return Lines; // Return as string array
        }

        if (Value == null)
            return null;

        return Type.ToLower() switch
        {
            "int" or "integer" => Convert.ToInt32(Value),
            "bool" or "boolean" => Convert.ToBoolean(Value),
            "float" or "single" => Convert.ToSingle(Value),
            "double" => Convert.ToDouble(Value),
            "decimal" => Convert.ToDecimal(Value),
            "array" or "list" => Value, // Keep as-is, will be handled by caller
            _ => Value?.ToString() // Default to string
        };
    }
}
