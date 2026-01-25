# Script Processor Unification: Verbs and Functions

## Overview

Consolidate verbs and functions into a single script processor. Verbs are simply functions with command hooks - they're not fundamentally different from functions.

## Current Architecture

### Separate Systems

**Verbs:**
- Stored in `Verbs` collection
- Resolved via `IVerbResolver`
- Executed via verb-specific execution path
- Have command patterns for matching
- Attached to objects/classes

**Functions:**
- Stored in `Functions` collection
- Resolved via `IFunctionResolver`
- Executed via function-specific execution path
- Called by name from scripts
- Can be global or object-specific

### Problems

1. **Duplication**: Two separate systems doing essentially the same thing
2. **Complexity**: Two resolvers, two execution paths, two storage systems
3. **Inconsistency**: Different ways to do the same thing
4. **Maintenance**: Changes need to be made in two places

## Unified Architecture

### Core Concept

**Verbs = Functions + Command Hooks**

A verb is just a function that:
- Has command pattern metadata (how to invoke it as a command)
- Can be called from command input
- Otherwise identical to a function

### Unified Script Processor

**Single System:**
- One script storage system
- One resolver
- One execution engine
- Functions can optionally have command hooks (making them verbs)

## Design

### Unified Script Definition

```csharp
public class Script
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string ObjectId { get; set; }  // Object/class this script belongs to
    public string? ClassId { get; set; }   // If on class, the class ID
    
    // Command hooks (makes it a "verb")
    public List<CommandPattern> CommandPatterns { get; set; } = new();
    public bool IsCommand { get; set; }  // Has command hooks
    
    // Function metadata
    public List<ScriptParameter> Parameters { get; set; } = new();
    public string? ReturnType { get; set; }
    
    // Common properties
    public string Description { get; set; }
    public List<string> Aliases { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### Command Pattern

```csharp
public class CommandPattern
{
    public string Pattern { get; set; }  // e.g., "{target}", "{verb} {target}"
    public List<string> Aliases { get; set; } = new();  // e.g., ["look", "l", "examine"]
    public int Priority { get; set; }  // For pattern matching priority
}
```

### Unified Resolver

```csharp
public interface IScriptResolver
{
    // Resolve script by command (for verbs)
    ScriptResolutionResult ResolveByCommand(string command, GameObject context);
    
    // Resolve script by name (for functions)
    Script? ResolveByName(string name, string objectId);
    
    // Resolve script by ID
    Script? ResolveById(string scriptId);
    
    // Get all scripts on an object
    List<Script> GetScriptsOnObject(string objectId);
    
    // Get all scripts with command patterns (verbs)
    List<Script> GetCommandScripts(GameObject context);
}
```

### Unified Execution

```csharp
public interface IScriptProcessor
{
    // Execute script (works for both verbs and functions)
    ScriptExecutionResult ExecuteScript(
        Script script,
        object?[]? parameters = null,
        GameObject? context = null,
        GameObject? caller = null
    );
    
    // Execute by command (finds script via command pattern, then executes)
    ScriptExecutionResult ExecuteCommand(
        string command,
        GameObject context,
        GameObject caller
    );
    
