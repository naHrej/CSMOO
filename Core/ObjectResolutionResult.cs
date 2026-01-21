using System.Collections.Generic;
using CSMOO.Object;

namespace CSMOO.Core;

/// <summary>
/// Result of resolving a textual object reference within a player's context.
/// </summary>
public class ObjectResolutionResult
{
    public ObjectResolutionResult(string query, GameObject? match, bool ambiguous, List<GameObject> matches)
    {
        Query = query;
        Match = match;
        Ambiguous = ambiguous;
        Matches = matches;
    }

    public string Query { get; }
    public GameObject? Match { get; }
    public bool Ambiguous { get; }
    public List<GameObject> Matches { get; }
}

