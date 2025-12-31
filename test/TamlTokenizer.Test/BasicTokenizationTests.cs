namespace TamlTokenizer.Test;

/// <summary>
/// Tests for basic TAML tokenization: key-value pairs, tabs, newlines.
/// Based on TAML spec: "Key and value separated by one or more tabs"
/// </summary>
[TestClass]
public sealed class BasicTokenizationTests
{
    [TestMethod]
    public void WhenKeyValuePairThenTokenizesCorrectly()
    {
        // Arrange
        var source = "key\tvalue";

        // Act
        TamlParseResult result = Taml.Tokenize(source);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(4, result.Tokens.Count); // Key, Tab, Value, EOF

        Assert.AreEqual(TamlTokenType.Key, result.Tokens[0].Type);
        Assert.AreEqual("key", result.Tokens[0].Value);

        Assert.AreEqual(TamlTokenType.Tab, result.Tokens[1].Type);

        Assert.AreEqual(TamlTokenType.Value, result.Tokens[2].Type);
        Assert.AreEqual("value", result.Tokens[2].Value);

        Assert.AreEqual(TamlTokenType.EndOfFile, result.Tokens[3].Type);
    }

    [TestMethod]
    public void WhenMultipleTabsThenAllowedForAlignment()
    {
        // TAML spec: "Multiple tabs can be used for visual alignment"
        var source = "key\t\t\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TamlTokenType.Key, result.Tokens[0].Type);
        Assert.AreEqual(TamlTokenType.Tab, result.Tokens[1].Type);
        Assert.AreEqual("\t\t\t", result.Tokens[1].Value); // 3 tabs
        Assert.AreEqual(TamlTokenType.Value, result.Tokens[2].Type);
    }

    [TestMethod]
    public void WhenMultipleLinesThenEachParsedSeparately()
    {
        var source = "name\tvalue\nage\t42";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(2, keys.Count);
        Assert.AreEqual("name", keys[0].Value);
        Assert.AreEqual("age", keys[1].Value);

        // "value" is a string Value, "42" is a Number
        TamlToken stringValue = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("value", stringValue.Value);

        TamlToken numberValue = result.Tokens.First(t => t.Type == TamlTokenType.Number);
        Assert.AreEqual("42", numberValue.Value);
    }

    [TestMethod]
    public void WhenValueContainsSpacesThenPreserved()
    {
        // TAML spec: Values are literal strings
        var source = "message\tHello World";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("Hello World", value.Value);
    }

    [TestMethod]
    public void WhenValueContainsNumbersThenPreserved()
    {
        var source = "port\t8080";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Number);
        Assert.AreEqual("8080", value.Value);
    }

    [TestMethod]
    public void WhenValueContainsBooleanThenClassifiedAsBoolean()
    {
        // Boolean values are now classified as Boolean type for syntax highlighting
        var source = "ssl\ttrue";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Boolean);
        Assert.AreEqual("true", value.Value);
        Assert.AreEqual(TamlTokenType.Boolean, value.Type);
    }

    [TestMethod]
    public void WhenValueIsFalseThenClassifiedAsBoolean()
    {
        var source = "enabled\tfalse";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Boolean);
        Assert.AreEqual("false", value.Value);
    }

    [TestMethod]
    public void WhenValueIsDecimalNumberThenClassifiedAsNumber()
    {
        var source = "price\t19.99";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Number);
        Assert.AreEqual("19.99", value.Value);
    }

    [TestMethod]
    public void WhenValueIsNegativeNumberThenClassifiedAsNumber()
    {
        var source = "offset\t-42";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Number);
        Assert.AreEqual("-42", value.Value);
    }

    [TestMethod]
    public void WhenValueLooksLikeBooleanButIsNotThenClassifiedAsValue()
    {
        // "TRUE" (uppercase) should remain a Value, not Boolean
        var source = "mode\tTRUE";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("TRUE", value.Value);
    }

    [TestMethod]
    public void WhenValueStartsWithNumberButIsNotNumericThenClassifiedAsValue()
    {
        // "123abc" is not a valid number
        var source = "id\t123abc";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        TamlToken value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("123abc", value.Value);
    }

    [TestMethod]
    public void WhenParentKeyWithoutValueThenTokenizedAsKeyOnly()
    {
        // TAML spec: "Keys with children have no value (just the key alone on the line)"
        var source = "server\n\thost\tlocalhost";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // First line: just "server" as a key with no value
        TamlToken firstKey = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.AreEqual("server", firstKey.Value);
    }

    [TestMethod]
    public void WhenEmptyInputThenReturnsOnlyEof()
    {
        TamlParseResult result = Taml.Tokenize("");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Tokens.Count);
        Assert.AreEqual(TamlTokenType.EndOfFile, result.Tokens[0].Type);
    }

    [TestMethod]
    public void WhenBlankLinesThenIgnored()
    {
        var source = "key1\tvalue1\n\nkey2\tvalue2";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(2, keys.Count);
    }

    [TestMethod]
    public void WhenKeyContainsHyphenThenAllowed()
    {
        // TAML spec example shows "user-authentication" as a value
        var source = "my-key\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("my-key", result.Tokens[0].Value);
    }

    [TestMethod]
    public void WhenKeyContainsUnderscoreThenAllowed()
    {
        var source = "my_key\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("my_key", result.Tokens[0].Value);
    }

    [TestMethod]
    public void WhenCrLfNewlineThenHandledCorrectly()
    {
        var source = "key1\tvalue1\r\nkey2\tvalue2";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(2, keys.Count);
    }

    [TestMethod]
    public void WhenPositionTrackingThenAccurate()
    {
        var source = "key\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        TamlToken keyToken = result.Tokens[0];
        Assert.AreEqual(1, keyToken.Line);
        Assert.AreEqual(1, keyToken.Column);
        Assert.AreEqual(0, keyToken.Position);
        Assert.AreEqual(3, keyToken.Length);

        TamlToken tabToken = result.Tokens[1];
        Assert.AreEqual(1, tabToken.Line);
        Assert.AreEqual(4, tabToken.Column);
        Assert.AreEqual(3, tabToken.Position);

        TamlToken valueToken = result.Tokens[2];
        Assert.AreEqual(1, valueToken.Line);
        Assert.AreEqual(5, valueToken.Column);
        Assert.AreEqual(4, valueToken.Position);
    }

    [TestMethod]
    public void WhenMultipleLinesThenLineNumbersCorrect()
    {
        var source = "line1\tvalue1\nline2\tvalue2\nline3\tvalue3";

        TamlParseResult result = Taml.Tokenize(source);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(1, keys[0].Line);
        Assert.AreEqual(2, keys[1].Line);
        Assert.AreEqual(3, keys[2].Line);
    }

    [TestMethod]
    public void WhenSpecExampleThenTokenizesCorrectly()
    {
        // From TAML spec example
        var source = """
            application	MyApp
            version	1.0.0
            author	Developer Name
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(3, keys.Count);
        Assert.AreEqual("application", keys[0].Value);
        Assert.AreEqual("version", keys[1].Value);
        Assert.AreEqual("author", keys[2].Value);

        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual("MyApp", values[0].Value);
        Assert.AreEqual("1.0.0", values[1].Value);
        Assert.AreEqual("Developer Name", values[2].Value);
    }
}