    // Execute by name (finds script by name, then executes)
    ScriptExecutionResult ExecuteFunction(
        string functionName,
        string objectId,
        object?[]? parameters = null,
        GameObject? caller = null
    );
}
```

## Migration Strategy

### Step 1: Create Unified Script Model

1. Create `Script` class (unified model)
2. Create `CommandPattern` class
3. Migrate verb definitions to `Script` with command patterns
4. Migrate function definitions to `Script` (no command patterns)

### Step 2: Create Unified Resolver

1. Create `IScriptResolver` interface
2. Implement `ScriptResolver` that handles both verbs and functions
3. Update to use unified script model

### Step 3: Create Unified Processor

1. Create `IScriptProcessor` interface
2. Implement `ScriptProcessor` that executes scripts
3. Handle both command invocation and function calls

### Step 4: Update Storage

1. Migrate verb storage to unified script storage
2. Migrate function storage to unified script storage
3. Update database schema if needed

### Step 5: Update Callers

1. Update `CommandProcessor` to use `IScriptProcessor.ExecuteCommand()`
2. Update script code to use `IScriptProcessor.ExecuteFunction()`
3. Remove `IVerbResolver` and `IFunctionResolver` usage

### Step 6: Remove Old Systems

1. Remove `IVerbResolver` / `VerbResolver`
2. Remove `IFunctionResolver` / `FunctionResolver`
3. Remove `IVerbManager` / `VerbManager` (replace with `IScriptManager`)
4. Remove `IFunctionManager` / `FunctionManager` (replace with `IScriptManager`)
5. Remove separate verb/function storage

## Benefits

### Simplicity
- One system instead of two
- One resolver instead of two
- One execution path instead of two
- Easier to understand and maintain

### Consistency
- Same execution model for both
- Same storage model for both
- Same resolution model for both
- No confusion about which to use

### Flexibility
- Functions can become verbs by adding command patterns
- Verbs can be called as functions
- Easier to add new features (applies to both)

### Maintainability
- Changes in one place affect both
- Less code to maintain
- Fewer interfaces to understand

## Implementation Details

### Script Storage

**Unified Storage:**
```csharp
// All scripts stored in same collection
public interface IScriptManager
{
    Script CreateScript(Script script);
    Script? GetScript(string scriptId);
    List<Script> GetScriptsOnObject(string objectId);
    List<Script> GetScriptsOnClass(string classId);
    bool UpdateScript(Script script);
    bool DeleteScript(string scriptId);
}
```

**Database Schema:**
```csharp
// Single collection for all scripts
Collection: "scripts"
{
    "id": "script-123",
    "name": "look",
    "code": "...",
    "objectId": "room-1",
    "classId": null,
    "commandPatterns": [
        { "pattern": "{target}", "aliases": ["look", "l", "examine"], "priority": 1 }
    ],
    "isCommand": true,
    "parameters": [],
    "returnType": "string",
    "description": "Look at an object",
    "aliases": ["look", "l"],
    "metadata": {}
}
```

### Command Resolution

**Unified Resolution:**
```csharp
public class ScriptResolver : IScriptResolver
{
    public ScriptResolutionResult ResolveByCommand(string command, GameObject context)
    {
        // Get all scripts with command patterns on context object and its classes
        var candidateScripts = GetCommandScripts(context);
        
        // Match command against patterns
        foreach (var script in candidateScripts)
        {
            foreach (var pattern in script.CommandPatterns)
            {
                if (MatchesPattern(command, pattern))
                {
                    return new ScriptResolutionResult
                    {
                        Script = script,
                        MatchedPattern = pattern,
                        Parameters = ExtractParameters(command, pattern)
                    };
                }
            }
        }
        
        return ScriptResolutionResult.NotFound();
    }
    
    private List<Script> GetCommandScripts(GameObject context)
    {
        // Get scripts on object
        var scripts = GetScriptsOnObject(context.Id);
        
        // Get scripts on object's class hierarchy
        var classScripts = GetScriptsOnClassHierarchy(context.ClassId);
        
        // Combine and filter to only command scripts
        return scripts.Concat(classScripts)
            .Where(s => s.IsCommand)
            .OrderByDescending(s => s.CommandPatterns.Max(p => p.Priority))
            .ToList();
    }
}
```

### Function Resolution

**Unified Resolution:**
```csharp
public Script? ResolveByName(string name, string objectId)
{
    // Get all scripts on object
    var scripts = GetScriptsOnObject(objectId);
    
    // Find by name or alias
    return scripts.FirstOrDefault(s => 
        s.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
        s.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase))
    );
}
```

### Execution

**Unified Execution:**
```csharp
public class ScriptProcessor : IScriptProcessor
{
    public ScriptExecutionResult ExecuteScript(
        Script script,
        object?[]? parameters = null,
        GameObject? context = null,
        GameObject? caller = null)
    {
        // Same execution path for both verbs and functions
        var engine = _scriptEngineFactory.Create();
        var globals = CreateScriptGlobals(context, caller);
        
        // Compile and execute
        var result = engine.Execute(script.Code, globals, parameters);
        
        return new ScriptExecutionResult
        {
            Success = true,
            ReturnValue = result,
            Script = script
        };
    }
    
