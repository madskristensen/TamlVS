namespace TamlTokenizer.Test;

/// <summary>
/// Tests for TAML comments.
/// Based on TAML spec: "Lines starting with # are ignored. Mid-line comments are not supported."
/// </summary>
[TestClass]
public sealed class CommentTests
{
    [TestMethod]
    public void WhenLineStartsWithHashThenComment()
    {
        var source = "# This is a comment";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comment = result.Tokens.First(t => t.Type == TamlTokenType.Comment);
        Assert.AreEqual("# This is a comment", comment.Value);
    }

    [TestMethod]
    public void WhenCommentOnOwnLineThenIgnored()
    {
        var source = """
            # Comment line
            key	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Comment));
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Key && t.Value == "key"));
    }

    [TestMethod]
    public void WhenMultipleCommentsThenAllRecognized()
    {
        var source = """
            # Comment 1
            key1	value1
            # Comment 2
            key2	value2
            # Comment 3
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comments = result.Tokens.Where(t => t.Type == TamlTokenType.Comment).ToList();
        Assert.AreEqual(3, comments.Count);
    }

    [TestMethod]
    public void WhenHashInValueThenNotComment()
    {
        // TAML spec: "# characters within keys or values are treated as literal characters"
        var source = "color\t#FF0000";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("#FF0000", value.Value);
    }

    [TestMethod]
    public void WhenHashInKeyThenNotComment()
    {
        // TAML spec: "# characters within keys or values are treated as literal characters"
        var source = "key#1\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var key = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.AreEqual("key#1", key.Value);
    }

    [TestMethod]
    public void WhenCommentAtEndOfFileThenRecognized()
    {
        var source = """
            key	value
            # Final comment
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comment = result.Tokens.First(t => t.Type == TamlTokenType.Comment);
        Assert.IsTrue(comment.Value.Contains("Final comment"));
    }

    [TestMethod]
    public void WhenCommentPreservesContentThenCorrect()
    {
        var source = "# TAML Example";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comment = result.Tokens.First(t => t.Type == TamlTokenType.Comment);
        Assert.AreEqual("# TAML Example", comment.Value);
    }

    [TestMethod]
    public void WhenIndentedCommentThenStillComment()
    {
        var source = """
            parent
            	# Indented comment
            	child	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comment = result.Tokens.FirstOrDefault(t => t.Type == TamlTokenType.Comment);
        Assert.IsNotNull(comment);
    }

    [TestMethod]
    public void WhenSpecExampleCommentThenCorrect()
    {
        // From TAML spec example document
        var source = """
            # TAML Example
            application	MyApp
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Comment));
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Key && t.Value == "application"));
    }

    [TestMethod]
    public void WhenCommentPositionThenCorrect()
    {
        var source = "# Comment\nkey\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comment = result.Tokens.First(t => t.Type == TamlTokenType.Comment);
        Assert.AreEqual(1, comment.Line);
        Assert.AreEqual(1, comment.Column);
    }

    [TestMethod]
    public void WhenEmptyCommentThenStillValid()
    {
        var source = "#\nkey\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comment = result.Tokens.First(t => t.Type == TamlTokenType.Comment);
        Assert.AreEqual("#", comment.Value);
    }

    [TestMethod]
    public void WhenCommentOnlyDocumentThenValid()
    {
        var source = """
            # Line 1
            # Line 2
            # Line 3
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comments = result.Tokens.Where(t => t.Type == TamlTokenType.Comment).ToList();
        Assert.AreEqual(3, comments.Count);
    }

    [TestMethod]
    public void WhenCommentWithSpecialCharactersThenPreserved()
    {
        var source = "# Special chars: !@#$%^&*()";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comment = result.Tokens.First(t => t.Type == TamlTokenType.Comment);
        Assert.IsTrue(comment.Value.Contains("!@#$%^&*()"));
    }

    [TestMethod]
    public void WhenCommentBetweenNestedItemsThenHandled()
    {
        var source = """
            server
            	# Server settings
            	host	localhost
            	# Port configuration
            	port	8080
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var comments = result.Tokens.Where(t => t.Type == TamlTokenType.Comment).ToList();
        Assert.AreEqual(2, comments.Count);
    }
}
