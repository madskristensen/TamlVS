namespace TamlTokenizer.Test;

/// <summary>
/// Tests for TAML lists.
/// Based on TAML spec: "List items are just values indented one tab from their parent key (no special syntax)"
/// List items are classified by their value type (Value, Boolean, Number) for proper syntax highlighting.
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

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Parent key "items" is a Key token
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual("items", keys[0].Value);

        // List items are classified as Value tokens (string values)
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(3, values.Count);
        Assert.AreEqual("first item", values[0].Value);
        Assert.AreEqual("second item", values[1].Value);
        Assert.AreEqual("third item", values[2].Value);
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

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // List items are classified as Value tokens
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(3, values.Count);

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

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // "features" is a Key, list items are Value tokens
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual("features", keys[0].Value);

        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(3, values.Count);
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

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // "categories" is a key at root level
        // "fruits" and "vegetables" are under categories but have children, so they become Keys
        // "apple", "banana", "carrot", "potato" are leaf list items (Value tokens)
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(3, keys.Count);
        Assert.AreEqual("categories", keys[0].Value);
        Assert.AreEqual("fruits", keys[1].Value);
        Assert.AreEqual("vegetables", keys[2].Value);

        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(4, values.Count);
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

        TamlParseResult result = Taml.Tokenize(source);

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

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // List items with spaces should be preserved as Value tokens
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(3, values.Count);
        Assert.IsTrue(values.All(v => v.Value.Contains(" ")));
    }

    [TestMethod]
    public void WhenEmptyListThenJustParentKey()
    {
        var source = """
            empty_list
            next_key	value
            """;

        TamlParseResult result = Taml.Tokenize(source);

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

        TamlParseResult result = Taml.Tokenize(source);

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

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // config, database, host, port, features are keys
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Any(k => k.Value == "config"));
        Assert.IsTrue(keys.Any(k => k.Value == "database"));
        Assert.IsTrue(keys.Any(k => k.Value == "host"));
        Assert.IsTrue(keys.Any(k => k.Value == "port"));
        Assert.IsTrue(keys.Any(k => k.Value == "features"));

        // authentication and logging are list items (Value tokens) under features
        // localhost is also a Value token
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(3, values.Count); // localhost + authentication + logging
        Assert.IsTrue(values.Any(v => v.Value == "localhost"));
        Assert.IsTrue(values.Any(v => v.Value == "authentication"));
        Assert.IsTrue(values.Any(v => v.Value == "logging"));

        // port value 5432 is a Number token
        var numbers = result.Tokens.Where(t => t.Type == TamlTokenType.Number).ToList();
        Assert.AreEqual(1, numbers.Count);
        Assert.AreEqual("5432", numbers[0].Value);
    }

    /// <summary>
    /// Tests the exact example from TAML spec 03-lists.taml
    /// </summary>
    [TestMethod]
    public void WhenSpecColorsListThenCorrect()
    {
        // From TAML spec 03-lists.taml - Simple list
        var source = """
            colors
            	red
            	green
            	blue
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual("colors", keys[0].Value);

        // List items are classified as Value tokens
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(3, values.Count);
        Assert.AreEqual("red", values[0].Value);
        Assert.AreEqual("green", values[1].Value);
        Assert.AreEqual("blue", values[2].Value);
    }

    /// <summary>
    /// Tests the exact example from TAML spec 03-lists.taml - longer values
    /// </summary>
    [TestMethod]
    public void WhenSpecFruitsListThenCorrect()
    {
        // From TAML spec 03-lists.taml - List with longer values
        var source = """
            fruits
            	apple
            	banana
            	orange
            	strawberry
            	watermelon
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual("fruits", keys[0].Value);

        // List items are classified as Value tokens
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(5, values.Count);
        Assert.AreEqual("apple", values[0].Value);
        Assert.AreEqual("banana", values[1].Value);
        Assert.AreEqual("orange", values[2].Value);
        Assert.AreEqual("strawberry", values[3].Value);
        Assert.AreEqual("watermelon", values[4].Value);
    }

    /// <summary>
    /// Tests the exact example from TAML spec 03-lists.taml - nested structure
    /// </summary>
    [TestMethod]
    public void WhenSpecTeamsNestedListThenCorrect()
    {
        // From TAML spec 03-lists.taml - Nested list structure
        var source = """
            teams
            	engineering
            		backend
            		frontend
            		devops
            	design
            		ux
            		ui
            	marketing
            		content
            		social-media
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // teams is a key, engineering/design/marketing are nested parent keys
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(4, keys.Count);
        Assert.AreEqual("teams", keys[0].Value);
        Assert.AreEqual("engineering", keys[1].Value);
        Assert.AreEqual("design", keys[2].Value);
        Assert.AreEqual("marketing", keys[3].Value);

        // The leaf items are list items (classified as Value tokens)
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(7, values.Count);
        Assert.IsTrue(values.Any(v => v.Value == "backend"));
        Assert.IsTrue(values.Any(v => v.Value == "frontend"));
        Assert.IsTrue(values.Any(v => v.Value == "devops"));
        Assert.IsTrue(values.Any(v => v.Value == "ux"));
        Assert.IsTrue(values.Any(v => v.Value == "ui"));
        Assert.IsTrue(values.Any(v => v.Value == "content"));
        Assert.IsTrue(values.Any(v => v.Value == "social-media"));
    }

    /// <summary>
    /// Tests that list items are classified by their value type for proper syntax highlighting.
    /// </summary>
    [TestMethod]
    public void WhenListItemsHaveTypedValuesThenClassifiedCorrectly()
    {
        var source = """
            settings
            	true
            	false
            	42
            	3.14
            	hello
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // "settings" is a Key
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual("settings", keys[0].Value);

        // Boolean list items
        var booleans = result.Tokens.Where(t => t.Type == TamlTokenType.Boolean).ToList();
        Assert.AreEqual(2, booleans.Count);
        Assert.AreEqual("true", booleans[0].Value);
        Assert.AreEqual("false", booleans[1].Value);

        // Number list items
        var numbers = result.Tokens.Where(t => t.Type == TamlTokenType.Number).ToList();
        Assert.AreEqual(2, numbers.Count);
        Assert.AreEqual("42", numbers[0].Value);
        Assert.AreEqual("3.14", numbers[1].Value);

        // String list item
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(1, values.Count);
        Assert.AreEqual("hello", values[0].Value);
    }

    /// <summary>
    /// Tests that null list items are classified correctly.
    /// </summary>
    [TestMethod]
    public void WhenListContainsNullThenClassifiedAsNull()
    {
        var source = """
            options
            	enabled
            	~
            	disabled
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Null list item
        var nulls = result.Tokens.Where(t => t.Type == TamlTokenType.Null).ToList();
        Assert.AreEqual(1, nulls.Count);

        // String list items
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(2, values.Count);
        Assert.IsTrue(values.Any(v => v.Value == "enabled"));
        Assert.IsTrue(values.Any(v => v.Value == "disabled"));
    }
}
