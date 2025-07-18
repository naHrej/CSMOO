# CSMOO - C# Multi-User Shared Object-Oriented Environment

A modern, LambdaMOO-style multi-user virtual environment with full C# scripting capabilities built on .NET 9.

## 🚀 Features

### **Core Architecture**
- **LambdaMOO-style object system** with class inheritance and instance overrides
- **Real-time multi-user environment** via Telnet server
- **NoSQL database** powered by LiteDB for flexible data storage
- **Full C# scripting engine** using Microsoft.CodeAnalysis.CSharp.Scripting
- **Object-oriented programming** in-game with classes, inheritance, and polymorphism

### **Advanced Verb System**
- **JSON-based verb definitions** - Modern, flexible verb storage and hot-reloading
- **In-game C# programming** - Write and execute C# code directly in the virtual world
- **Verb inheritance** - Objects inherit verbs from their class hierarchy
- **Method overriding** - Instance verbs can override class verbs
- **Inter-verb communication** - Call verbs on other objects as functions
- **Pattern matching** - Sophisticated command parsing and routing
- **Hot reload support** - Update verb definitions without server restart (`@verbreload`)

### **Natural Movement System**
- **Unprefixed movement** - Type `north`, `south`, `n`, `s` directly without `go` prefix
- **Dynamic exit discovery** - Automatically detects available exits from room data
- **Abbreviation support** - Common direction shortcuts (`n` → `north`, `se` → `southeast`)
- **Custom exit names** - Supports non-standard exit names and aliases
- **Intelligent fallback** - Falls back to existing `go` verb for consistency

