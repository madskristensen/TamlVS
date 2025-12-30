namespace TamlTokenizer.Test;

/// <summary>
/// Tests for TAML indentation and nested structures.
/// Based on TAML spec: "One tab character = one level of nesting"
/// </summary>
[TestClass]
public sealed class IndentationTests
{
    [TestMethod]
    public void WhenNestedStructureThenIndentTokenEmitted()
    {
        // TAML spec: "Children are indented with tabs"
        var source = "parent\n\tchild\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Indent));
    }

    [TestMethod]
    public void WhenDedentingThenDedentTokenEmitted()
    {
        var source = "parent\n\tchild\tvalue\nsibling\tvalue2";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Dedent));
    }

    [TestMethod]
    public void WhenMultipleLevelsThenCorrectIndentCount()
    {
        var source = """
            root
            	level1
            		level2
            			level3	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var indents = result.Tokens.Where(t => t.Type == TamlTokenType.Indent).ToList();
        Assert.AreEqual(3, indents.Count); // 3 indentation increases
    }

    [TestMethod]
    public void WhenReturningToRootThenMultipleDedents()
    {
        var source = """
            root
            	level1
            		level2	value
            back_at_root	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Should have 2 dedents to go from level2 back to root
        var dedents = result.Tokens.Where(t => t.Type == TamlTokenType.Dedent).ToList();
        Assert.AreEqual(2, dedents.Count);
    }

    [TestMethod]
    public void WhenSpecNestedExampleThenCorrect()
    {
        // From TAML spec
        var source = """
            parent
            	child	value
            	another_child	value
            	nested
            		deeper	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(5, keys.Count);
        Assert.AreEqual("parent", keys[0].Value);
        Assert.AreEqual("child", keys[1].Value);
        Assert.AreEqual("another_child", keys[2].Value);
        Assert.AreEqual("nested", keys[3].Value);
        Assert.AreEqual("deeper", keys[4].Value);
    }

    [TestMethod]
    public void WhenConfigExampleThenCorrect()
    {
        // From TAML spec example
        var source = """
            config
            	database
            		host	localhost
            		port	5432
            		credentials
            			username	admin
            			password	secret
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(7, keys.Count);

        // Verify hierarchy through line positions
        Assert.AreEqual(1, keys[0].Line); // config
        Assert.AreEqual(2, keys[1].Line); // database
        Assert.AreEqual(3, keys[2].Line); // host
        Assert.AreEqual(4, keys[3].Line); // port
        Assert.AreEqual(5, keys[4].Line); // credentials
        Assert.AreEqual(6, keys[5].Line); // username
        Assert.AreEqual(7, keys[6].Line); // password
    }

    [TestMethod]
    public void WhenEndOfFileThenRemainingDedentsEmitted()
    {
        var source = "root\n\tchild\n\t\tgrandchild\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // At EOF, should emit dedents to close all open levels
        var dedents = result.Tokens.Where(t => t.Type == TamlTokenType.Dedent).ToList();
        Assert.IsTrue(dedents.Count >= 2);
    }

    [TestMethod]
    public void WhenSameLevelThenNoIndentOrDedent()
    {
        var source = """
            key1	value1
            key2	value2
            key3	value3
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // No indentation changes, all at root level
        var indents = result.Tokens.Where(t => t.Type == TamlTokenType.Indent).ToList();
        var dedents = result.Tokens.Where(t => t.Type == TamlTokenType.Dedent).ToList();

        Assert.AreEqual(0, indents.Count);
        Assert.AreEqual(0, dedents.Count);
    }

    [TestMethod]
    public void WhenListItemsThenIndentedFromParent()
    {
        // TAML spec: "List items are just values indented one tab from their parent"
        var source = """
            items
            	first item
            	second item
            	third item
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Should have one indent after "items"
        var indents = result.Tokens.Where(t => t.Type == TamlTokenType.Indent).ToList();
        Assert.AreEqual(1, indents.Count);
    }

    [TestMethod]
    public void WhenEnvironmentsExampleThenCorrect()
    {
        // From TAML spec example
        var source = """
            environments
            	development
            		debug	true
            		log_level	verbose
            	production
            		debug	false
            		log_level	error
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(7, keys.Count);
    }

    [TestMethod]
    public void WhenIndentAfterParentKeyThenCorrect()
    {
        // TAML spec: "If a key has children, it has no value on the same line"
        var source = """
            server
            	host	localhost
            	port	8080
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // "server" should be a key with no value on same line
        var serverKey = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.AreEqual("server", serverKey.Value);

        // Next non-whitespace should be newline, then indent, then child key
        int serverIndex = result.Tokens.ToList().IndexOf(serverKey);
        var afterServer = result.Tokens.Skip(serverIndex + 1)
            .Where(t => t.Type != TamlTokenType.Whitespace)
            .First();
        Assert.AreEqual(TamlTokenType.Newline, afterServer.Type);
    }

    [TestMethod]
    public void WhenDeeplyNestedThenAllLevelsTracked()
    {
        var source = """
            a
            	b
            		c
            			d
            				e	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var indents = result.Tokens.Where(t => t.Type == TamlTokenType.Indent).ToList();
        Assert.AreEqual(4, indents.Count); // 4 levels of indentation
    }

    [TestMethod]
    public void WhenPartialDedentThenCorrectLevel()
    {
        var source = """
            level0
            	level1
            		level2	value
            	back_to_level1	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Should have 2 indents, 1 dedent (from level2 to level1)
        var indents = result.Tokens.Where(t => t.Type == TamlTokenType.Indent).ToList();
        var dedents = result.Tokens.Where(t => t.Type == TamlTokenType.Dedent).ToList();

        Assert.AreEqual(2, indents.Count);
        Assert.IsTrue(dedents.Count >= 1);
    }
}
