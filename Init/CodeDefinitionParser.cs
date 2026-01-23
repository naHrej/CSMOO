using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSMOO.Functions;
using CSMOO.Verbs;
using CSMOO.Object;
using CSMOO.Logging;
using dotless.Core.Parser;
using System.Text.RegularExpressions;
using System.Linq;

namespace CSMOO.Init;

/// <summary>
/// Parses C# class definitions to extract function and verb definitions
/// </summary>
public static class CodeDefinitionParser
{
    /// <summary>
    /// Dictionary to store help metadata for categories and topics
    /// Key: category/topic name, Value: (description, summary)
    /// </summary>
    private static Dictionary<string, (string? Description, string? Summary)> _helpMetadata = new Dictionary<string, (string?, string?)>(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// General help preamble text (displayed when user types 'help' with no arguments)
    /// </summary>
    private static string? _helpPreamble = null;
    
    /// <summary>
    /// Get help metadata for a category or topic
    /// </summary>
    public static (string? Description, string? Summary) GetHelpMetadata(string name)
    {
        return _helpMetadata.TryGetValue(name, out var metadata) ? metadata : (null, null);
    }
    
    /// <summary>
    /// Get the general help preamble text
    /// </summary>
    public static string? GetHelpPreamble()
    {
        return _helpPreamble;
    }
    
    /// <summary>
    /// Parse HelpMetadata class to extract category and topic descriptions
    /// </summary>
    public static void ParseHelpMetadata(string filePath)
    {
        try
        {
            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.ValueText;
                
                // Only process HelpMetadata class
                if (className != "HelpMetadata")
                    continue;
                
                foreach (var method in classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var methodName = method.Identifier.ValueText;
                    
                    // Handle general help preamble
                    if (methodName == "_help_preamble")
                    {
                        var helpMetadata = ExtractHelpMetadata(method);
                        if (helpMetadata == null)
                        {
                            helpMetadata = ExtractHelpMetadataFromSource(code, method);
                        }
                        
                        if (helpMetadata != null)
                        {
                            var description = ExtractDescriptionFromXml(helpMetadata, code, method);
                            _helpPreamble = description ?? helpMetadata.Summary;
                            Logger.Info($"[HELP PARSER] Parsed help preamble: {!string.IsNullOrEmpty(_helpPreamble)}");
                        }
                        continue;
                    }
                    
                    // Only process methods that start with _category_ or _topic_
                    if (!methodName.StartsWith("_category_") && !methodName.StartsWith("_topic_"))
                        continue;
                    
                    var helpMetadata2 = ExtractHelpMetadata(method);
                    if (helpMetadata2 == null)
                    {
                        // Try fallback
                        helpMetadata2 = ExtractHelpMetadataFromSource(code, method);
                    }
                    
                    if (helpMetadata2 != null)
                    {
                        // Extract category or topic name from method name
                        string? categoryOrTopic = null;
                        if (methodName.StartsWith("_category_"))
                        {
                            categoryOrTopic = methodName.Substring("_category_".Length);
                        }
                        else if (methodName.StartsWith("_topic_"))
                        {
                            categoryOrTopic = methodName.Substring("_topic_".Length);
                        }
                        
                        if (!string.IsNullOrEmpty(categoryOrTopic))
                        {
                            // Extract description from XML
                            var description = ExtractDescriptionFromXml(helpMetadata2, code, method);
                            var summary = helpMetadata2.Summary;
                            
                            _helpMetadata[categoryOrTopic] = (description, summary);
                            Logger.Info($"[HELP PARSER] Parsed help metadata for '{categoryOrTopic}': Description={!string.IsNullOrEmpty(description)}, Summary={!string.IsNullOrEmpty(summary)}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing help metadata from {filePath}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Extract description from XML documentation
    /// </summary>
    private static string? ExtractDescriptionFromXml(HelpMetadata metadata, string sourceCode, MethodDeclarationSyntax method)
    {
        // Extract from source XML - get the normalized XML text first
        var methodName = method.Identifier.ValueText;
        var methodStart = method.Identifier.SpanStart;
        var allLines = sourceCode.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        
        int methodLineIndex = -1;
        int charCount = 0;
        string newlineStr = sourceCode.Contains("\r\n") ? "\r\n" : (sourceCode.Contains("\n") ? "\n" : "\r");
        int newlineLen = newlineStr.Length;
        
        for (int lineIdx = 0; lineIdx < allLines.Length; lineIdx++)
        {
            int lineEnd = charCount + allLines[lineIdx].Length;
            if (methodStart >= charCount && methodStart <= lineEnd)
            {
                methodLineIndex = lineIdx;
                break;
            }
            charCount = lineEnd + newlineLen;
        }
        
        if (methodLineIndex < 0) return null;
        
        var lines = allLines.Take(methodLineIndex).ToArray();
        var xmlLines = new List<string>();
        var i = lines.Length - 1;
        
        while (i >= 0 && string.IsNullOrWhiteSpace(lines[i]))
            i--;
        
        // Skip attribute lines
        while (i >= 0)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("[") && (trimmed.Contains("Verb") || trimmed.Contains("]")))
            {
                i--;
                continue;
            }
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                i--;
                continue;
            }
            break;
        }
        
        // Collect XML comment lines
        while (i >= 0)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("///"))
            {
                xmlLines.Add(trimmed);
                i--;
                continue;
            }
            if (!string.IsNullOrWhiteSpace(trimmed))
                break;
            i--;
        }
        
        if (xmlLines.Count == 0) return null;
        
        xmlLines.Reverse();
        var allXmlText = string.Join("\n", xmlLines);
        var normalizedText = Regex.Replace(allXmlText, @"^\s*///\s*", "", RegexOptions.Multiline);
        
        // Extract description tag
        // Use a more precise pattern that stops at </description> and doesn't capture nested XML tags
        var descMatch = Regex.Match(normalizedText, @"<description>\s*(.*?)\s*</description>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (descMatch.Success)
        {
            var descriptionText = descMatch.Groups[1].Value;
            // Remove any stray XML tags that might have been captured (like <category> or <topic>)
            // These should not be in the description text itself
            descriptionText = Regex.Replace(descriptionText, @"<(category|topic|Category|Topic)>\s*.*?\s*</\1>", "", 
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            // Use special cleaning for descriptions to preserve HTML styling with double-escaping
            return CleanXmlTextForDescription(descriptionText);
        }
        
        return null;
    }
    /// <summary>
    /// Parse a C# file and extract function definitions
    /// </summary>
    public static List<FunctionDefinition> ParseFunctions(string filePath)
    {
        try
        {
            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            
            var functions = new List<FunctionDefinition>();
            
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.ValueText;
                
                foreach (var method in classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var returnType = method.ReturnType.ToString();
                    
                    // Skip verb methods - they're handled by ParseVerbs
                    if (returnType == "verb")
                        continue;
                        
                    // Only process public methods
                    if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                        continue;
                    
                    var helpMetadata = ExtractHelpMetadata(method);
                    
                    var function = new FunctionDefinition
                    {
                        Name = method.Identifier.ValueText,
                        TargetClass = className,
                        Code = ExtractMethodBody(method),
                        Parameters = ExtractParameterTypes(method).ToArray(),
                        ParameterNames = ExtractParameterNames(method).ToArray(),
                        ReturnType = returnType,
                        Description = helpMetadata?.Summary ?? $"Function {method.Identifier.ValueText} for {className}",
                        Accessors = ExtractAccessors(method),
                        Categories = helpMetadata?.Categories ?? new List<string>(),
                        Topics = helpMetadata?.Topics ?? new List<string>(),
                        Usage = helpMetadata?.Usage,
                        HelpText = helpMetadata?.HelpText
                    };
                    
                    functions.Add(function);
                }
            }
            
            return functions;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing functions from {filePath}: {ex.Message}");
            return new List<FunctionDefinition>();
        }
    }
    
    /// <summary>
    /// Parse a C# file and extract verb definitions
    /// </summary>
    public static List<VerbDefinition> ParseVerbs(string filePath)
    {
        try
        {
            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            
            var verbs = new List<VerbDefinition>();
            
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.ValueText;
                
                foreach (var method in classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var returnType = method.ReturnType.ToString();
                    
                    // Only process verb methods
                    if (returnType != "verb")
                        continue;
                        
                    // Only process public methods
                    if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                        continue;
                    
                    var methodName = method.Identifier.ValueText;
                    
                    // Try to extract help metadata from the method node
                    var helpMetadata = ExtractHelpMetadata(method);
                    
                    // If not found, try extracting from source text directly (fallback)
                    if (helpMetadata == null)
                    {
                        helpMetadata = ExtractHelpMetadataFromSource(code, method);
                    }
                    
                    // Combine aliases from attributes and XML comments
                    var attributeAliases = ExtractAliasesFromAttributes(method);
                    var xmlAliases = helpMetadata?.Aliases ?? new List<string>();
                    var allAliases = attributeAliases.Concat(xmlAliases).Distinct().ToList();
                    
                    var verb = new VerbDefinition
                    {
                        Name = methodName.ToLower(),
                        TargetClass = className,
                        Code = ExtractMethodBody(method),
                        Pattern = ExtractPatternFromAttributes(method) ?? GeneratePatternFromParameters(method),
                        Aliases = string.Join(" ", allAliases),
                        Description = ExtractDescriptionFromAttributes(method) ?? helpMetadata?.Summary ?? $"Verb {methodName} for {className}",
                        Categories = helpMetadata?.Categories ?? new List<string>(),
                        Topics = helpMetadata?.Topics ?? new List<string>(),
                        Usage = helpMetadata?.Usage,
                        HelpText = helpMetadata?.HelpText
                    };
                    
                    if (verb.Categories.Count > 0 || verb.Topics.Count > 0)
                    {
                        Logger.Info($"[HELP PARSER] Verb '{verb.Name}' has {verb.Categories.Count} categories and {verb.Topics.Count} topics");
                    }
                    else
                    {
                        Logger.Info($"[HELP PARSER] Verb '{verb.Name}' has NO categories or topics (helpMetadata was {(helpMetadata == null ? "NULL" : "not null")})");
                    }
                    
                    verbs.Add(verb);
                }
            }
            
            return verbs;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing verbs from {filePath}: {ex.Message}");
            return new List<VerbDefinition>();
        }
    }


        private static List<Keyword> ExtractAccessors(MemberDeclarationSyntax member)
    {
        var accessors = new List<Keyword>();

        // collect modifiers (e.g. "public", "readonly")
        var modifiers = member.Modifiers.Select(m => m.Text.ToLowerInvariant()).ToList();

        // collect attribute names (e.g. "ReadOnly", "MyNs.ReadOnlyAttribute")
        var attributes = member.AttributeLists
            .SelectMany(attr => attr.Attributes.Select(a => a.Name.ToString().ToLowerInvariant()));

        modifiers.AddRange(attributes);

        foreach (var mod in modifiers)
        {
            switch (mod)
            {
                case "public":
                    accessors.Add(Keyword.Public);
                    break;
                case "private":
                    accessors.Add(Keyword.Private);
                    break;
                case "internal":
                    accessors.Add(Keyword.Internal);
                    break;
                case "protected":
                    accessors.Add(Keyword.Protected);
                    break;
                case "readonly":
                    accessors.Add(Keyword.ReadOnly);
                    break;
                case "writeonly":
                    accessors.Add(Keyword.WriteOnly);
                    break;
                case "hidden":
                    accessors.Add(Keyword.Hidden);
                    break;
                case "adminonly":
                    accessors.Add(Keyword.AdminOnly);
                    break;
            }
        }

        return accessors;
    }

    /// <summary>
    /// Parse a C# file and extract property definitions
    /// </summary>
    public static List<PropertyDefinition> ParseProperties(string filePath)
    {
        try
        {
            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            var properties = new List<PropertyDefinition>();

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.ValueText;

                foreach (var field in classDecl.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    // Process all fields regardless of access modifier
                    // The access control will be handled by the Accessor property
                    
                    foreach (var variable in field.Declaration.Variables)
                    {
                        var property = new PropertyDefinition
                        {
                            Name = variable.Identifier.ValueText,
                            TargetClass = className,
                            Type = DeterminePropertyType(field.Declaration.Type),
                            Value = ExtractPropertyValue(variable),
                            Description = ExtractDocumentation(field) ?? $"Property {variable.Identifier.ValueText} for {className}",
                            Accessors = ExtractAccessors(field)
                        };

                        properties.Add(property);
                    }
                }
            }

            return properties;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing properties from {filePath}: {ex.Message}");
            return new List<PropertyDefinition>();
        }
    }
    
    /// <summary>
    /// Extract the method body as an array of code lines
    /// </summary>
    private static string[] ExtractMethodBody(MethodDeclarationSyntax method)
    {
        if (method.Body == null)
            return new string[0];
            
        // Get the body text without the surrounding braces
        var bodyText = method.Body.ToString();
        
        // Remove opening and closing braces
        if (bodyText.StartsWith("{") && bodyText.EndsWith("}"))
        {
            bodyText = bodyText.Substring(1, bodyText.Length - 2).Trim();
        }
        
        // Split into lines and clean up
        var lines = bodyText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(line => line.TrimEnd())
                           .ToArray();
        
        return lines;
    }
    
    /// <summary>
    /// Extract parameter types from method signature
    /// </summary>
    private static List<string> ExtractParameterTypes(MethodDeclarationSyntax method)
    {
        return method.ParameterList.Parameters
                    .Select(p => p.Type?.ToString() ?? "object")
                    .ToList();
    }
    
    /// <summary>
    /// Extract parameter names from method signature
    /// </summary>
    private static List<string> ExtractParameterNames(MethodDeclarationSyntax method)
    {
        return method.ParameterList.Parameters
                    .Select(p => p.Identifier.ValueText)
                    .ToList();
    }
    
    /// <summary>
    /// Generate a pattern from method parameters for verbs
    /// </summary>
    private static string GeneratePatternFromParameters(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;
        
        if (parameters.Count == 0)
            return "*"; // No parameters, match anything
        
        // Generate pattern like "methodname {param1} {param2}"
        var pattern = method.Identifier.ValueText.ToLower();
        
        foreach (var param in parameters)
        {
            pattern += $" {{{param.Identifier.ValueText}}}";
        }
        
        return pattern;
    }
    
    /// <summary>
    /// Extract documentation comments from method
    /// </summary>
    private static string? ExtractDocumentation(MethodDeclarationSyntax method)
    {
        var helpMetadata = ExtractHelpMetadata(method);
        return helpMetadata?.Summary;
    }

    /// <summary>
    /// Extract help metadata from XML documentation comments
    /// </summary>
    private static HelpMetadata? ExtractHelpMetadata(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.ValueText;
        
        string text = "";
        
        // Approach 1: Check all leading trivia of the method (includes XML after attributes)
        var allLeadingTrivia = method.GetLeadingTrivia();
        foreach (var trivia in allLeadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                text = trivia.ToString();
                Logger.Info($"[HELP PARSER] Method '{methodName}' - Found doc comment in leading trivia");
                break;
            }
        }
        
        // Approach 2: If we have attributes, check ALL trivia in ALL attribute lists
        // XML comments can appear after attributes but before the method declaration
        if (string.IsNullOrEmpty(text) && method.AttributeLists.Count > 0)
        {
            // Check trailing trivia of each attribute list
            foreach (var attrList in method.AttributeLists)
            {
                var trailingTrivia = attrList.GetTrailingTrivia();
                foreach (var trivia in trailingTrivia)
                {
                    if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                        trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                    {
                        text = trivia.ToString();
                        Logger.Info($"[HELP PARSER] Method '{methodName}' - Found doc comment in attribute list trailing trivia");
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(text)) break;
                
                // Also check leading trivia of attribute list (unlikely but possible)
                var leadingTrivia = attrList.GetLeadingTrivia();
                foreach (var trivia in leadingTrivia)
                {
                    if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                        trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                    {
                        text = trivia.ToString();
                        Logger.Info($"[HELP PARSER] Method '{methodName}' - Found doc comment in attribute list leading trivia");
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(text)) break;
            }
        }
        
        // Approach 3: Check modifiers trivia (public, static, etc.) - XML might be between modifier and method
        if (string.IsNullOrEmpty(text) && method.Modifiers.Count > 0)
        {
            var lastModifier = method.Modifiers.Last();
            var modifierTrailingTrivia = lastModifier.TrailingTrivia;
            foreach (var trivia in modifierTrailingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    text = trivia.ToString();
                    Logger.Info($"[HELP PARSER] Method '{methodName}' - Found doc comment in modifier trailing trivia");
                    break;
                }
            }
        }
        
        // Approach 4: Check return type trivia (verb keyword) - XML might be before return type
        if (string.IsNullOrEmpty(text) && method.ReturnType != null)
        {
            var returnTypeLeadingTrivia = method.ReturnType.GetLeadingTrivia();
            foreach (var trivia in returnTypeLeadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    text = trivia.ToString();
                    Logger.Info($"[HELP PARSER] Method '{methodName}' - Found doc comment in return type leading trivia");
                    break;
                }
            }
        }
        
        if (string.IsNullOrEmpty(text))
        {
            // Debug: Log what trivia we found
            var triviaTypes = string.Join(", ", allLeadingTrivia.Select(t => t.Kind().ToString()));
            Logger.Info($"[HELP PARSER] Method '{methodName}' - No documentation comment found. Leading trivia ({allLeadingTrivia.Count} items): {triviaTypes}");
            if (method.AttributeLists.Count > 0)
            {
                var lastAttrTrailing = method.AttributeLists.Last().GetTrailingTrivia();
                var attrTriviaTypes = string.Join(", ", lastAttrTrailing.Select(t => t.Kind().ToString()));
                Logger.Info($"[HELP PARSER] Method '{methodName}' - Last attribute trailing trivia ({lastAttrTrailing.Count} items): {attrTriviaTypes}");
            }
            return null;
        }
        
        // Normalize the XML text by removing /// prefixes from each line
        // Roslyn includes the /// markers in the documentation comment text
        // Try multiple patterns to handle different formats
        var normalizedText = text;
        // Remove /// at start of lines
        normalizedText = Regex.Replace(normalizedText, @"^\s*///\s*", "", RegexOptions.Multiline);
        // Also handle cases where /// might be followed by a space
        normalizedText = Regex.Replace(normalizedText, @"///\s*", "", RegexOptions.Multiline);
        
        // Log at Info level so it's visible
        if (normalizedText.Length > 0)
        {
            var debugText = normalizedText.Length > 200 ? normalizedText.Substring(0, 200) + "..." : normalizedText;
            Logger.Info($"[HELP PARSER] Method '{methodName}' - Normalized XML (first 200 chars): {debugText.Replace("\r", "\\r").Replace("\n", "\\n")}");
        }
        
        var metadata = new HelpMetadata();
        
        // Extract summary
        var summaryMatch = Regex.Match(normalizedText, @"<summary>\s*(.*?)\s*</summary>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (summaryMatch.Success)
        {
            metadata.Summary = CleanXmlText(summaryMatch.Groups[1].Value);
        }
        
        // Extract categories
        metadata.Categories = ExtractXmlTags(normalizedText, "category");
        if (metadata.Categories.Count > 0)
        {
            Logger.Info($"[HELP PARSER] Method '{methodName}' - Found {metadata.Categories.Count} categories: {string.Join(", ", metadata.Categories)}");
        }
        
        // Extract topics
        metadata.Topics = ExtractXmlTags(normalizedText, "topic");
        if (metadata.Topics.Count > 0)
        {
            Logger.Info($"[HELP PARSER] Method '{methodName}' - Found {metadata.Topics.Count} topics: {string.Join(", ", metadata.Topics)}");
        }
        
        // Extract usage
        var usageMatch = Regex.Match(normalizedText, @"<usage>\s*(.*?)\s*</usage>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (usageMatch.Success)
        {
            var rawUsage = CleanXmlText(usageMatch.Groups[1].Value);
            // Automatically style parameters in usage
            metadata.Usage = StyleUsageParameters(rawUsage);
        }
        
        // Extract help text (multiline content)
        var helpMatch = Regex.Match(normalizedText, @"<help>\s*(.*?)\s*</help>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (helpMatch.Success)
        {
            metadata.HelpText = CleanXmlText(helpMatch.Groups[1].Value);
        }
        
        // Extract aliases (additional to VerbAliases attribute)
        metadata.Aliases = ExtractXmlTags(normalizedText, "alias");
        
        // Only return if we found at least a summary
        return string.IsNullOrEmpty(metadata.Summary) && 
               metadata.Categories.Count == 0 && 
               metadata.Topics.Count == 0 && 
               string.IsNullOrEmpty(metadata.Usage) && 
               string.IsNullOrEmpty(metadata.HelpText) 
            ? null 
            : metadata;
    }

    /// <summary>
    /// Fallback: Extract help metadata directly from source text by finding XML comments before method
    /// </summary>
    private static HelpMetadata? ExtractHelpMetadataFromSource(string sourceCode, MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.ValueText;
        Logger.Info($"[HELP PARSER] Method '{methodName}' - Fallback: Starting source text extraction");
        
        // Anchor on the identifier token (method name). This is reliably within the method declaration,
        // even when Roslyn's MethodDeclarationSyntax.Span doesn't include attributes the way we expect.
        var methodStart = method.Identifier.SpanStart;
        
        // Split source into lines to find which line contains the method
        var allLines = sourceCode.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        
        // Find which line contains the method identifier by counting characters
        int methodLineIndex = -1;
        int charCount = 0;
        string newlineStr = sourceCode.Contains("\r\n") ? "\r\n" : (sourceCode.Contains("\n") ? "\n" : "\r");
        int newlineLen = newlineStr.Length;
        
        for (int lineIdx = 0; lineIdx < allLines.Length; lineIdx++)
        {
            int lineEnd = charCount + allLines[lineIdx].Length;
            if (methodStart >= charCount && methodStart <= lineEnd)
            {
                methodLineIndex = lineIdx;
                break;
            }
            charCount = lineEnd + newlineLen; // Move past line content and newline
        }
        
        if (methodLineIndex < 0)
        {
            Logger.Info($"[HELP PARSER] Method '{methodName}' - Fallback: Could not find method line");
            return null;
        }
        
        // Get lines before the method declaration line
        var lines = allLines.Take(methodLineIndex).ToArray();
        
        Logger.Info($"[HELP PARSER] Method '{methodName}' - Fallback: Checking {lines.Length} lines before method (method is on line {methodLineIndex + 1})");
        
        var xmlLines = new List<string>();
        
        // Walk backwards from the line right above the method.
        // Order in file: [attributes] -> /// XML comments -> method
        // So we need to: skip whitespace -> collect /// lines -> skip [attributes] -> stop
        var i = lines.Length - 1;
        while (i >= 0 && string.IsNullOrWhiteSpace(lines[i]))
            i--;
        
        // First, collect consecutive /// lines (these come right before the method, after attributes)
        while (i >= 0)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("///"))
            {
                xmlLines.Add(trimmed);
                i--;
                continue;
            }
            // Stop collecting if we hit something that's not /// or whitespace
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                break;
            }
            // Whitespace between XML comment lines is okay
            i--;
        }
        
        // If we found XML comments, we're done. The attributes come before them, but we don't need them.
        // If we didn't find any, the XML might be before attributes, so skip attributes and try again
        if (xmlLines.Count == 0)
        {
            // Skip over attribute lines and whitespace, then try collecting /// again
            while (i >= 0)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("[") && (trimmed.Contains("Verb") || trimmed.Contains("]")))
                {
                    i--;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    i--;
                    continue;
                }
                // Now try collecting /// lines that might be before attributes
                if (trimmed.StartsWith("///"))
                {
                    xmlLines.Add(trimmed);
                    i--;
                    // Continue collecting /// lines
                    while (i >= 0)
                    {
                        var trimmed2 = lines[i].TrimStart();
                        if (trimmed2.StartsWith("///"))
                        {
                            xmlLines.Add(trimmed2);
                            i--;
                            continue;
                        }
                        if (!string.IsNullOrWhiteSpace(trimmed2))
                        {
                            break;
                        }
                        i--;
                    }
                }
                break;
            }
        }
        
        Logger.Info($"[HELP PARSER] Method '{methodName}' - Fallback: Found {xmlLines.Count} XML comment lines");
        
        if (xmlLines.Count == 0)
        {
            // Debug: show last few lines before method
            var lastLines = lines.Length > 10 ? lines.Skip(lines.Length - 10) : lines;
            Logger.Info($"[HELP PARSER] Method '{methodName}' - Fallback: Last 10 lines before method: {string.Join(" | ", lastLines.Select(l => l.Trim().Substring(0, Math.Min(50, l.Trim().Length))))}");
            return null;
        }
        
        xmlLines.Reverse();
        
        // Normalize by removing /// prefixes
        var allXmlText = string.Join("\n", xmlLines);
        var normalizedText = Regex.Replace(allXmlText, @"^\s*///\s*", "", RegexOptions.Multiline);
        
        // Found XML comments in source - parse them
        var metadata = new HelpMetadata();
        
        // Extract categories
        metadata.Categories = ExtractXmlTags(normalizedText, "category");
        
        // Extract topics
        metadata.Topics = ExtractXmlTags(normalizedText, "topic");
        
        // Extract usage
        var usageMatch = Regex.Match(normalizedText, @"<usage>\s*(.*?)\s*</usage>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (usageMatch.Success)
        {
            var rawUsage = CleanXmlText(usageMatch.Groups[1].Value);
            // Automatically style parameters in usage
            metadata.Usage = StyleUsageParameters(rawUsage);
        }
        
        // Extract help text
        var helpMatch = Regex.Match(normalizedText, @"<help>\s*(.*?)\s*</help>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (helpMatch.Success)
        {
            metadata.HelpText = CleanXmlText(helpMatch.Groups[1].Value);
        }
        
        // Extract summary
        var summaryMatch = Regex.Match(normalizedText, @"<summary>\s*(.*?)\s*</summary>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (summaryMatch.Success)
        {
            metadata.Summary = CleanXmlText(summaryMatch.Groups[1].Value);
        }
        
        // Only return if we found something
        if (metadata.Categories.Count > 0 || metadata.Topics.Count > 0 || 
            !string.IsNullOrEmpty(metadata.Usage) || !string.IsNullOrEmpty(metadata.HelpText))
        {
            Logger.Info($"[HELP PARSER] Method '{methodName}' - Found help metadata via source text fallback");
            return metadata;
        }
        
        return null;
    }

    /// <summary>
    /// Extract all occurrences of an XML tag from documentation text
    /// </summary>
    private static List<string> ExtractXmlTags(string xml, string tagName)
    {
        // XML text should already be normalized (/// prefixes removed)
        // Use a pattern that handles whitespace and newlines
        var pattern = $@"<{tagName}>\s*(.*?)\s*</{tagName}>";
        var matches = Regex.Matches(xml, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var results = matches.Cast<Match>()
            .Select(m => CleanXmlText(m.Groups[1].Value))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        
        // Log at Info level for visibility
        if (results.Count > 0)
        {
            Logger.Info($"[HELP PARSER] ExtractXmlTags('{tagName}') found {results.Count} matches: {string.Join(", ", results)}");
        }
        
        return results;
    }

    /// <summary>
    /// Clean XML text by removing comment markers and extra whitespace
    /// </summary>
    private static string CleanXmlText(string text)
    {
        var cleaned = text
            .Replace("///", "")
            .Replace("//", "")
            .Trim();
        
        // Decode HTML entities to allow HTML in help text
        // XML comments require escaping < and > as &lt; and &gt;
        cleaned = cleaned
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'");
        
        return cleaned;
    }
    
    /// <summary>
    /// Clean XML text for descriptions - handles escaping for HTML styling
    /// User can write &lt;category&gt; (single-escaped) in XML
    /// XML parser decodes &lt; to <, so we get &lt;category&gt; in extracted text
    /// We need to escape < and > that aren't part of HTML tags back to &lt; and &gt;
    /// This allows user to write &lt;category&gt; instead of &amp;lt;category&amp;gt;
    /// </summary>
    private static string CleanXmlTextForDescription(string text)
    {
        var cleaned = text
            .Replace("///", "")
            .Replace("//", "")
            .Trim();
        
        // First decode all HTML entities
        cleaned = cleaned
            .Replace("&amp;", "&")  // Decode &amp; first
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">");
        
        // Now we need to escape < and > that aren't part of HTML tags
        // HTML tags are like <span>, </span>, <div class='...'>, etc.
        // We'll preserve HTML tags but escape other angle brackets
        
        // Strategy: Find all valid HTML tags (whitelist) and replace them with placeholders,
        // escape remaining < and >, then restore the HTML tags
        
        // Whitelist of valid HTML tags that should be preserved
        var validHtmlTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "span", "div", "p", "strong", "em", "b", "i", "u", "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li", "br", "hr", "a", "img", "code", "pre", "blockquote", "section"
        };
        
        // Pattern to match HTML tags: <tag> or </tag> or <tag attr="value">
        var htmlTagPattern = @"</?([a-zA-Z][a-zA-Z0-9]*)(?:\s+[^>]*)?>";
        var htmlTags = new List<(string Original, string Placeholder)>();
        var tagIndex = 0;
        
        // Find all HTML tags and replace only valid ones with placeholders
        cleaned = Regex.Replace(cleaned, htmlTagPattern, match =>
        {
            var tagName = match.Groups[1].Value;
            // Only preserve tags that are in our whitelist
            if (validHtmlTags.Contains(tagName))
            {
                var placeholder = $"__HTML_TAG_{tagIndex}__";
                htmlTags.Add((match.Value, placeholder));
                tagIndex++;
                return placeholder;
            }
            // For non-whitelisted tags (like <category>), return as-is so they get escaped later
            return match.Value;
        });
        
        // Now escape any remaining < and > (these are not valid HTML tags)
        cleaned = cleaned
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
        
        // Restore valid HTML tags (they should remain as-is)
        foreach (var (original, placeholder) in htmlTags)
        {
            cleaned = cleaned.Replace(placeholder, original);
        }
        
        return cleaned;
    }
    
    /// <summary>
    /// Automatically style parameters in usage strings
    /// Detects &lt;parameter&gt; patterns and wraps them with param styling
    /// User can write &lt;parameter&gt; in XML (single-escaped) or &amp;lt;parameter&amp;gt; (double-escaped)
    /// After XML parsing, we get &lt;parameter&gt; (if double-escaped) or &lt;parameter&gt; (if single-escaped, XML parser decodes it)
    /// We style it and escape for storage: &lt;span class='param'&gt;&amp;lt;parameter&amp;gt;&lt;/span&gt;
    /// </summary>
    private static string StyleUsageParameters(string usage)
    {
        if (string.IsNullOrEmpty(usage)) return usage;
        
        // After CleanXmlText, we have either:
        // - &lt;parameter&gt; (if user wrote &amp;lt;parameter&amp;gt; in XML)
        // - <parameter> (if user wrote &lt;parameter&gt; in XML, XML parser decoded it)
        // We need to detect both patterns and style them
        
        var result = usage;
        var startIdx = 0;
        
        while (true)
        {
            // First try to find &lt; (escaped < from double-escaping)
            var idx = result.IndexOf("&lt;", startIdx, StringComparison.OrdinalIgnoreCase);
            var isEscaped = true;
            
            if (idx < 0)
            {
                // Try unescaped < (from single-escaping that XML parser decoded)
                idx = result.IndexOf("<", startIdx, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) break;
                isEscaped = false;
                
                // Skip if it's already part of an HTML tag (like <span class='param'>)
                if (idx > 0)
                {
                    var before = result.Substring(Math.Max(0, idx - 10), Math.Min(10, idx));
                    if (before.Contains("span") || before.Contains("class") || before.Contains("div"))
                    {
                        startIdx = idx + 1;
                        continue;
                    }
                }
            }
            
            // Find the matching closing bracket
            var endIdx = -1;
            string paramName;
            
            if (isEscaped)
            {
                // Escaped version: &lt;parameter&gt;
                endIdx = result.IndexOf("&gt;", idx + 4, StringComparison.OrdinalIgnoreCase);
                if (endIdx < 0) break;
                paramName = result.Substring(idx + 4, endIdx - idx - 4);
                
                // Wrap with styling: &lt;span class='param'&gt;&amp;lt;parameter&amp;gt;&lt;/span&gt;
                // This will display as: <span class='param'>&lt;parameter&gt;</span>
                var replacement = $"&lt;span class='param'&gt;&amp;lt;{paramName}&amp;gt;&lt;/span&gt;";
                result = result.Substring(0, idx) + replacement + result.Substring(endIdx + 4);
                startIdx = idx + replacement.Length;
            }
            else
            {
                // Unescaped version: <parameter>
                endIdx = result.IndexOf(">", idx + 1, StringComparison.OrdinalIgnoreCase);
                if (endIdx < 0) break;
                paramName = result.Substring(idx + 1, endIdx - idx - 1);
                
                // Skip if it looks like an HTML tag (contains space, =, /, or common HTML tag names)
                if (paramName.Contains(" ") || paramName.Contains("=") || paramName.Contains("/") ||
                    paramName.Equals("span", StringComparison.OrdinalIgnoreCase) ||
                    paramName.Equals("div", StringComparison.OrdinalIgnoreCase) ||
                    paramName.StartsWith("span", StringComparison.OrdinalIgnoreCase) ||
                    paramName.StartsWith("div", StringComparison.OrdinalIgnoreCase))
                {
                    startIdx = endIdx + 1;
                    continue;
                }
                
                // Valid parameter - wrap with styling: &lt;span class='param'&gt;&amp;lt;parameter&amp;gt;&lt;/span&gt;
                // This will display as: <span class='param'>&lt;parameter&gt;</span>
                var replacement = $"&lt;span class='param'&gt;&amp;lt;{paramName}&amp;gt;&lt;/span&gt;";
                result = result.Substring(0, idx) + replacement + result.Substring(endIdx + 1);
                startIdx = idx + replacement.Length;
            }
        }
        
        return result;
    }

    /// <summary>
    /// Helper class to hold parsed help metadata from XML documentation
    /// </summary>
    private class HelpMetadata
    {
        public string? Summary { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Topics { get; set; } = new List<string>();
        public string? Usage { get; set; }
        public string? HelpText { get; set; }
        public List<string> Aliases { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Extract aliases from attributes
    /// </summary>
    private static List<string> ExtractAliasesFromAttributes(MethodDeclarationSyntax method)
    {
        var aliases = new List<string>();
        
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == "VerbAliases" || attribute.Name.ToString() == "VerbAliasesAttribute")
                {
                    if (attribute.ArgumentList?.Arguments.Count > 0)
                    {
                        var firstArg = attribute.ArgumentList.Arguments[0];
                        if (firstArg.Expression is LiteralExpressionSyntax literal)
                        {
                            var aliasText = literal.Token.ValueText;
                            aliases.AddRange(aliasText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(a => a.Trim())
                                           .Where(a => !string.IsNullOrEmpty(a)));
                        }
                    }
                }
            }
        }
        
        return aliases;
    }

    /// <summary>
    /// Extract description from attributes
    /// </summary>
    private static string? ExtractDescriptionFromAttributes(MethodDeclarationSyntax method)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == "VerbDescription" || attribute.Name.ToString() == "VerbDescriptionAttribute")
                {
                    if (attribute.ArgumentList?.Arguments.Count > 0)
                    {
                        var firstArg = attribute.ArgumentList.Arguments[0];
                        if (firstArg.Expression is LiteralExpressionSyntax literal)
                        {
                            return literal.Token.ValueText;
                        }
                    }
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Extract pattern from attributes
    /// </summary>
    private static string? ExtractPatternFromAttributes(MethodDeclarationSyntax method)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == "VerbPattern" || attribute.Name.ToString() == "VerbPatternAttribute")
                {
                    if (attribute.ArgumentList?.Arguments.Count > 0)
                    {
                        var firstArg = attribute.ArgumentList.Arguments[0];
                        if (firstArg.Expression is LiteralExpressionSyntax literal)
                        {
                            var patternText = literal.Token.ValueText;
                            return patternText;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Determine property type from type syntax
    /// </summary>
    private static string DeterminePropertyType(TypeSyntax typeSyntax)
    {
        var typeText = typeSyntax.ToString();
        
        return typeText switch
        {
            "int" => "int",
            "string" => "string",
            "bool" => "bool",
            "float" => "float",
            "double" => "double",
            "decimal" => "decimal",
            "string[]" => "array",
            _ when typeText.EndsWith("[]") => "array",
            _ => "string" // Default to string
        };
    }
    
    /// <summary>
    /// Extract property value from variable declarator
    /// </summary>
    private static object? ExtractPropertyValue(VariableDeclaratorSyntax variable)
    {
        if (variable.Initializer?.Value == null)
            return null;
            
        var valueExpression = variable.Initializer.Value;
        
        // Handle different types of expressions
        if (valueExpression is LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText;
        }
        else if (valueExpression is InvocationExpressionSyntax invocation)
        {
            // Handle LoadFile("filename") calls
            if (invocation.Expression.ToString() == "LoadFile" && 
                invocation.ArgumentList.Arguments.Count > 0)
            {
                var firstArg = invocation.ArgumentList.Arguments[0];
                if (firstArg.Expression is LiteralExpressionSyntax filenameArg)
                {
                    // This is a file reference - we'll store the filename
                    return new { IsFileReference = true, Filename = filenameArg.Token.ValueText };
                }
            }
        }
        
        // For other expressions, return the full text
        return valueExpression.ToString();
    }
    
    /// <summary>
    /// Extract documentation from field declaration
    /// </summary>
    private static string? ExtractDocumentation(FieldDeclarationSyntax field)
    {
        // Look for XML documentation comments
        var triviaList = field.GetLeadingTrivia();
        foreach (var trivia in triviaList)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                var text = trivia.ToString();
                // Extract summary content
                var summaryMatch = System.Text.RegularExpressions.Regex.Match(text, @"<summary>\s*(.*?)\s*</summary>", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (summaryMatch.Success)
                {
                    return summaryMatch.Groups[1].Value.Trim();
                }
            }
        }
        
        return null;
    }
}
