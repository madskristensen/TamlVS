namespace TamlTokenizer.Test;

/// <summary>
/// Tests for TAML null (~) and empty string ("") handling.
/// Based on TAML spec: "Use ~ to represent null values. Use "" to represent empty strings."
/// </summary>
[TestClass]
public sealed class NullAndEmptyStringTests
{
    [TestMethod]
    public void WhenTildeThenTokenizedAsNull()
    {
        // TAML spec: "Use the tilde character ~ to represent a null value"
        var source = "key\t~";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var nullToken = result.Tokens.First(t => t.Type == TamlTokenType.Null);
        Assert.AreEqual("~", nullToken.Value);
    }

    [TestMethod]
    public void WhenDoubleQuotesThenTokenizedAsEmptyString()
    {
        // TAML spec: "Use two double-quote characters "" to represent an empty string"
        var source = "key\t\"\"";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var emptyToken = result.Tokens.First(t => t.Type == TamlTokenType.EmptyString);
        Assert.AreEqual("\"\"", emptyToken.Value);
    }

    [TestMethod]
    public void WhenNullAndEmptyStringThenDistinguished()
    {
        // TAML spec: "TAML distinguishes between null and empty string"
        var source = "nullable\t~\nempty\t\"\"";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var nullToken = result.Tokens.FirstOrDefault(t => t.Type == TamlTokenType.Null);
        var emptyToken = result.Tokens.FirstOrDefault(t => t.Type == TamlTokenType.EmptyString);

        Assert.IsNotNull(nullToken);
        Assert.IsNotNull(emptyToken);
        Assert.AreNotEqual(nullToken.Type, emptyToken.Type);
    }

    [TestMethod]
    public void WhenSpecExampleWithNullThenCorrect()
    {
        // From TAML spec: "license ~"
        var source = "license\t~";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TamlTokenType.Key, result.Tokens[0].Type);
        Assert.AreEqual("license", result.Tokens[0].Value);
        Assert.AreEqual(TamlTokenType.Null, result.Tokens[2].Type);
    }

    [TestMethod]
    public void WhenSpecExampleWithEmptyStringThenCorrect()
    {
        // From TAML spec: "empty_field """
        var source = "empty_field\t\"\"";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TamlTokenType.Key, result.Tokens[0].Type);
        Assert.AreEqual("empty_field", result.Tokens[0].Value);
        Assert.AreEqual(TamlTokenType.EmptyString, result.Tokens[2].Type);
    }

    [TestMethod]
    public void WhenNullAtEndOfLineThenRecognized()
    {
        var source = "password\t~\nusername\tadmin";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var nullToken = result.Tokens.First(t => t.Type == TamlTokenType.Null);
        Assert.AreEqual(1, nullToken.Line);
    }

    [TestMethod]
    public void WhenEmptyStringAtEndOfLineThenRecognized()
    {
        var source = "nickname\t\"\"\nbio\tHello";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var emptyToken = result.Tokens.First(t => t.Type == TamlTokenType.EmptyString);
        Assert.AreEqual(1, emptyToken.Line);
    }

    [TestMethod]
    public void WhenTildeInMiddleOfValueThenNotNull()
    {
        // Tilde should only be null when it's the entire value
        var source = "path\t~/home/user";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var valueToken = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("~/home/user", valueToken.Value);
    }

    [TestMethod]
    public void WhenSingleQuoteThenNotEmptyString()
    {
        // Only "" is empty string, not single quote
        var source = "key\t\"";

        var result = Taml.Tokenize(source);

        // Should be treated as a regular value, not empty string
        var valueToken = result.Tokens.FirstOrDefault(t => t.Type == TamlTokenType.Value);
        Assert.IsNotNull(valueToken);
        Assert.AreEqual("\"", valueToken.Value);
    }

    [TestMethod]
    public void WhenQuotedStringWithContentThenNotEmptyString()
    {
        // "value" is not the same as "" - TAML doesn't use quotes for non-empty strings
        var source = "key\t\"value\"";

        var result = Taml.Tokenize(source);

        // Should be treated as a regular value including quotes
        var valueToken = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("\"value\"", valueToken.Value);
    }

    [TestMethod]
    public void WhenMultipleNullValuesThenAllRecognized()
    {
        var source = "a\t~\nb\t~\nc\t~";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var nullTokens = result.Tokens.Where(t => t.Type == TamlTokenType.Null).ToList();
        Assert.AreEqual(3, nullTokens.Count);
    }

    [TestMethod]
    public void WhenMixedValuesFromSpecThenCorrect()
    {
        // From TAML spec validation example
        var source = """
            username	alice
            password	~
            nickname	""
            bio	Hello world
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var values = result.Tokens.Where(t =>
            t.Type == TamlTokenType.Value ||
            t.Type == TamlTokenType.Null ||
            t.Type == TamlTokenType.EmptyString).ToList();

        Assert.AreEqual(4, values.Count);
        Assert.AreEqual("alice", values[0].Value);
        Assert.AreEqual(TamlTokenType.Null, values[1].Type);
        Assert.AreEqual(TamlTokenType.EmptyString, values[2].Type);
        Assert.AreEqual("Hello world", values[3].Value);
    }

    [TestMethod]
    public void WhenNullFollowedByCommentThenStillRecognized()
    {
        var source = "key\t~\n# This is a comment";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Null));
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Comment));
    }
}
