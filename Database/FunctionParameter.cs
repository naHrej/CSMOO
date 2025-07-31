using LiteDB;

namespace CSMOO.Database.Models;

/// <summary>
/// Represents a parameter for a user-defined function
/// </summary>
public class FunctionParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "object";
    public string DefaultValue { get; set; } = string.Empty;
}



