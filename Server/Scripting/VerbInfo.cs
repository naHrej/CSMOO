using System;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Information about a verb for display purposes
/// </summary>
public class VerbInfo
{
    public string ObjectId { get; set; } = "";
    public string ObjectName { get; set; } = "";
    public string VerbName { get; set; } = "";
    public string? Aliases { get; set; }
    public string? Pattern { get; set; }
    public string? Description { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = "";
    public string[] CodeLines { get; set; } = new string[0];
}
