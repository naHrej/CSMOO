using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CSMOO.Scripting;

/// <summary>
/// Interface for caching compiled scripts
/// </summary>
public interface ICompilationCache
{
    /// <summary>
    /// Gets a cached compiled verb script
    /// </summary>
    /// <param name="verbId">Verb ID</param>
    /// <returns>Cached compiled script, or null if not cached or hash mismatch</returns>
    Script<object>? GetVerb(string verbId);
    
    /// <summary>
    /// Gets a cached compiled function script
    /// </summary>
    /// <param name="functionId">Function ID</param>
    /// <returns>Cached compiled script, or null if not cached or hash mismatch</returns>
    Script<object>? GetFunction(string functionId);
    
    /// <summary>
    /// Gets the code hash for a verb
    /// </summary>
    /// <param name="verbId">Verb ID</param>
    /// <returns>Code hash, or null if not cached</returns>
    string? GetVerbCodeHash(string verbId);
    
    /// <summary>
    /// Gets the code hash for a function
    /// </summary>
    /// <param name="functionId">Function ID</param>
    /// <returns>Code hash, or null if not cached</returns>
    string? GetFunctionCodeHash(string functionId);
    
    /// <summary>
    /// Caches a compiled verb script
    /// </summary>
    /// <param name="verbId">Verb ID</param>
    /// <param name="script">Compiled script</param>
    /// <param name="codeHash">Code hash for invalidation</param>
    void SetVerb(string verbId, Script<object> script, string codeHash);
    
    /// <summary>
    /// Caches a compiled function script
    /// </summary>
    /// <param name="functionId">Function ID</param>
    /// <param name="script">Compiled script</param>
    /// <param name="codeHash">Code hash for invalidation</param>
    void SetFunction(string functionId, Script<object> script, string codeHash);
    
    /// <summary>
    /// Invalidates cached verb script
    /// </summary>
    /// <param name="verbId">Verb ID</param>
    void InvalidateVerb(string verbId);
    
    /// <summary>
    /// Invalidates cached function script
    /// </summary>
    /// <param name="functionId">Function ID</param>
    void InvalidateFunction(string functionId);
    
    /// <summary>
    /// Clears all cached scripts
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Gets the total number of cached scripts
    /// </summary>
    /// <returns>Count of cached scripts</returns>
    int GetCacheCount();
}
