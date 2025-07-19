# Lambda Expressions in CSMOO Scripting

## Overview
Lambda expressions are now fully supported in the CSMOO scripting engine! This enables more concise and powerful code using functional programming concepts.

**Important:** Due to C# dynamic dispatch limitations, we provide special extension methods for lambda operations with dynamic objects.

## Basic Lambda Syntax

### Working with Dynamic Objects (Recommended Approach)
```csharp
// Use extension methods for dynamic objects - these work with lambdas!
var visibleObjects = GetObjectsInLocation(roomId).WhereObjects(obj => obj.visible == true);
var weaponNames = GetObjectsInLocation(roomId).WhereObjects(obj => obj.type == "weapon")
                                               .SelectObjects(obj => obj.name);

// Count objects matching condition
var weaponCount = GetObjectsInLocation(roomId).CountObjects(obj => obj.type == "weapon");

// Check if any objects match
var hasWeapons = GetObjectsInLocation(roomId).AnyObjects(obj => obj.type == "weapon");
```

### Working with Regular Collections
```csharp
// Find all online players (works normally)
var onlinePlayers = GetAllPlayers().Where(p => p.IsOnline);

// Find objects by property using helper methods
var weapons = FindObjects(obj => GetProperty(obj.Id, "type") == "weapon");
```

## Extension Methods for Dynamic Objects

### Filtering and Selection
```csharp
// Filter objects
var redItems = GetObjectsInLocation(roomId).WhereObjects(obj => obj.color == "red");

// Transform to other types
var names = GetObjectsInLocation(roomId).SelectObjects(obj => obj.name).ToList();

// Find first matching
var sword = GetObjectsInLocation(roomId).FirstObjects(obj => obj.name == "sword");
```

### Aggregation and Counting
```csharp
// Count matching objects
var weaponCount = GetObjectsInLocation(roomId).CountObjects(obj => obj.type == "weapon");

// Check if any match
var hasGold = GetObjectsInLocation(roomId).AnyObjects(obj => obj.name.Contains("gold"));
```

### Ordering and Grouping
```csharp
// Order objects
var sortedByName = GetObjectsInLocation(roomId).OrderByObjects(obj => obj.name);
var sortedByValue = GetObjectsInLocation(roomId).OrderByDescendingObjects(obj => obj.value);

// Group objects
var groupedByType = GetObjectsInLocation(roomId).GroupByObjects(obj => obj.type);
```

### Actions on Objects
```csharp
// Execute action on each matching object
GetObjectsInLocation(roomId).WhereObjects(obj => obj.broken == true)
                            .ForEachObjects(obj => obj.broken = false);

// Take/Skip for pagination
var firstFiveWeapons = GetObjectsInLocation(roomId).WhereObjects(obj => obj.type == "weapon")
                                                   .TakeObjects(5);
```

## Player Operations with Lambdas

### Player Filtering and Actions
```csharp
// Filter players and execute actions
GetAllPlayers().WherePlayers(p => p.IsOnline && IsAdmin(p))
               .ForEachPlayers(p => Notify(p, "Admin message"));

// Complex player operations
var newPlayers = GetAllPlayers().WherePlayers(p => (DateTime.Now - p.CreatedAt).TotalDays < 7);
```

## Lambda-Friendly Helper Methods

### Player Operations
```csharp
// Execute action for matching players
ForEachPlayer(p => IsAdmin(p), p => notify(p, "Admin message"));

// Count players matching condition
var adminCount = CountPlayers(p => IsAdmin(p));

// Check if any player matches
var hasOnlineAdmin = AnyPlayer(p => p.IsOnline && IsAdmin(p));
```

### Object Operations
```csharp
// Execute action for matching objects
ForEachObject(obj => GetProperty(obj.Id, "broken") == "true", 
              obj => SetProperty(obj.Id, "broken", "false"));

// Find objects with complex conditions
var expensiveItems = FindObjects(obj => {
    var price = GetProperty(obj.Id, "price");
    return int.TryParse(price, out int p) && p > 1000;
});
```

### Location-Based Operations
```csharp
// Find specific objects in a room
var hiddenItems = FindObjectsInLocation(roomId, obj => 
    !GetBoolProperty(obj.GameObject.Id, "visible", true));
```

## Common Patterns

### Chaining Operations
```csharp
// Chain multiple operations
var result = GetAllPlayers()
    .Where(p => p.IsOnline)
    .Select(p => p.name)
    .OrderBy(name => name)
    .Take(5)
    .ToList();
```

### Conditional Logic in Lambdas
```csharp
// Complex filtering logic
var specialItems = FindObjects(obj => {
    var type = GetProperty(obj.Id, "type");
    var rarity = GetProperty(obj.Id, "rarity");
    return (type == "weapon" || type == "armor") && 
           (rarity == "rare" || rarity == "legendary");
});
```

### Grouping and Aggregation
```csharp
// Group players by location
var playersByRoom = GetAllPlayers()
    .Where(p => p.IsOnline)
    .GroupBy(p => p.location)
    .ToDictionary(g => g.Key, g => g.ToList());
```

## Examples for Different Use Cases

### Admin Commands
```csharp
// Broadcast to all online admins
ForEachPlayer(p => p.IsOnline && IsAdmin(p), 
              p => notify(p, "Admin broadcast message"));
```

### Item Management
```csharp
// Find all weapons that need repair
var brokenWeapons = FindObjects(obj =>
    GetProperty(obj.Id, "type") == "weapon" &&
    GetBoolProperty(obj.Id, "broken", false));

// Repair all broken items
ForEachObject(obj => GetBoolProperty(obj.Id, "broken", false),
              obj => SetProperty(obj.Id, "broken", "false"));
```

### Player Statistics
```csharp
// Generate player statistics
var stats = new {
    TotalPlayers = GetAllPlayers().Count(),
    OnlineCount = CountPlayers(p => p.IsOnline),
    AdminCount = CountPlayers(p => IsAdmin(p)),
    NewPlayers = CountPlayers(p => (DateTime.Now - p.CreatedAt).TotalDays < 7)
};
```

## Performance Tips

1. **Use ToList() sparingly** - Only call it when you need the results immediately
2. **Filter early** - Put Where() clauses before Select() when possible
3. **Avoid complex lambdas in loops** - Extract complex logic to separate methods
4. **Cache results** - Store frequently used filtered lists in variables

## Testing Lambda Support

Use these verbs to test that lambda expressions work correctly:

### Quick Test
```
lambda-demo
```

### Comprehensive Test
```
lambda-test
```

This will run comprehensive tests of lambda functionality and show examples of usage.

## Summary of Lambda Support

✅ **What Works:**
- Standard LINQ operations on regular collections (Players, Objects, etc.)
- Lambda expressions with strongly-typed collections
- Extension methods for dynamic objects that solve the dynamic dispatch issue
- Method chaining with lambdas
- Complex filtering, grouping, and transformations

✅ **Key Features:**
- `WhereObjects()`, `SelectObjects()`, `CountObjects()` etc. for dynamic objects
- `WherePlayers()`, `ForEachPlayers()` for player collections
- Full LINQ support for regular collections
- Exception handling in lambda operations
- Performance optimized implementations

⚠️ **Important Notes:**
- Use extension methods (e.g., `WhereObjects()`) instead of direct LINQ on dynamic objects
- This solves the CS1977 error with dynamic dispatch
- Regular collections work with standard LINQ methods
- All extension methods are automatically imported in scripts