    public ScriptExecutionResult ExecuteCommand(
        string command,
        GameObject context,
        GameObject caller)
    {
        // Resolve command to script
        var resolution = _scriptResolver.ResolveByCommand(command, context);
        
        if (!resolution.Found)
        {
            return ScriptExecutionResult.NotFound();
        }
        
        // Execute the script
        return ExecuteScript(
            resolution.Script,
            resolution.Parameters,
            context,
            caller
        );
    }
    
    public ScriptExecutionResult ExecuteFunction(
        string functionName,
        string objectId,
        object?[]? parameters = null,
        GameObject? caller = null)
    {
        // Resolve function to script
        var script = _scriptResolver.ResolveByName(functionName, objectId);
        
        if (script == null)
        {
            return ScriptExecutionResult.NotFound();
        }
        
        // Get context object
        var context = _objectManager.GetObject(objectId);
        
        // Execute the script
        return ExecuteScript(script, parameters, context, caller);
    }
}
```

## Backward Compatibility

### During Migration

**Temporary Adapters:**
```csharp
// Temporary adapter during migration
public class VerbResolverAdapter : IVerbResolver
{
    private readonly IScriptResolver _scriptResolver;
    
    public Verb? ResolveVerb(string command, GameObject context)
    {
        var resolution = _scriptResolver.ResolveByCommand(command, context);
        if (!resolution.Found)
            return null;
            
        // Convert Script to Verb (temporary)
        return ConvertScriptToVerb(resolution.Script);
    }
}
```

**Remove After Migration:**
- Once all code uses unified system, remove adapters
- Remove `IVerbResolver` and `IFunctionResolver` interfaces
- Remove old verb/function managers

## Updated Architecture

### Before (Separate Systems)

```
CommandProcessor
├── IVerbResolver → Verb → ScriptEngine
└── IFunctionResolver → Function → ScriptEngine
```

### After (Unified System)

```
CommandProcessor
└── IScriptProcessor
    ├── ExecuteCommand() → Script (with command pattern)
    └── ExecuteFunction() → Script (by name)
        └── ScriptEngine (single execution path)
```

## Code Changes Required

### CommandProcessor

**Before:**
```csharp
var verb = _verbResolver.ResolveVerb(command, context);
if (verb != null)
{
    var result = ExecuteVerb(verb, context);
}
```

**After:**
```csharp
var result = _scriptProcessor.ExecuteCommand(command, context, caller);
```

### Script Code

**Before:**
```csharp
// Call function
var result = CallFunction("functionName", objectId, params);

// Call verb
var result = CallVerb("verbName", objectId, params);
```

**After:**
```csharp
// Both use same method
var result = CallScript("scriptName", objectId, params);
```

## Benefits Summary

1. **Simpler Architecture**: One system instead of two
2. **Less Code**: Remove duplicate implementations
3. **Easier Maintenance**: Changes in one place
4. **More Flexible**: Functions can become verbs easily
5. **Consistent**: Same execution model
6. **Cleaner API**: Single interface instead of two

## Migration Checklist

- [ ] Create unified `Script` model
- [ ] Create `IScriptResolver` interface
- [ ] Implement `ScriptResolver`
- [ ] Create `IScriptProcessor` interface
- [ ] Implement `ScriptProcessor`
- [ ] Migrate verb storage to unified storage
- [ ] Migrate function storage to unified storage
- [ ] Update `CommandProcessor` to use unified system
- [ ] Update script code to use unified system
- [ ] Create temporary adapters (if needed)
- [ ] Remove `IVerbResolver` and implementations
- [ ] Remove `IFunctionResolver` and implementations
- [ ] Remove `IVerbManager` and `IFunctionManager`
- [ ] Remove separate verb/function storage
- [ ] Update all tests
- [ ] Update documentation

## Notes

- This is a significant architectural change
- Should be done during Phase 0 cleanup/refactoring
- Will simplify the codebase significantly
- Makes future features easier to add
- Aligns with "verbs are functions with command hooks" philosophy
