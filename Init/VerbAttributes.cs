namespace CSMOO.Init;

/// <summary>
/// Attribute to specify aliases for a verb
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class VerbAliasesAttribute : Attribute
{
    public string Aliases { get; }
    
    public VerbAliasesAttribute(string aliases)
    {
        Aliases = aliases;
    }
}

/// <summary>
/// Attribute to specify description for a verb
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class VerbDescriptionAttribute : Attribute
{
    public string Description { get; }
    
    public VerbDescriptionAttribute(string description)
    {
        Description = description;
    }
}

/// <summary>
/// Attribute to specify pattern for a verb
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class VerbPatternAttribute : Attribute
{
    public string Pattern { get; }
    
    public VerbPatternAttribute(string pattern)
    {
        Pattern = pattern;
    }
}
