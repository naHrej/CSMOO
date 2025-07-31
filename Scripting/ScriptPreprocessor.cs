using System.Text.RegularExpressions;

namespace CSMOO.Scripting;

/// <summary>
/// Preprocesses script code to convert natural syntax to proper C# method calls
/// </summary>
public static class ScriptPreprocessor
{
    /// <summary>
    /// Preprocesses script code to convert syntax like:
    /// - player:verbname(args) -> GetObject("player").verbname(args)
    /// - player.Property -> GetObject("player").Property
    /// - #123:verbname() -> obj(123).verbname()
    /// - #123.Property -> obj(123).Property
    /// </summary>
    public static string Preprocess(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        // Pattern for object:verb(args) syntax
        // Matches: objectname:verbname(args), #123:verbname(args), etc.
        var verbCallPattern = @"\b([a-zA-Z_]\w*|#\d+|player|me|here|system|this):([a-zA-Z_]\w*)\s*\(([^)]*)\)";
        code = Regex.Replace(code, verbCallPattern, match =>
        {
            var objectRef = match.Groups[1].Value;
            var verbName = match.Groups[2].Value;
            var args = match.Groups[3].Value;

            // Handle special cases
            if (objectRef.StartsWith("#"))
            {
                // Convert #123:verb(args) to obj(123).verb(args)
                var dbref = objectRef.Substring(1);
                return $"obj({dbref}).{verbName}({args})";
            }
            else if (objectRef == "this")
            {
                // Convert this:verb(args) to @this.verb(args)
                return $"@this.{verbName}({args})";
            }
            else if (objectRef == "player" || objectRef == "me" || objectRef == "here" || objectRef == "system")
            {
                // Convert player:verb(args) to player.verb(args), etc.
                return $"{objectRef}.{verbName}({args})";
            }
            else
            {
                // Convert object:verb(args) to GetObject("object").verb(args)
                return $"GetObject(\"{objectRef}\").{verbName}({args})";
            }
        });

        // Pattern for property access: object.property (but not method calls)
        // This is trickier because we need to avoid method calls and built-in C# properties
        // We'll only convert known object references followed by capitalized properties
        var propertyPattern = @"\b(player|me|here|system|this|#\d+)\.([A-Z][a-zA-Z_]\w*)(?!\s*\()";
        code = Regex.Replace(code, propertyPattern, match =>
        {
            var objectRef = match.Groups[1].Value;
            var propertyName = match.Groups[2].Value;

            // Skip common C# properties that shouldn't be converted
            if (IsBuiltInProperty(propertyName))
            {
                return match.Value; // Return unchanged
            }

            if (objectRef.StartsWith("#"))
            {
                // Convert #123.Property to obj(123).Property
                var dbref = objectRef.Substring(1);
                return $"obj({dbref}).{propertyName}";
            }
            else if (objectRef == "this")
            {
                // Convert this.Property to @this.Property
                return $"@this.{propertyName}";
            }
            else if (objectRef == "player" || objectRef == "me" || objectRef == "here" || objectRef == "system")
            {
                // Convert player.Property to player.Property, etc. (no change needed)
                return $"{objectRef}.{propertyName}";
            }
            else
            {
                // Convert object.Property to GetObject("object").Property
                return $"GetObject(\"{objectRef}\").{propertyName}";
            }
        });

        // Pattern for assignment: object.property = value
        var assignmentPattern = @"\b(player|me|here|system|this|#\d+)\.([A-Z][a-zA-Z_]\w*)\s*=\s*([^;]+)";
        code = Regex.Replace(code, assignmentPattern, match =>
        {
            var objectRef = match.Groups[1].Value;
            var propertyName = match.Groups[2].Value;
            var value = match.Groups[3].Value;

            // Skip common C# properties that shouldn't be converted
            if (IsBuiltInProperty(propertyName))
            {
                return match.Value; // Return unchanged
            }

            if (objectRef.StartsWith("#"))
            {
                // Convert #123.Property = value to obj(123).Property = value
                var dbref = objectRef.Substring(1);
                return $"obj({dbref}).{propertyName} = {value}";
            }
            else if (objectRef == "this")
            {
                // Convert this.Property = value to @this.Property = value
                return $"@this.{propertyName} = {value}";
            }
            else if (objectRef == "player" || objectRef == "me" || objectRef == "here" || objectRef == "system")
            {
                // Convert player.Property = value to player.Property = value, etc. (no change needed)
                return $"{objectRef}.{propertyName} = {value}";
            }
            else
            {
                // Convert object.Property = value to GetObject("object").Property = value
                return $"GetObject(\"{objectRef}\").{propertyName} = {value}";
            }
        });

        return code;
    }

    /// <summary>
    /// Check if a property name is a built-in C# property that shouldn't be converted
    /// </summary>
    private static bool IsBuiltInProperty(string propertyName)
    {
        // Common C# properties that should not be converted
        var builtInProperties = new[]
        {
            "Length", "Count", "ToString", "GetType", "Equals", "GetHashCode",
            "Id", "Name", "Location", "SessionGuid", "IsOnline", // Our known Player properties
            "Message", "Exception", "Source", "StackTrace" // Common exception properties
        };

        return Array.Exists(builtInProperties, prop => 
            prop.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
    }
}



