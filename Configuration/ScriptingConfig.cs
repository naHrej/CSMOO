namespace CSMOO.Configuration;

/// <summary>
/// Scripting engine configuration
/// </summary>
public class ScriptingConfig
{
    public int MaxCallDepth { get; set; } = 100;
    public int MaxExecutionTimeMs { get; set; } = 5000; // 5 seconds default
}
