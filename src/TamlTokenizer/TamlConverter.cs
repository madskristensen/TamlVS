using System;
using System.Collections.Generic;
using System.Text;

namespace TamlTokenizer;

/// <summary>
/// Provides conversion methods between TAML and JSON formats.
/// </summary>
public static class TamlConverter
{
    /// <summary>
    /// Converts TAML source to JSON format.
    /// </summary>
    /// <param name="tamlSource">The TAML source text.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>The JSON representation of the TAML document.</returns>
    public static string ToJson(string tamlSource, bool indented = true)
    {
        if (tamlSource is null)
            throw new ArgumentNullException(nameof(tamlSource));

        TamlNode root = ParseToTree(tamlSource);
        var sb = new StringBuilder();
        WriteJsonObject(sb, root, indented ? 0 : -1);
        return sb.ToString();
    }

    /// <summary>
    /// Converts JSON source to TAML format.
    /// </summary>
    /// <param name="jsonSource">The JSON source text.</param>
    /// <returns>The TAML representation of the JSON document.</returns>
    public static string FromJson(string jsonSource)
    {
        if (jsonSource is null)
            throw new ArgumentNullException(nameof(jsonSource));

        var sb = new StringBuilder();
        TamlNode root = ParseJsonToTree(jsonSource);
        WriteTamlNode(sb, root, 0, isRoot: true);
        return sb.ToString();
    }

    /// <summary>
    /// Sorts keys alphabetically at each level of the TAML document.
    /// </summary>
    /// <param name="tamlSource">The TAML source text.</param>
    /// <returns>The TAML text with sorted keys.</returns>
    public static string SortKeys(string tamlSource)
    {
        if (tamlSource is null)
            throw new ArgumentNullException(nameof(tamlSource));

        TamlNode root = ParseToTree(tamlSource);
        SortNodeChildren(root);
        var sb = new StringBuilder();
        WriteTamlNode(sb, root, 0, isRoot: true);
        return sb.ToString();
    }

    private static void SortNodeChildren(TamlNode node)
    {
        if (node.Children.Count > 0)
        {
            node.Children.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
            foreach (TamlNode child in node.Children)
            {
                SortNodeChildren(child);
            }
        }
    }

    private static TamlNode ParseToTree(string tamlSource)
    {
        IReadOnlyList<TamlToken> tokens = Taml.GetTokens(tamlSource);
        var root = new TamlNode { Key = "", Value = null };
        var stack = new Stack<TamlNode>();
        stack.Push(root);

        var currentIndent = 0;
        TamlNode? currentNode = null;
        string? currentKey = null;
        var collectingValue = false;

        for (var i = 0; i < tokens.Count; i++)
        {
            TamlToken token = tokens[i];

            switch (token.Type)
            {
                case TamlTokenType.Indent:
                    currentIndent++;
                    if (currentNode != null)
                    {
                        stack.Push(currentNode);
                    }
                    break;

                case TamlTokenType.Dedent:
                    currentIndent--;
                    if (stack.Count > 1)
                    {
                        stack.Pop();
                    }
                    break;

                case TamlTokenType.Key:
                    currentKey = token.Value;
                    currentNode = new TamlNode { Key = currentKey };
                    stack.Peek().Children.Add(currentNode);
                    collectingValue = false;
                    break;

                case TamlTokenType.Tab:
                    if (currentKey != null)
                    {
                        collectingValue = true;
                    }
                    break;

                case TamlTokenType.Value:
                case TamlTokenType.Boolean:
                case TamlTokenType.Number:
                    if (collectingValue && currentNode != null)
                    {
                        currentNode.Value = token.Value;
                        currentNode.TokenType = token.Type;
                    }
                    break;

                case TamlTokenType.Null:
                    if (collectingValue && currentNode != null)
                    {
                        currentNode.Value = null;
                        currentNode.IsNull = true;
                        currentNode.TokenType = token.Type;
                    }
                    break;

                case TamlTokenType.EmptyString:
                    if (collectingValue && currentNode != null)
                    {
                        currentNode.Value = "";
                        currentNode.TokenType = token.Type;
                    }
                    break;

                case TamlTokenType.Newline:
                    currentKey = null;
                    collectingValue = false;
                    break;
            }
        }

        return root;
    }

