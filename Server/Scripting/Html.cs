using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Simplified HTML generation utilities for verb scripts
/// Provides an easy-to-use API for creating HTML content
/// </summary>
public static class Html
{
    /// <summary>
    /// Creates a new HTML document
    /// </summary>
    public static HtmlDocument NewDocument()
    {
        return new HtmlDocument();
    }

    /// <summary>
    /// Creates an HTML element with the specified tag name
    /// </summary>
    public static HtmlNode Element(string tagName, string? text = null)
    {
        var doc = new HtmlDocument();
        var element = doc.CreateElement(tagName);
        if (!string.IsNullOrEmpty(text))
        {
            element.InnerHtml = HtmlDocument.HtmlEncode(text);
        }
        return element;
    }

    /// <summary>
    /// Creates a div element with optional text content
    /// </summary>
    public static HtmlNode Div(string? text = null)
    {
        return Element("div", text);
    }

    /// <summary>
    /// Creates a span element with optional text content
    /// </summary>
    public static HtmlNode Span(string? text = null)
    {
        return Element("span", text);
    }

    /// <summary>
    /// Creates a paragraph element with optional text content
    /// </summary>
    public static HtmlNode P(string? text = null)
    {
        return Element("p", text);
    }

    /// <summary>
    /// Creates a heading element (h1-h6) with optional text content
    /// </summary>
    public static HtmlNode Heading(int level, string? text = null)
    {
        if (level < 1 || level > 6)
            throw new ArgumentException("Heading level must be between 1 and 6", nameof(level));
        
        return Element($"h{level}", text);
    }

    /// <summary>
    /// Creates an anchor (link) element
    /// </summary>
    public static HtmlNode Link(string href, string? text = null)
    {
        var element = Element("a", text);
        element.SetAttributeValue("href", href);
        return element;
    }

    /// <summary>
    /// Creates an image element
    /// </summary>
    public static HtmlNode Image(string src, string? alt = null)
    {
        var element = Element("img");
        element.SetAttributeValue("src", src);
        if (!string.IsNullOrEmpty(alt))
        {
            element.SetAttributeValue("alt", alt);
        }
        return element;
    }

    /// <summary>
    /// Creates a styled container with cyberpunk theme colors
    /// </summary>
    public static HtmlNode CyberpunkContainer(string? text = null)
    {
        var div = Div(text);
        div.SetAttributeValue("style", 
            "background: linear-gradient(135deg, #1a1a2e, #16213e, #0f3460); " +
            "color: #00ff88; " +
            "padding: 20px; " +
            "border: 2px solid #00ff88; " +
            "border-radius: 15px; " +
            "font-family: monospace;");
        return div;
    }

    /// <summary>
    /// Creates a highlighted command span
    /// </summary>
    public static HtmlNode Command(string commandText)
    {
        var span = Span(commandText);
        span.SetAttributeValue("style",
            "color: #00ff88; " +
            "font-weight: bold; " +
            "background: #003322; " +
            "padding: 2px 8px; " +
            "border-radius: 4px; " +
            "border: 1px solid #00ff88;");
        return span;
    }

    /// <summary>
    /// Creates a title element with glowing text effect
    /// </summary>
    public static HtmlNode GlowTitle(string text, string? color = null)
    {
        var div = Div(text);
        color ??= "#00ff88";
        div.SetAttributeValue("style",
            $"font-size: 3.5rem; " +
            $"font-weight: bold; " +
            $"color: {color}; " +
            $"text-shadow: 0 0 20px {color}; " +
            "text-align: center; " +
            "margin-bottom: 20px; " +
            "letter-spacing: 8px;");
        return div;
    }

    /// <summary>
    /// Utility class for building CSS styles
    /// </summary>
    public static class Style
    {
        public static string Build(params (string property, string value)[] styles)
        {
            return string.Join("; ", styles.Select(s => $"{s.property}: {s.value}"));
        }

        public static string Color(string color) => $"color: {color}";
        public static string Background(string color) => $"background: {color}";
        public static string FontSize(string size) => $"font-size: {size}";
        public static string Padding(string padding) => $"padding: {padding}";
        public static string Margin(string margin) => $"margin: {margin}";
        public static string Border(string border) => $"border: {border}";
        public static string BorderRadius(string radius) => $"border-radius: {radius}";
        public static string TextAlign(string align) => $"text-align: {align}";
        public static string FontFamily(string family) => $"font-family: {family}";
        public static string FontWeight(string weight) => $"font-weight: {weight}";
        public static string Display(string display) => $"display: {display}";
        public static string FlexDirection(string direction) => $"flex-direction: {direction}";
        public static string AlignItems(string align) => $"align-items: {align}";
        public static string JustifyContent(string justify) => $"justify-content: {justify}";

        /// <summary>
        /// Process LESS-like CSS with variables and basic nesting
        /// </summary>
        public static string ProcessLess(string lessCSS, Dictionary<string, string>? variables = null)
        {
            variables ??= new Dictionary<string, string>();
            
            // Replace variables (format: @variableName)
            foreach (var kvp in variables)
            {
                lessCSS = lessCSS.Replace($"@{kvp.Key}", kvp.Value);
            }
            
            // Process simple nesting (one level deep for MUD compatibility)
            lessCSS = ProcessSimpleNesting(lessCSS);
            
            // Remove extra whitespace and normalize
            lessCSS = Regex.Replace(lessCSS, @"\s+", " ").Trim();
            
            return lessCSS;
        }

