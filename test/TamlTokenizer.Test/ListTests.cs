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

    /// <summary>
    /// Tests the complex real-world example from TAML spec 07-complex-example.taml
    /// This tests deep nesting, mixed lists/objects, and various value types.
    /// </summary>
    [TestMethod]
    public void WhenComplexRealWorldExampleThenParsesCorrectly()
    {
        // Excerpt from TAML spec 07-complex-example.taml
        var source = """
            # Infrastructure section with mixed lists and objects
            infrastructure
            	region	us-east-1
            	availability_zones
            		us-east-1a
            		us-east-1b
            		us-east-1c

            	compute
            		web_servers
            			instance_type	t3.medium
            			count	5
            			auto_scaling
            				enabled	true
            				min	3
            				max	10
            				target_cpu	70
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess, "Should parse without errors");

        // Verify keys (all parent keys and key-value keys)
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Any(k => k.Value == "infrastructure"));
        Assert.IsTrue(keys.Any(k => k.Value == "region"));
        Assert.IsTrue(keys.Any(k => k.Value == "availability_zones"));
        Assert.IsTrue(keys.Any(k => k.Value == "compute"));
        Assert.IsTrue(keys.Any(k => k.Value == "web_servers"));
        Assert.IsTrue(keys.Any(k => k.Value == "instance_type"));
        Assert.IsTrue(keys.Any(k => k.Value == "count"));
        Assert.IsTrue(keys.Any(k => k.Value == "auto_scaling"));
        Assert.IsTrue(keys.Any(k => k.Value == "enabled"));
        Assert.IsTrue(keys.Any(k => k.Value == "min"));
        Assert.IsTrue(keys.Any(k => k.Value == "max"));
        Assert.IsTrue(keys.Any(k => k.Value == "target_cpu"));

        // Verify list items (availability zones) are classified as Value
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.IsTrue(values.Any(v => v.Value == "us-east-1"));
        Assert.IsTrue(values.Any(v => v.Value == "us-east-1a"));
        Assert.IsTrue(values.Any(v => v.Value == "us-east-1b"));
        Assert.IsTrue(values.Any(v => v.Value == "us-east-1c"));
        Assert.IsTrue(values.Any(v => v.Value == "t3.medium"));

        // Verify numbers
        var numbers = result.Tokens.Where(t => t.Type == TamlTokenType.Number).ToList();
        Assert.IsTrue(numbers.Any(n => n.Value == "5"));
        Assert.IsTrue(numbers.Any(n => n.Value == "3"));
        Assert.IsTrue(numbers.Any(n => n.Value == "10"));
        Assert.IsTrue(numbers.Any(n => n.Value == "70"));

        // Verify booleans
        var booleans = result.Tokens.Where(t => t.Type == TamlTokenType.Boolean).ToList();
        Assert.AreEqual(1, booleans.Count);
        Assert.AreEqual("true", booleans[0].Value);
    }

    /// <summary>
    /// Tests lists followed by sibling objects at same nesting level.
    /// </summary>
    [TestMethod]
    public void WhenListFollowedBySiblingObjectThenCorrect()
    {
        // From complex example: outputs (list) followed by metrics (object)
        var source = """
            observability
            	logging
            		level	info
            		outputs
            			console
            			file
            	metrics
            		enabled	true
            		provider	datadog
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Verify structure
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Any(k => k.Value == "observability"));
        Assert.IsTrue(keys.Any(k => k.Value == "logging"));
        Assert.IsTrue(keys.Any(k => k.Value == "level"));
        Assert.IsTrue(keys.Any(k => k.Value == "outputs"));
        Assert.IsTrue(keys.Any(k => k.Value == "metrics"));
        Assert.IsTrue(keys.Any(k => k.Value == "enabled"));
        Assert.IsTrue(keys.Any(k => k.Value == "provider"));

        // List items under outputs
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.IsTrue(values.Any(v => v.Value == "console"));
        Assert.IsTrue(values.Any(v => v.Value == "file"));
        Assert.IsTrue(values.Any(v => v.Value == "info"));
        Assert.IsTrue(values.Any(v => v.Value == "datadog"));

        // Boolean
        var booleans = result.Tokens.Where(t => t.Type == TamlTokenType.Boolean).ToList();
        Assert.AreEqual(1, booleans.Count);
        Assert.AreEqual("true", booleans[0].Value);
    }

    /// <summary>
    /// Tests deep nesting (4+ levels) with mixed value types.
    /// </summary>
    [TestMethod]
    public void WhenDeepNestingWithMixedTypesThenCorrect()
    {
        // From complex example: authentication > oauth > providers > google
        var source = """
            config
            	authentication
            		oauth
            			providers
            				google
            					client_id	google-id-123
            					client_secret	~
            					enabled	true
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // All nested keys
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(8, keys.Count);
        Assert.AreEqual("config", keys[0].Value);
        Assert.AreEqual("authentication", keys[1].Value);
        Assert.AreEqual("oauth", keys[2].Value);
        Assert.AreEqual("providers", keys[3].Value);
        Assert.AreEqual("google", keys[4].Value);
        Assert.AreEqual("client_id", keys[5].Value);
        Assert.AreEqual("client_secret", keys[6].Value);
        Assert.AreEqual("enabled", keys[7].Value);

        // Verify null value
        var nulls = result.Tokens.Where(t => t.Type == TamlTokenType.Null).ToList();
        Assert.AreEqual(1, nulls.Count);

        // Verify boolean
        var booleans = result.Tokens.Where(t => t.Type == TamlTokenType.Boolean).ToList();
        Assert.AreEqual(1, booleans.Count);
        Assert.AreEqual("true", booleans[0].Value);

        // Verify string value
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.AreEqual(1, values.Count);
        Assert.AreEqual("google-id-123", values[0].Value);
    }

    /// <summary>
    /// Tests lists at end of nested structure (email templates).
    /// </summary>
    [TestMethod]
    public void WhenListAtEndOfNestedStructureThenCorrect()
    {
        var source = """
            email
            	provider	sendgrid
            	from_address	noreply@example.com
            	templates
            		welcome
            		order_confirmation
            		password_reset
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Any(k => k.Value == "email"));
        Assert.IsTrue(keys.Any(k => k.Value == "provider"));
        Assert.IsTrue(keys.Any(k => k.Value == "from_address"));
        Assert.IsTrue(keys.Any(k => k.Value == "templates"));

        // List items (templates)
        var values = result.Tokens.Where(t => t.Type == TamlTokenType.Value).ToList();
        Assert.IsTrue(values.Any(v => v.Value == "sendgrid"));
        Assert.IsTrue(values.Any(v => v.Value == "noreply@example.com"));
        Assert.IsTrue(values.Any(v => v.Value == "welcome"));
        Assert.IsTrue(values.Any(v => v.Value == "order_confirmation"));
        Assert.IsTrue(values.Any(v => v.Value == "password_reset"));
    }

    /// <summary>
    /// Tests feature flags pattern with objects containing typed values.
    /// </summary>
    [TestMethod]
    public void WhenFeatureFlagsPatternThenCorrect()
    {
        var source = """
            features
            	new_checkout_flow
            		enabled	true
            		rollout_percent	100
            	dark_mode
            		enabled	false
            		rollout_percent	0
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);

        // Verify keys
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.AreEqual(7, keys.Count);

        // Verify booleans (true and false)
        var booleans = result.Tokens.Where(t => t.Type == TamlTokenType.Boolean).ToList();
        Assert.AreEqual(2, booleans.Count);
        Assert.IsTrue(booleans.Any(b => b.Value == "true"));
        Assert.IsTrue(booleans.Any(b => b.Value == "false"));

        // Verify numbers
        var numbers = result.Tokens.Where(t => t.Type == TamlTokenType.Number).ToList();
        Assert.AreEqual(2, numbers.Count);
        Assert.IsTrue(numbers.Any(n => n.Value == "100"));
        Assert.IsTrue(numbers.Any(n => n.Value == "0"));
    }
}
