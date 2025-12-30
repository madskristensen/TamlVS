namespace TamlTokenizer.Test;

/// <summary>
/// Tests for TAML lists.
/// Based on TAML spec: "List items are just values indented one tab from their parent key (no special syntax)"
/// </summary>
[TestClass]
public sealed class ListTests
{
    [TestMethod]
    public void WhenSimpleListThenTokenized()
    {
        // TAML spec example
        var source = """
            items
            	first item
            	second item
            	third item
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // List items are tokenized as keys (same highlighting as parent keys)
        // items + 3 list items = 4 keys
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(4, keys.Count);
        Assert.AreEqual("items", keys[0].Value);
        Assert.AreEqual("first item", keys[1].Value);
        Assert.AreEqual("second item", keys[2].Value);
        Assert.AreEqual("third item", keys[3].Value);
    }

    [TestMethod]
    public void WhenListItemsAsValuesThenCorrect()
    {
        var source = """
            features
            	authentication
            	logging
            	caching
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // List items are indented values (or keys without values)
        var indents = result.Tokens.Where(t => t.Type == TamlTokenType.Indent).ToList();
        Assert.AreEqual(1, indents.Count);
    }

    [TestMethod]
    public void WhenSpecFeaturesListThenCorrect()
    {
        // From TAML spec example
        var source = """
            features
            	user-authentication
            	api-gateway
            	rate-limiting
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // All list items should be recognized
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        // "features" plus three list items (which appear as keys without values)
        Assert.IsTrue(keys.Count >= 1);
    }

    [TestMethod]
    public void WhenNestedListsThenCorrect()
    {
        var source = """
            categories
            	fruits
            		apple
            		banana
            	vegetables
            		carrot
            		potato
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Count >= 7);
    }

    [TestMethod]
    public void WhenListAfterKeyValueThenCorrect()
    {
        var source = """
            config
            	name	MyApp
            	tags
            		web
            		api
            		cloud
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Any(k => k.Value == "config"));
        Assert.IsTrue(keys.Any(k => k.Value == "name"));
        Assert.IsTrue(keys.Any(k => k.Value == "tags"));
    }

    [TestMethod]
    public void WhenListWithSpacesInItemsThenPreserved()
    {
        var source = """
            messages
            	Hello World
            	Good Morning
            	Thank You
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // List items with spaces should be preserved
        var values = result.Tokens.Where(t =>
            t.Type == TamlTokenType.Key || t.Type == TamlTokenType.Value).ToList();
        Assert.IsTrue(values.Any(v => v.Value.Contains(" ")));
    }

    [TestMethod]
    public void WhenEmptyListThenJustParentKey()
    {
        var source = """
            empty_list
            next_key	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Any(k => k.Value == "empty_list"));
        Assert.IsTrue(keys.Any(k => k.Value == "next_key"));
    }

    [TestMethod]
    public void WhenListFollowedBySiblingThenCorrect()
    {
        var source = """
            items
            	item1
            	item2
            other	value
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Should dedent back to root level for "other"
        var dedents = result.Tokens.Where(t => t.Type == TamlTokenType.Dedent).ToList();
        Assert.IsTrue(dedents.Count >= 1);
    }

    [TestMethod]
    public void WhenMixedListAndObjectThenCorrect()
    {
        // From TAML spec example
        var source = """
            config
            	database
            		host	localhost
            		port	5432
            	features
            		authentication
            		logging
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Any(k => k.Value == "database"));
        Assert.IsTrue(keys.Any(k => k.Value == "features"));
    }
}
