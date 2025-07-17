# HTML Generation API for CSMOO Verbs

The CSMOO scripting engine now includes **HtmlAgilityPack** with a simplified `Html.*` API for easily creating styled HTML content in your verb scripts.

## Basic Usage

### Creating Elements

```csharp
// Basic elements
var div = Html.Div("Hello World");
var span = Html.Span("Highlighted text");
var paragraph = Html.P("This is a paragraph");

// Empty elements (add content later)
var container = Html.Div();
```

### Adding Styles

```csharp
// Using the fluent WithStyle method
var styledDiv = Html.Div("Styled content")
    .WithStyle("color: #00ff88; background: #003322; padding: 10px;");

// Using the Style helper class
var style = Html.Style.Build(
    ("color", "#00ff88"),
    ("font-size", "1.2rem"),
    ("margin", "10px")
);
var element = Html.Div("Content").WithStyle(style);

// Using LESS-style CSS with variables
var variables = new Dictionary<string, string>
{
    ["primary-color"] = "#00ff88",
    ["dark-bg"] = "#003322",
    ["spacing"] = "10px"
};

var lessStyledDiv = Html.Div("LESS styled content")
    .WithLessStyle("color: @primary-color; background: @dark-bg; padding: @spacing;", variables);
```

### LESS CSS Support

```csharp
// Create LESS CSS with variables and builder pattern
var cssBuilder = Html.Style.Less()
    .Variable("primary", "#00ff88")
    .Variable("secondary", "#ff4444")
    .Variable("spacing", "15px")
    .Rule(".container", 
        ("color", "@primary"),
        ("padding", "@spacing"),
        ("border", "1px solid @primary"))
    .NestedRule(".container", ".button",
        ("background", "@secondary"),
        ("color", "white"),
        ("padding", "5px @spacing"));

var processedCSS = cssBuilder.Build();
var element = Html.Div("Content").WithStyle(processedCSS);

// Direct LESS processing
var lessCSS = @"
    @primary: #00ff88;
    @spacing: 10px;
    
    color: @primary;
    padding: @spacing;
    border: 1px solid @primary;
";

var processedStyle = Html.Style.ProcessLess(lessCSS);
var directElement = Html.Div("Direct LESS").WithStyle(processedStyle);
```

### Cyberpunk/Gaming Theme Helpers

```csharp
// Pre-styled cyberpunk container
var container = Html.CyberpunkContainer("Welcome!");

// Glowing title effect
var title = Html.GlowTitle("CSMOO", "#00ff88");

// Highlighted command text
var command = Html.Command("login username password");
```

### Building Complex HTML

```csharp
// Create a complete structure
var main = Html.Div()
    .WithStyle("display: flex; flex-direction: column; align-items: center;");

var header = Html.GlowTitle("Game Server");
var content = Html.Div("Server is running...").WithStyle("margin: 20px;");
var footer = Html.P("© 2025 CSMOO").WithStyle("font-size: 0.8rem; color: #888;");

// Assemble the structure
main.AppendChildren(header, content, footer);

// Convert to single line for MUD client compatibility
return main.ToSingleLine();
```

### Common Patterns for MUD Output

#### Status Display
```csharp
var statusBox = Html.CyberpunkContainer()
    .WithStyle("margin: 10px; padding: 15px;");

var health = Html.P($"Health: {player.Health}/100")
    .WithStyle("color: #ff4444; font-weight: bold;");
    
var mana = Html.P($"Mana: {player.Mana}/100")
    .WithStyle("color: #4444ff; font-weight: bold;");

statusBox.AppendChildren(health, mana);
return statusBox.ToSingleLine();
```

#### Status Display with LESS CSS
```csharp
// Define theme variables
var theme = new Dictionary<string, string>
{
    ["health-color"] = "#ff4444",
    ["mana-color"] = "#4444ff",
    ["text-style"] = "font-weight: bold; font-size: 1.1rem;"
};

var statusBox = Html.CyberpunkContainer()
    .WithStyle("margin: 10px; padding: 15px;");

var health = Html.P($"Health: {player.Health}/100")
    .WithLessStyle("color: @health-color; @text-style", theme);
    
var mana = Html.P($"Mana: {player.Mana}/100")
    .WithLessStyle("color: @mana-color; @text-style", theme);

statusBox.AppendChildren(health, mana);
return statusBox.ToSingleLine();
```

