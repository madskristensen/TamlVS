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
        var result = Taml.Tokenize(source);

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

        var result = Taml.Tokenize(source);

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

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(2, keys.Count);
        Assert.AreEqual("name", keys[0].Value);
        Assert.AreEqual("age", keys[1].Value);

        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(2, values.Count);
        Assert.AreEqual("value", values[0].Value);
        Assert.AreEqual("42", values[1].Value);
    }

    [TestMethod]
    public void WhenValueContainsSpacesThenPreserved()
    {
        // TAML spec: Values are literal strings
        var source = "message\tHello World";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("Hello World", value.Value);
    }

    [TestMethod]
    public void WhenValueContainsNumbersThenPreserved()
    {
        var source = "port\t8080";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("8080", value.Value);
    }

    [TestMethod]
    public void WhenValueContainsBooleanThenPreservedAsString()
    {
        // TAML spec: "All values are strings by default"
        var source = "ssl\ttrue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("true", value.Value);
        Assert.AreEqual(TamlTokenType.Value, value.Type); // Not a special Boolean type
    }

    [TestMethod]
    public void WhenParentKeyWithoutValueThenTokenizedAsKeyOnly()
    {
        // TAML spec: "Keys with children have no value (just the key alone on the line)"
        var source = "server\n\thost\tlocalhost";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // First line: just "server" as a key with no value
        var firstKey = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.AreEqual("server", firstKey.Value);
    }

    [TestMethod]
    public void WhenEmptyInputThenReturnsOnlyEof()
    {
        var result = Taml.Tokenize("");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Tokens.Count);
        Assert.AreEqual(TamlTokenType.EndOfFile, result.Tokens[0].Type);
    }

    [TestMethod]
    public void WhenBlankLinesThenIgnored()
    {
        var source = "key1\tvalue1\n\nkey2\tvalue2";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(2, keys.Count);
    }

    [TestMethod]
    public void WhenKeyContainsHyphenThenAllowed()
    {
        // TAML spec example shows "user-authentication" as a value
        var source = "my-key\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("my-key", result.Tokens[0].Value);
    }

    [TestMethod]
    public void WhenKeyContainsUnderscoreThenAllowed()
    {
        var source = "my_key\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("my_key", result.Tokens[0].Value);
    }

    [TestMethod]
    public void WhenCrLfNewlineThenHandledCorrectly()
    {
        var source = "key1\tvalue1\r\nkey2\tvalue2";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(2, keys.Count);
    }

    [TestMethod]
    public void WhenPositionTrackingThenAccurate()
    {
        var source = "key\tvalue";

        var result = Taml.Tokenize(source);

        var keyToken = result.Tokens[0];
        Assert.AreEqual(1, keyToken.Line);
        Assert.AreEqual(1, keyToken.Column);
        Assert.AreEqual(0, keyToken.Position);
        Assert.AreEqual(3, keyToken.Length);

        var tabToken = result.Tokens[1];
        Assert.AreEqual(1, tabToken.Line);
        Assert.AreEqual(4, tabToken.Column);
        Assert.AreEqual(3, tabToken.Position);

        var valueToken = result.Tokens[2];
        Assert.AreEqual(1, valueToken.Line);
        Assert.AreEqual(5, valueToken.Column);
        Assert.AreEqual(4, valueToken.Position);
    }

    [TestMethod]
    public void WhenMultipleLinesThenLineNumbersCorrect()
    {
        var source = "line1\tvalue1\nline2\tvalue2\nline3\tvalue3";

        var result = Taml.Tokenize(source);

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

        var result = Taml.Tokenize(source);

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
