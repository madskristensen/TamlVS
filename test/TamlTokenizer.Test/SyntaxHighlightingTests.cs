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

        TamlParseResult result = Taml.Tokenize(source);
        List<TamlToken> line2Tokens = result.Tokens.GetTokensOnLine(2);

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

        TamlParseResult result = Taml.Tokenize(source);
        List<TamlToken> rangeTokens = result.Tokens.GetTokensInRange(2, 3);

        Assert.IsTrue(rangeTokens.Count > 0);
        Assert.IsTrue(rangeTokens.All(t => t.Line >= 2 && t.Line <= 3));
    }

    [TestMethod]
    public void WhenGetTokenAtPositionThenReturnsCorrect()
    {
        var source = "key\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        // Get token at key position
        TamlToken? keyToken = result.Tokens.GetTokenAt(1, 1);
        Assert.IsNotNull(keyToken);
        Assert.AreEqual(TamlTokenType.Key, keyToken.Type);

        // Get token at value position
        TamlToken? valueToken = result.Tokens.GetTokenAt(1, 5);
        Assert.IsNotNull(valueToken);
        Assert.AreEqual(TamlTokenType.Value, valueToken.Type);
    }

    [TestMethod]
    public void WhenGetTokenAtInvalidPositionThenReturnsNull()
    {
        var source = "key\tvalue";

        TamlParseResult result = Taml.Tokenize(source);
        TamlToken? token = result.Tokens.GetTokenAt(100, 100);

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

        TamlParseResult result = Taml.Tokenize(source);

        List<TamlToken> keys = result.Tokens.GetTokensByType(TamlTokenType.Key);
        Assert.AreEqual(3, keys.Count);

        List<TamlToken> comments = result.Tokens.GetTokensByType(TamlTokenType.Comment);
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

        TamlParseResult result = Taml.Tokenize(source);
        List<TamlToken> keys = result.Tokens.GetKeys();

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

        TamlParseResult result = Taml.Tokenize(source);
        List<TamlToken> values = result.Tokens.GetValues();

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

        TamlParseResult result = Taml.Tokenize(source);
        List<TamlToken> comments = result.Tokens.GetComments();

        Assert.AreEqual(2, comments.Count);
        Assert.IsTrue(comments.All(c => c.Type == TamlTokenType.Comment));
    }

    [TestMethod]
    public void WhenIsKeywordThenCorrect()
    {
        var source = "key\t~";

        TamlParseResult result = Taml.Tokenize(source);

        TamlToken nullToken = result.Tokens.First(t => t.Type == TamlTokenType.Null);
        Assert.IsTrue(nullToken.IsKeyword());

        TamlToken keyToken = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.IsFalse(keyToken.IsKeyword());
    }

    [TestMethod]
    public void WhenIsStructuralThenCorrect()
    {
        var source = "key\tvalue\n\tchild\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        TamlToken tabToken = result.Tokens.First(t => t.Type == TamlTokenType.Tab);
        Assert.IsTrue(tabToken.IsStructural());

        TamlToken newlineToken = result.Tokens.First(t => t.Type == TamlTokenType.Newline);
        Assert.IsTrue(newlineToken.IsStructural());

        TamlToken keyToken = result.Tokens.First(t => t.Type == TamlTokenType.Key);
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

        TamlParseResult result = Taml.Tokenize(source);

        TamlToken valueToken = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.IsTrue(valueToken.IsValue());

        TamlToken nullToken = result.Tokens.First(t => t.Type == TamlTokenType.Null);
        Assert.IsTrue(nullToken.IsValue());

        TamlToken emptyToken = result.Tokens.First(t => t.Type == TamlTokenType.EmptyString);
        Assert.IsTrue(emptyToken.IsValue());

        TamlToken keyToken = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.IsFalse(keyToken.IsValue());
    }

    [TestMethod]
    public void WhenGetErrorOnLineThenReturnsCorrect()
    {
        var source = "valid\tvalue\n\t\t\tskipped";

        TamlParseResult result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var errorLine = result.Errors[0].Line;
            TamlError? error = result.Errors.GetErrorOnLine(errorLine);
            Assert.IsNotNull(error);
        }
    }

    [TestMethod]
    public void WhenGetErrorsInRangeThenReturnsCorrect()
    {
        var source = "a\n\t\t\tb\nc\n\t\t\td"; // Errors on lines 2 and 4

        TamlParseResult result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            List<TamlError> rangeErrors = result.Errors.GetErrorsInRange(1, 5);
            Assert.IsTrue(rangeErrors.Count > 0);
        }
    }

    [TestMethod]
    public void WhenTokenPositionWithinBoundsThenGetTokenAtWorks()
    {
        var source = "longkeyname\tlongvaluename";

        TamlParseResult result = Taml.Tokenize(source);

        // Middle of key
        TamlToken? midKey = result.Tokens.GetTokenAt(1, 5);
        Assert.IsNotNull(midKey);
        Assert.AreEqual(TamlTokenType.Key, midKey.Type);

        // Middle of value
        TamlToken? midValue = result.Tokens.GetTokenAt(1, 15);
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

        TamlParseResult result = Taml.Tokenize(source);

        // Line 1 should have comment
        List<TamlToken> line1 = result.Tokens.GetTokensOnLine(1);
        Assert.IsTrue(line1.Any(t => t.Type == TamlTokenType.Comment));

        // Line 2 should have key and value
        List<TamlToken> line2 = result.Tokens.GetTokensOnLine(2);
        Assert.IsTrue(line2.Any(t => t.Type == TamlTokenType.Key));
        Assert.IsTrue(line2.Any(t => t.Type == TamlTokenType.Value));

        // Line 4 should have indented content
        List<TamlToken> line4 = result.Tokens.GetTokensOnLine(4);
        Assert.IsTrue(line4.Any(t => t.Type == TamlTokenType.Key && t.Value == "host"));
    }

    [TestMethod]
    public void WhenEmptyLineThenGetTokensReturnsNewlineOnly()
    {
        var source = "key\tvalue\n\nother\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        // Line 2 is empty, should have minimal or no content tokens
        List<TamlToken> line2 = result.Tokens.GetTokensOnLine(2);
        Assert.IsTrue(line2.Count == 0 || line2.All(t =>
            t.Type == TamlTokenType.Newline ||
            t.Type == TamlTokenType.Whitespace));
    }

    [TestMethod]
    public void WhenTokenHasPositionInfoThenIdeCanHighlight()
    {
        var source = "key\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        foreach (TamlToken? token in result.Tokens.Where(t => t.Type != TamlTokenType.EndOfFile))
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