    private static void WriteJsonObject(StringBuilder sb, TamlNode node, int indent)
    {
        var indented = indent >= 0;
        var indentStr = indented ? new string(' ', indent * 2) : "";
        var childIndentStr = indented ? new string(' ', (indent + 1) * 2) : "";
        var newline = indented ? "\n" : "";
        var space = indented ? " " : "";

        sb.Append('{');
        if (node.Children.Count > 0)
        {
            sb.Append(newline);
            for (var i = 0; i < node.Children.Count; i++)
            {
                TamlNode child = node.Children[i];
                sb.Append(childIndentStr);
                sb.Append('"');
                sb.Append(EscapeJsonString(child.Key ?? string.Empty));
                sb.Append("\":");
                sb.Append(space);

                if (child.Children.Count > 0)
                {
                    WriteJsonObject(sb, child, indented ? indent + 1 : -1);
                }
                else
                {
                    WriteJsonValue(sb, child);
                }

                if (i < node.Children.Count - 1)
                {
                    sb.Append(',');
                }
                sb.Append(newline);
            }
            sb.Append(indentStr);
        }
        sb.Append('}');
    }

    private static void WriteJsonValue(StringBuilder sb, TamlNode node)
    {
        if (node.IsNull)
        {
            sb.Append("null");
        }
        else if (node.Value == null)
        {
            // Parent node with no value but also no children - treat as null
            sb.Append("null");
        }
        else if (node.TokenType == TamlTokenType.Boolean)
        {
            sb.Append(node.Value); // true or false
        }
        else if (node.TokenType == TamlTokenType.Number)
        {
            sb.Append(node.Value);
        }
        else
        {
            sb.Append('"');
            sb.Append(EscapeJsonString(node.Value));
            sb.Append('"');
        }
    }

    private static string EscapeJsonString(string value)
    {
        var sb = new StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private static TamlNode ParseJsonToTree(string json)
    {
        var index = 0;
        SkipWhitespace(json, ref index);

        if (index >= json.Length || json[index] != '{')
        {
            throw new FormatException("JSON must start with an object '{'");
        }

        return ParseJsonObject(json, ref index);
    }

    private static TamlNode ParseJsonObject(string json, ref int index)
    {
        var node = new TamlNode { Key = "" };
        index++; // skip '{'
        SkipWhitespace(json, ref index);

        while (index < json.Length && json[index] != '}')
        {
            // Parse key
            if (json[index] != '"')
            {
                throw new FormatException($"Expected '\"' at position {index}");
            }
            var key = ParseJsonString(json, ref index);
            SkipWhitespace(json, ref index);

            if (index >= json.Length || json[index] != ':')
            {
                throw new FormatException($"Expected ':' at position {index}");
            }
            index++; // skip ':'
            SkipWhitespace(json, ref index);

            // Parse value
            TamlNode child = ParseJsonValue(json, ref index);
            child.Key = key;
            node.Children.Add(child);

            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ',')
            {
                index++;
                SkipWhitespace(json, ref index);
            }
        }

        if (index < json.Length)
        {
            index++; // skip '}'
        }

        return node;
    }

    private static TamlNode ParseJsonValue(string json, ref int index)
    {
        SkipWhitespace(json, ref index);

        if (index >= json.Length)
        {
            throw new FormatException("Unexpected end of JSON");
        }

        var c = json[index];

        if (c == '{')
        {
            return ParseJsonObject(json, ref index);
        }
        else if (c == '[')
        {
            // Arrays are not directly supported in TAML, convert to indexed children
            return ParseJsonArray(json, ref index);
        }
        else if (c == '"')
        {
            var value = ParseJsonString(json, ref index);
            return new TamlNode { Value = value, TokenType = TamlTokenType.Value };
        }
        else if (c == 't' || c == 'f')
        {
            var boolValue = ParseJsonBoolean(json, ref index);
            return new TamlNode { Value = boolValue, TokenType = TamlTokenType.Boolean };
        }
        else if (c == 'n')
        {
            ParseJsonNull(json, ref index);
            return new TamlNode { IsNull = true, TokenType = TamlTokenType.Null };
        }
        else if (c == '-' || char.IsDigit(c))
        {
            var numValue = ParseJsonNumber(json, ref index);
            return new TamlNode { Value = numValue, TokenType = TamlTokenType.Number };
        }
        else
        {
            throw new FormatException($"Unexpected character '{c}' at position {index}");
        }
    }