### **Object Addressing & Navigation**
- **DBREF system** - Easy numeric object references (#1, #2, #3, etc.)
- **Class syntax** - Address objects via `class:ClassName` or `Object.class`
- **Keyword resolution** - Use `this`, `me`, `here`, `system` for context-aware addressing
- **Name-based lookup** - Find objects by their display names

### **Complete C# Language Support**
- **All control structures**: `for`, `foreach`, `while`, `do-while`, `switch`, `if/else`
- **Full type system**: `int`, `string`, `bool`, `List<T>`, `Dictionary<K,V>`, custom types
- **Type inference**: `var` keyword for automatic type detection
- **LINQ support**: Complete query expressions and method syntax
- **Lambda expressions**: `=>` syntax for functional programming
- **Async/await**: Full asynchronous programming support
- **Exception handling**: `try/catch/finally` blocks
- **Generics**: `<T>` type parameters and constraints

### **Interactive Programming Environment**
- **Multi-line code editor** - Write complex programs with proper syntax
- **Real-time compilation** - Immediate feedback on syntax errors
- **Debug output** - Console logging for development and troubleshooting
- **Code persistence** - All verbs saved to database with version tracking
- **Syntax validation** - Full C# compiler error reporting

### **Verb Calling as Functions**
```csharp
// Call verbs on different objects
var result = CallVerb("player", "getName");
var damage = CallVerb("#123", "calculateDamage", weaponType, strength);
var response = CallVerb("class:Monster", "getDefaultBehavior");

// Convenience methods for common targets
This("heal", 50);           // Call verb on current object
Me("levelUp");              // Call verb on player
Here("announceEvent", msg); // Call verb on current room
System("logEvent", data);   // Call verb on system object
Object(42, "activate");     // Call verb on object #42
Class("Weapon", "repair");  // Call verb on Weapon class
```

### **Database & Persistence**
- **Automatic object persistence** - All changes saved immediately
- **Flexible schema** - Add properties dynamically without migrations
- **Relationship tracking** - Parent/child object hierarchies
- **Property inheritance** - Objects inherit default values from classes
- **Transaction safety** - Consistent state even during crashes

### **Networking & Sessions**
- **Telnet protocol** - Connect with [MUjs](https://github.com/naHrej/MUjs) (recommended) or any traditional MUD/telnet client
- **Session management** - Multiple concurrent users
- **Real-time messaging** - Instant communication between players
- **Command processing** - Sophisticated input parsing and routing
- **Error handling** - Graceful recovery from script errors

## 🛠 Technical Stack

- **.NET 9** - Latest C# language features and performance
- **Microsoft.CodeAnalysis.CSharp.Scripting** - Full C# compilation and execution
- **LiteDB** - Embedded NoSQL database with LINQ support
- **System.Net.Sockets** - TCP/Telnet networking
- **Antlr4** - Advanced command parsing (planned feature)

## 🎮 Usage Examples

### Creating Objects and Verbs
```bash
# Connect to server
telnet localhost 1701

# Create a new object
@create Sword

# Program a verb on the object
@program #5:swing
Say("You swing the mighty sword!");
SayToRoom($"{Player.GetProperty("name")} swings a sword!");
return "The sword gleams in the light.";
.

# Test the verb
swing
# Output: You swing the mighty sword!
#         The sword gleams in the light.
```

### Advanced C# Programming in Verbs
```csharp
// Example: Complex combat system verb
@program #1:combat
var weapons = new List<string> { "sword", "axe", "bow" };
var damage = 0;

for (int i = 0; i < Args.Count; i++) {
    var weapon = Args[i].ToLower();
    if (weapons.Contains(weapon)) {
        damage += weapon switch {
            "sword" => 10,
            "axe" => 15,
            "bow" => 8,
            _ => 0
        };
    }
}

var enemies = GetProperty("enemies") as List<string> ?? new List<string>();
foreach (var enemy in enemies.Where(e => !string.IsNullOrEmpty(e))) {
    var enemyObj = FindObjectInRoom(enemy);
    if (enemyObj != null) {
        CallVerb(enemyObj, "takeDamage", damage);
    }
}

return $"Combat complete! Total damage: {damage}";
.
```

### Object-Oriented Programming
```csharp
// Create a Monster class with behavior
@program class:Monster:attack
var targetId = Args.FirstOrDefault() ?? Player.Id;
var strength = GetProperty("strength")?.AsInt32 ?? 10;
var damage = new Random().Next(1, strength + 1);

var target = CallVerb(targetId, "getName");
Say($"The {GetProperty("name")} attacks {target} for {damage} damage!");

// Call the target's damage handling verb
CallVerb(targetId, "takeDamage", damage);
.

// Instance can override class behavior
@program #15:attack
// This specific monster has a special attack
Say("The dragon breathes fire!");
CallVerb("class:Monster", "attack", Args); // Call parent implementation
Say("The flames engulf everything!");
.
```

## 🏗 Architecture

### Object Hierarchy
```
Object (abstract base)
├── Room (locations and environments)
├── Container (objects that hold other objects)
│   ├── Player (user avatars)
│   └── Thing (items and interactive objects)
└── [Custom Classes] (user-defined object types)
```

### Verb Resolution Order
1. **Instance verbs** - Specific to individual objects
2. **Class hierarchy** - Inherited from object's class chain (child to parent)
3. **Pattern matching** - Advanced command parsing
4. **Global verbs** - System-wide commands

### Database Schema
- **GameObjects** - Object instances with properties and relationships
- **ObjectClasses** - Class definitions and inheritance
- **Verbs** - Code attached to objects and classes
- **Players** - User accounts and session data

## 🚦 Getting Started

### Recommended Client
CSMOO is designed to work optimally with **[MUjs](https://github.com/naHrej/MUjs)**, a modern web-based MUD client that provides:
- Rich text formatting and color support
- Enhanced user interface elements
- Improved command input and history
- Better integration with CSMOO's advanced features

However, CSMOO is fully compatible with any traditional MUD client or standard telnet client.

### Prerequisites
- .NET 9 SDK
- Windows/Linux/macOS
- Telnet client (built into most systems)

### Building and Running
```bash
# Clone the repository
git clone [your-repo-url]
cd CSMOO

# Build the project
dotnet build

# Run the server
dotnet run

# Connect from another terminal
telnet localhost 1701
```

### First Steps
```bash
# Login as admin
login admin password

# Explore the world
look
exits

# Try natural movement (new!)
north     # or just 'n'
south     # or just 's'

# Create your first object
@create MyFirstObject

# Program it with C# code (or use JSON files)
@program #3:greet
Say($"Hello, {Player.GetProperty("name")}!");
return "Welcome to CSMOO!";
.

# Test your creation
greet

# Hot reload verb definitions (new!)
@verbreload
```

## 🎯 Use Cases

- **Educational Programming** - Learn C# in an interactive environment
- **Game Development** - Create MUDs, text adventures, and virtual worlds
- **Collaborative Coding** - Multi-user programming environments
- **Rapid Prototyping** - Test ideas with immediate feedback
- **Virtual Workspaces** - Shared development environments

## 🔮 Planned Features

- [ ] Web interface for modern browser access
- [ ] Advanced ANTLR4 command parsing
- [ ] Plugin system for external modules
- [ ] RESTful API for external integrations
- [ ] WebSocket support for real-time web clients
- [ ] Advanced debugging tools
- [ ] Code repository and version control
- [ ] Lua scripting support alongside C#

## 📚 Documentation

- **PROG_GUIDE.md** - Comprehensive verb programming guide
- **Examples/** directory for:
  - Object system examples  
  - Advanced verb programming patterns
  - Inheritance and polymorphism demos
  - Inter-object communication samples
  - JSON verb definition examples

## 🤝 Contributing

This is a showcase project demonstrating modern MUD development with C# scripting. Feel free to explore the code and adapt it for your own projects!

## 📄 License

[Your chosen license here]

---

**CSMOO** - Where classic MUD concepts meet modern C# development. Build, script, and explore in a fully programmable virtual world!