#### Command Help
```csharp
var helpContainer = Html.Div().WithStyle("font-family: monospace;");

var title = Html.Heading(2, "Available Commands").WithStyle("color: #00ff88;");
var loginCmd = Html.P().AppendChild(Html.Span("To login: ")).AppendChild(Html.Command("login <username> <password>"));
var helpCmd = Html.P().AppendChild(Html.Span("For help: ")).AppendChild(Html.Command("help <topic>"));

helpContainer.AppendChildren(title, loginCmd, helpCmd);
return helpContainer.ToSingleLine();
```

#### Error Messages
```csharp
var errorBox = Html.Div("Error: Command not found!")
    .WithStyle("color: #ff4444; background: #440000; border: 1px solid #ff4444; padding: 10px; border-radius: 5px;");

return errorBox.ToSingleLine();
```

## Available Methods

### Core Elements
- `Html.Element(tagName, text)` - Create any HTML element
- `Html.Div(text)` - Create a div element
- `Html.Span(text)` - Create a span element  
- `Html.P(text)` - Create a paragraph element
- `Html.Heading(level, text)` - Create h1-h6 elements
- `Html.Link(href, text)` - Create anchor links
- `Html.Image(src, alt)` - Create images

### Themed Elements
- `Html.CyberpunkContainer(text)` - Pre-styled cyberpunk theme container
- `Html.Command(text)` - Highlighted command text
- `Html.GlowTitle(text, color)` - Title with glow effect

### Fluent Extensions
- `.WithStyle(cssString)` - Add CSS styles
- `.WithLessStyle(lessCSS, variables)` - Add LESS-style CSS with variables
- `.WithClass(className)` - Add CSS classes
- `.WithAttribute(name, value)` - Add any attribute
- `.AppendChild(childElement)` - Add a single child
- `.AppendChildren(child1, child2, ...)` - Add multiple children
- `.ToSingleLine()` - Convert to single line (required for MUD clients)

### Style Helper
```csharp
// Regular CSS builder
Html.Style.Build(
    ("property", "value"),
    ("property2", "value2")
)

// LESS CSS processor
Html.Style.ProcessLess(lessCSS, variables)

// LESS CSS builder
Html.Style.Less()
    .Variable("name", "value")
    .Rule("selector", "properties")
    .NestedRule("parent", "child", "properties")
    .Build()
```

### LESS CSS Features

The LESS CSS processor supports:

1. **Variables**: Use `@variableName` syntax
2. **Simple Nesting**: One level of nesting for MUD compatibility
3. **Builder Pattern**: Fluent API for creating complex styles
4. **Variable Substitution**: Dictionary-based variable replacement

#### LESS CSS Examples

```csharp
// Variables example
var vars = new Dictionary<string, string> { ["theme"] = "#00ff88" };
Html.Style.ProcessLess("color: @theme; border: 1px solid @theme;", vars);

// Builder pattern example
var css = Html.Style.Less()
    .Variable("glow", "#00ff88")
    .Variable("dark", "#001122")
    .Rule(".cyberpunk", ("color", "@glow"), ("background", "@dark"))
    .Build();

// Simple nesting (converts to standard CSS)
var nestedCSS = Html.Style.ProcessLess(@"
    .container { 
        padding: 10px; 
        .button { 
            background: #00ff88; 
            color: white; 
        } 
    }
");
// Results in: .container { padding: 10px; } .container .button { background: #00ff88; color: white; }
```

## Important Notes

1. **Always use `.ToSingleLine()`** when returning HTML from verbs - MUD clients need HTML as a single continuous string.

2. **Inline styles work best** - Many MUD clients don't support separate CSS files.

3. **Test colors carefully** - Different clients may render colors differently.

4. **Keep it simple** - Complex layouts may not work in all MUD clients.

5. **LESS CSS limitations** - For MUD compatibility, nesting is limited to one level deep. Complex LESS features like mixins and functions are not supported.

6. **Variables are powerful** - Use LESS variables to maintain consistent theming across your verb outputs.

## Example: Complete Login Banner

See `Resources/verbs/system/display_login.json` for a complete example of building a complex, styled login screen using this API.