    private static TamlNode ParseJsonArray(string json, ref int index)
    {
        var node = new TamlNode();
        index++; // skip '['
        SkipWhitespace(json, ref index);

        var arrayIndex = 0;
        while (index < json.Length && json[index] != ']')
        {
            TamlNode child = ParseJsonValue(json, ref index);
            child.Key = arrayIndex.ToString();
            node.Children.Add(child);
            arrayIndex++;

            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ',')
            {
                index++;
                SkipWhitespace(json, ref index);
            }
        }

        if (index < json.Length)
        {
            index++; // skip ']'
        }

        return node;
    }

    private static string ParseJsonString(string json, ref int index)
    {
        index++; // skip opening '"'
        var sb = new StringBuilder();

        while (index < json.Length && json[index] != '"')
        {
            if (json[index] == '\\')
            {
                index++;
                if (index >= json.Length) break;

                switch (json[index])
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (index + 4 < json.Length)
                        {
                            var hex = json.Substring(index + 1, 4);
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            index += 4;
                        }
                        break;
                    default:
                        sb.Append(json[index]);
                        break;
                }
            }
            else
            {
                sb.Append(json[index]);
            }
            index++;
        }

        if (index < json.Length)
        {
            index++; // skip closing '"'
        }

        return sb.ToString();
    }

    private static string ParseJsonBoolean(string json, ref int index)
    {
        if (json.Substring(index).StartsWith("true"))
        {
            index += 4;
            return "true";
        }
        else if (json.Substring(index).StartsWith("false"))
        {
            index += 5;
            return "false";
        }
        throw new FormatException($"Invalid boolean at position {index}");
    }

    private static void ParseJsonNull(string json, ref int index)
    {
        if (json.Substring(index).StartsWith("null"))
        {
            index += 4;
        }
        else
        {
            throw new FormatException($"Invalid null at position {index}");
        }
    }

    private static string ParseJsonNumber(string json, ref int index)
    {
        var start = index;
        if (json[index] == '-') index++;

        while (index < json.Length && char.IsDigit(json[index])) index++;

        if (index < json.Length && json[index] == '.')
        {
            index++;
            while (index < json.Length && char.IsDigit(json[index])) index++;
        }

        if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
        {
            index++;
            if (index < json.Length && (json[index] == '+' || json[index] == '-')) index++;
            while (index < json.Length && char.IsDigit(json[index])) index++;
        }

        return json.Substring(start, index - start);
    }

    private static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
        {
            index++;
        }
    }

    private static void WriteTamlNode(StringBuilder sb, TamlNode node, int indent, bool isRoot)
    {
        var tabs = new string('\t', indent);

        foreach (TamlNode child in node.Children)
        {
            if (!isRoot || sb.Length > 0)
            {
                // Don't add newline before the very first entry
            }

            sb.Append(tabs);
            sb.Append(child.Key);

            if (child.Children.Count > 0)
            {
                sb.Append('\n');
                WriteTamlNode(sb, child, indent + 1, isRoot: false);
            }
            else
            {
                sb.Append('\t');
                if (child.IsNull)
                {
                    sb.Append('~');
                }
                else if (child.Value == "")
                {
                    sb.Append("\"\"");
                }
                else if (child.Value != null)
                {
                    sb.Append(child.Value);
                }
                else
                {
                    sb.Append('~');
                }
                sb.Append('\n');
            }
        }
    }

    private class TamlNode
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public bool IsNull { get; set; }
        public TamlTokenType TokenType { get; set; } = TamlTokenType.Value;
        public List<TamlNode> Children { get; } = [];
    }
}
