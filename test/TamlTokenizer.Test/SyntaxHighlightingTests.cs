namespace TamlTokenizer.Test;

/// <summary>
/// Tests for syntax highlighting extension methods.
/// These are critical for VS editor extension integration.
/// </summary>
[TestClass]
public sealed class SyntaxHighlightingTests
{
    [TestMethod]
    public void WhenGetTokensOnLineThenReturnsCorrect()
    {
        var source = """
            line1	value1
            line2	value2
            line3	value3
            """;

        var result = Taml.Tokenize(source);
        var line2Tokens = result.Tokens.GetTokensOnLine(2);

        Assert.IsTrue(line2Tokens.Count > 0);
        Assert.IsTrue(line2Tokens.All(t => t.Line == 2));
    }

    [TestMethod]
    public void WhenGetTokensInRangeThenReturnsCorrect()
    {
        var source = """
            line1	value1
            line2	value2
            line3	value3
            line4	value4
            """;

        var result = Taml.Tokenize(source);
        var rangeTokens = result.Tokens.GetTokensInRange(2, 3);

        Assert.IsTrue(rangeTokens.Count > 0);
        Assert.IsTrue(rangeTokens.All(t => t.Line >= 2 && t.Line <= 3));
    }

    [TestMethod]
    public void WhenGetTokenAtPositionThenReturnsCorrect()
    {
        var source = "key\tvalue";

        var result = Taml.Tokenize(source);

        // Get token at key position
        var keyToken = result.Tokens.GetTokenAt(1, 1);
        Assert.IsNotNull(keyToken);
        Assert.AreEqual(TamlTokenType.Key, keyToken.Type);

        // Get token at value position
        var valueToken = result.Tokens.GetTokenAt(1, 5);
        Assert.IsNotNull(valueToken);
        Assert.AreEqual(TamlTokenType.Value, valueToken.Type);
    }

    [TestMethod]
    public void WhenGetTokenAtInvalidPositionThenReturnsNull()
    {
        var source = "key\tvalue";

        var result = Taml.Tokenize(source);
        var token = result.Tokens.GetTokenAt(100, 100);

        Assert.IsNull(token);
    }

    [TestMethod]
    public void WhenGetTokensByTypeThenFiltersCorrectly()
    {
        var source = """
            key1	value1
            key2	value2
            # comment
            key3	value3
            """;

        var result = Taml.Tokenize(source);

        var keys = result.Tokens.GetTokensByType(TamlTokenType.Key);
        Assert.AreEqual(3, keys.Count);

        var comments = result.Tokens.GetTokensByType(TamlTokenType.Comment);
        Assert.AreEqual(1, comments.Count);
    }

    [TestMethod]
    public void WhenGetKeysThenReturnsAllKeys()
    {
        var source = """
            key1	value1
            key2	value2
            key3	value3
            """;

        var result = Taml.Tokenize(source);
        var keys = result.Tokens.GetKeys();

        Assert.AreEqual(3, keys.Count);
        Assert.IsTrue(keys.All(k => k.Type == TamlTokenType.Key));
    }

    [TestMethod]
    public void WhenGetValuesThenReturnsAllValues()
    {
        var source = """
            key1	value1
            key2	~
            key3	""
            key4	value4
            """;

        var result = Taml.Tokenize(source);
        var values = result.Tokens.GetValues();

        Assert.AreEqual(4, values.Count);
        Assert.IsTrue(values.Any(v => v.Type == TamlTokenType.Value));
        Assert.IsTrue(values.Any(v => v.Type == TamlTokenType.Null));
        Assert.IsTrue(values.Any(v => v.Type == TamlTokenType.EmptyString));
    }

    [TestMethod]
    public void WhenGetCommentsThenReturnsAllComments()
    {
        var source = """
            # Comment 1
            key	value
            # Comment 2
            """;

        var result = Taml.Tokenize(source);
        var comments = result.Tokens.GetComments();

        Assert.AreEqual(2, comments.Count);
        Assert.IsTrue(comments.All(c => c.Type == TamlTokenType.Comment));
    }

    [TestMethod]
    public void WhenIsKeywordThenCorrect()
    {
        var source = "key\t~";

        var result = Taml.Tokenize(source);

        var nullToken = result.Tokens.First(t => t.Type == TamlTokenType.Null);
        Assert.IsTrue(nullToken.IsKeyword());

        var keyToken = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.IsFalse(keyToken.IsKeyword());
    }