        /// <summary>
        /// Create a LESS-style CSS builder with variables support
        /// </summary>
        public static LessBuilder Less()
        {
            return new LessBuilder();
        }

        private static string ProcessSimpleNesting(string css)
        {
            // Simple nesting processor - handles basic cases like:
            // .container { color: red; .nested { background: blue; } }
            // Converts to: .container { color: red; } .container .nested { background: blue; }
            
            var result = css;
            var nestedPattern = @"([^{}]+)\s*\{\s*([^{}]*?)\s*([^{}]+\s*\{[^{}]*\})\s*([^{}]*?)\s*\}";
            
            while (Regex.IsMatch(result, nestedPattern))
            {
                result = Regex.Replace(result, nestedPattern, match =>
                {
                    var parent = match.Groups[1].Value.Trim();
                    var parentProps = match.Groups[2].Value.Trim();
                    var nestedRule = match.Groups[3].Value.Trim();
                    var remainingProps = match.Groups[4].Value.Trim();
                    
                    // Extract nested selector and properties
                    var nestedMatch = Regex.Match(nestedRule, @"([^{]+)\s*\{\s*([^}]*)\s*\}");
                    if (nestedMatch.Success)
                    {
                        var nestedSelector = nestedMatch.Groups[1].Value.Trim();
                        var nestedProps = nestedMatch.Groups[2].Value.Trim();
                        
                        var parentRule = string.IsNullOrEmpty(parentProps + remainingProps) 
                            ? "" 
                            : $"{parent} {{ {parentProps} {remainingProps} }}";
                        var expandedNested = $"{parent} {nestedSelector} {{ {nestedProps} }}";
                        
                        return string.IsNullOrEmpty(parentRule) ? expandedNested : $"{parentRule} {expandedNested}";
                    }
                    
                    return match.Value;
                });
            }
            
            return result;
        }
    }

    /// <summary>
    /// LESS-style CSS builder with variables and nesting support
    /// </summary>
    public class LessBuilder
    {
        private readonly Dictionary<string, string> _variables = new();
        private readonly List<string> _rules = new();

        public LessBuilder Variable(string name, string value)
        {
            _variables[name] = value;
            return this;
        }

        public LessBuilder Rule(string selector, string properties)
        {
            _rules.Add($"{selector} {{ {properties} }}");
            return this;
        }

        public LessBuilder Rule(string selector, params (string property, string value)[] properties)
        {
            var props = string.Join("; ", properties.Select(p => $"{p.property}: {p.value}"));
            return Rule(selector, props);
        }

        public LessBuilder NestedRule(string parentSelector, string childSelector, string properties)
        {
            _rules.Add($"{parentSelector} {{ {childSelector} {{ {properties} }} }}");
            return this;
        }

        public LessBuilder NestedRule(string parentSelector, string childSelector, params (string property, string value)[] properties)
        {
            var props = string.Join("; ", properties.Select(p => $"{p.property}: {p.value}"));
            return NestedRule(parentSelector, childSelector, props);
        }

        public string Build()
        {
            var combined = string.Join(" ", _rules);
            return Style.ProcessLess(combined, _variables);
        }
    }

    /// <summary>
    /// Converts an HtmlNode to a string without line breaks (for MUD client compatibility)
    /// </summary>
    public static string ToSingleLine(this HtmlNode node)
    {
        return node.OuterHtml.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
    }

    /// <summary>
    /// Extension method to easily set multiple CSS properties
    /// </summary>
    public static HtmlNode WithStyle(this HtmlNode node, string style)
    {
        node.SetAttributeValue("style", style);
        return node;
    }

    /// <summary>
    /// Extension method to apply LESS-style CSS with variables and nesting
    /// </summary>
    public static HtmlNode WithLessStyle(this HtmlNode node, string lessCSS, Dictionary<string, string>? variables = null)
    {
        var processedCSS = Style.ProcessLess(lessCSS, variables);
        node.SetAttributeValue("style", processedCSS);
        return node;
    }

    /// <summary>
    /// Extension method to easily add CSS classes
    /// </summary>
    public static HtmlNode WithClass(this HtmlNode node, string className)
    {
        node.SetAttributeValue("class", className);
        return node;
    }

    /// <summary>
    /// Extension method to easily add attributes
    /// </summary>
    public static HtmlNode WithAttribute(this HtmlNode node, string name, string value)
    {
        node.SetAttributeValue(name, value);
        return node;
    }

    /// <summary>
    /// Extension method to append child elements
    /// </summary>
    public static HtmlNode AppendChild(this HtmlNode parent, HtmlNode child)
    {
        parent.AppendChild(child);
        return parent;
    }

    /// <summary>
    /// Extension method to append multiple child elements
    /// </summary>
    public static HtmlNode AppendChildren(this HtmlNode parent, params HtmlNode[] children)
    {
        foreach (var child in children)
        {
            parent.AppendChild(child);
        }
        return parent;
    }
}
