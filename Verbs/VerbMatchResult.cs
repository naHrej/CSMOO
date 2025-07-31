using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LiteDB;
using CSMOO.Database;
using CSMOO.Logging;

namespace CSMOO.Verbs;

/// <summary>
/// Result of verb matching that includes extracted pattern variables
/// </summary>
public class VerbMatchResult
{
    public Verb Verb { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    
    public VerbMatchResult(Verb verb, Dictionary<string, string>? variables = null)
    {
        Verb = verb;
        Variables = variables ?? new Dictionary<string, string>();
    }
}