    [TestMethod]
    public void WhenIsStructuralThenCorrect()
    {
        var source = "key\tvalue\n\tchild\tvalue";

        var result = Taml.Tokenize(source);

        var tabToken = result.Tokens.First(t => t.Type == TamlTokenType.Tab);
        Assert.IsTrue(tabToken.IsStructural());

        var newlineToken = result.Tokens.First(t => t.Type == TamlTokenType.Newline);
        Assert.IsTrue(newlineToken.IsStructural());

        var keyToken = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.IsFalse(keyToken.IsStructural());
    }

    [TestMethod]
    public void WhenIsValueThenCorrect()
    {
        var source = """
            key1	value
            key2	~
            key3	""
            """;

        var result = Taml.Tokenize(source);

        var valueToken = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.IsTrue(valueToken.IsValue());

        var nullToken = result.Tokens.First(t => t.Type == TamlTokenType.Null);
        Assert.IsTrue(nullToken.IsValue());

        var emptyToken = result.Tokens.First(t => t.Type == TamlTokenType.EmptyString);
        Assert.IsTrue(emptyToken.IsValue());

        var keyToken = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.IsFalse(keyToken.IsValue());
    }

    [TestMethod]
    public void WhenGetErrorOnLineThenReturnsCorrect()
    {
        var source = "valid\tvalue\n\t\t\tskipped";

        var result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var errorLine = result.Errors[0].Line;
            var error = result.Errors.GetErrorOnLine(errorLine);
            Assert.IsNotNull(error);
        }
    }

    [TestMethod]
    public void WhenGetErrorsInRangeThenReturnsCorrect()
    {
        var source = "a\n\t\t\tb\nc\n\t\t\td"; // Errors on lines 2 and 4

        var result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var rangeErrors = result.Errors.GetErrorsInRange(1, 5);
            Assert.IsTrue(rangeErrors.Count > 0);
        }
    }

    [TestMethod]
    public void WhenTokenPositionWithinBoundsThenGetTokenAtWorks()
    {
        var source = "longkeyname\tlongvaluename";

        var result = Taml.Tokenize(source);

        // Middle of key
        var midKey = result.Tokens.GetTokenAt(1, 5);
        Assert.IsNotNull(midKey);
        Assert.AreEqual(TamlTokenType.Key, midKey.Type);

        // Middle of value
        var midValue = result.Tokens.GetTokenAt(1, 15);
        Assert.IsNotNull(midValue);
        Assert.AreEqual(TamlTokenType.Value, midValue.Type);
    }

    [TestMethod]
    public void WhenMultiLineDocumentThenLineBasedQueriesWork()
    {
        var source = """
            # Header comment
            app	MyApp
            server
            	host	localhost
            	port	8080
            # Footer
            """;

        var result = Taml.Tokenize(source);

        // Line 1 should have comment
        var line1 = result.Tokens.GetTokensOnLine(1);
        Assert.IsTrue(line1.Any(t => t.Type == TamlTokenType.Comment));

        // Line 2 should have key and value
        var line2 = result.Tokens.GetTokensOnLine(2);
        Assert.IsTrue(line2.Any(t => t.Type == TamlTokenType.Key));
        Assert.IsTrue(line2.Any(t => t.Type == TamlTokenType.Value));

        // Line 4 should have indented content
        var line4 = result.Tokens.GetTokensOnLine(4);
        Assert.IsTrue(line4.Any(t => t.Type == TamlTokenType.Key && t.Value == "host"));
    }

    [TestMethod]
    public void WhenEmptyLineThenGetTokensReturnsNewlineOnly()
    {
        var source = "key\tvalue\n\nother\tvalue";

        var result = Taml.Tokenize(source);

        // Line 2 is empty, should have minimal or no content tokens
        var line2 = result.Tokens.GetTokensOnLine(2);
        Assert.IsTrue(line2.Count == 0 || line2.All(t =>
            t.Type == TamlTokenType.Newline ||
            t.Type == TamlTokenType.Whitespace));
    }

    [TestMethod]
    public void WhenTokenHasPositionInfoThenIdeCanHighlight()
    {
        var source = "key\tvalue";

        var result = Taml.Tokenize(source);

        foreach (var token in result.Tokens.Where(t => t.Type != TamlTokenType.EndOfFile))
        {
            // Every token should have valid position info for IDE highlighting
            Assert.IsTrue(token.Line > 0);
            Assert.IsTrue(token.Column > 0);
            Assert.IsTrue(token.Position >= 0);
            Assert.IsTrue(token.Length >= 0);
            Assert.AreEqual(token.Position + token.Length, token.EndPosition);
        }
    }
}
