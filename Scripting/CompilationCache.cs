using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CSMOO.Scripting;

/// <summary>
/// Thread-safe in-memory cache for compiled scripts
/// </summary>
internal class CachedScript
{
    public Script<object> CompiledScript { get; set; } = null!;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Thread-safe in-memory implementation of ICompilationCache
/// </summary>
public class CompilationCache : ICompilationCache
{
    private readonly ConcurrentDictionary<string, CachedScript> _verbCache = new();
    private readonly ConcurrentDictionary<string, CachedScript> _functionCache = new();

    public Script<object>? GetVerb(string verbId)
    {
        if (_verbCache.TryGetValue(verbId, out var cached))
        {
            return cached.CompiledScript;
        }
        return null;
    }

    public Script<object>? GetFunction(string functionId)
    {
        if (_functionCache.TryGetValue(functionId, out var cached))
        {
            return cached.CompiledScript;
        }
        return null;
    }

    public string? GetVerbCodeHash(string verbId)
    {
        if (_verbCache.TryGetValue(verbId, out var cached))
        {
            return cached.CodeHash;
        }
        return null;
    }

    public string? GetFunctionCodeHash(string functionId)
    {
        if (_functionCache.TryGetValue(functionId, out var cached))
        {
            return cached.CodeHash;
        }
        return null;
    }

    public void SetVerb(string verbId, Script<object> script, string codeHash)
    {
        _verbCache.AddOrUpdate(verbId,
            new CachedScript
            {
                CompiledScript = script,
                CodeHash = codeHash,
                CachedAt = DateTime.UtcNow
            },
            (key, existing) => new CachedScript
            {
                CompiledScript = script,
                CodeHash = codeHash,
                CachedAt = DateTime.UtcNow
            });
    }

    public void SetFunction(string functionId, Script<object> script, string codeHash)
    {
        _functionCache.AddOrUpdate(functionId,
            new CachedScript
            {
                CompiledScript = script,
                CodeHash = codeHash,
                CachedAt = DateTime.UtcNow
            },
            (key, existing) => new CachedScript
            {
                CompiledScript = script,
                CodeHash = codeHash,
                CachedAt = DateTime.UtcNow
            });
    }

    public void InvalidateVerb(string verbId)
    {
        _verbCache.TryRemove(verbId, out _);
    }

    public void InvalidateFunction(string functionId)
    {
        _functionCache.TryRemove(functionId, out _);
    }

    public void Clear()
    {
        _verbCache.Clear();
        _functionCache.Clear();
    }

    public int GetCacheCount()
    {
        return _verbCache.Count + _functionCache.Count;
    }
}
