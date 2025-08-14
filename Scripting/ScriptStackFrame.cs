namespace CSMOO.Scripting;

/// <summary>
/// Represents a single frame in the script call stack
/// </summary>
public class ScriptStackFrame
{
    public string Type { get; set; } = string.Empty; // "verb" or "function"
    public string Name { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public int LineNumber { get; set; } = 0;
    public string SourceCode { get; set; } = string.Empty;
    public string ErrorContext { get; set; } = string.Empty; // The line that caused the error

    public override string ToString()
    {
        var objectInfo = !string.IsNullOrEmpty(ObjectName) ? $"{ObjectName}({ObjectId})" : ObjectId;
        var lineInfo = LineNumber > 0 ? $" at line {LineNumber}" : "";
        return $"  at {Type} {Name} in {objectInfo}{lineInfo}";
    }
}
