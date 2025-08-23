using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSMOO.Functions;
using CSMOO.Verbs;
using CSMOO.Object;
using CSMOO.Logging;

namespace CSMOO.Parsing;

/// <summary>
/// Parses C# class definitions to extract function and verb definitions
/// </summary>
public static class CodeDefinitionParser
{
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
                    
                    var function = new FunctionDefinition
                    {
                        Name = method.Identifier.ValueText,
                        TargetClass = className,
                        Code = ExtractMethodBody(method),
                        Parameters = ExtractParameterTypes(method).ToArray(),
                        ParameterNames = ExtractParameterNames(method).ToArray(),
                        ReturnType = returnType,
                        Description = ExtractDocumentation(method) ?? $"Function {method.Identifier.ValueText} for {className}"
                    };
                    
                    functions.Add(function);
                    Logger.Debug($"Parsed function: {function.Name} with return type {function.ReturnType}");
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
                    var verb = new VerbDefinition
                    {
                        Name = methodName.ToLower(),
                        TargetClass = className,
                        Code = ExtractMethodBody(method),
                        Pattern = ExtractPatternFromAttributes(method) ?? GeneratePatternFromParameters(method),
                        Aliases = string.Join(" ", ExtractAliasesFromAttributes(method)),
                        Description = ExtractDescriptionFromAttributes(method) ?? ExtractDocumentation(method) ?? $"Verb {methodName} for {className}"
                    };
                    
                    verbs.Add(verb);
                    Logger.Debug($"Parsed verb: {verb.Name} for class {verb.TargetClass}");
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
                    // Only process public fields
                    if (!field.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                        continue;
                    
                    foreach (var variable in field.Declaration.Variables)
                    {
                        var property = new PropertyDefinition
                        {
                            Name = variable.Identifier.ValueText,
                            TargetClass = className,
                            Type = DeterminePropertyType(field.Declaration.Type),
                            Value = ExtractPropertyValue(variable),
                            Description = ExtractDocumentation(field) ?? $"Property {variable.Identifier.ValueText} for {className}"
                        };
                        
                        properties.Add(property);
                        Logger.Debug($"Parsed property: {property.Name} for class {property.TargetClass}");
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
        var documentationComment = method.GetLeadingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                               t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        
        if (documentationComment.IsKind(SyntaxKind.None))
            return null;
        
        var text = documentationComment.ToString();
        
        // Extract summary from XML documentation
        var summaryMatch = System.Text.RegularExpressions.Regex.Match(text, @"<summary>\s*(.*?)\s*</summary>", 
            System.Text.RegularExpressions.RegexOptions.Singleline);
        
        if (summaryMatch.Success)
        {
            return summaryMatch.Groups[1].Value.Trim()
                .Replace("///", "").Replace("//", "").Trim();
        }
        
        // Fallback to simple comment parsing
        return text.Replace("///", "").Replace("//", "").Trim();
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
                            Logger.Debug($"Extracted alias text: '{aliasText}' from attribute");
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
                            Logger.Debug($"Extracted pattern text: '{patternText}' from attribute");
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
