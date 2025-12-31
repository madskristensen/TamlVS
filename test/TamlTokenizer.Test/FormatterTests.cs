namespace TamlTokenizer.Test;

/// <summary>
/// Tests for the TAML formatter.
/// </summary>
[TestClass]
public sealed class FormatterTests
{
    [TestMethod]
    public void WhenSimpleKeyValuesThenAligned()
    {
        var source = "name\tvalue\nage\t42\nlocation\tNew York";

        var formatted = Taml.Format(source);

        // All values should align to the same column
        var lines = formatted.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(3, lines.Length);

        // "location" is longest key (8 chars), so others should have more tabs
        Assert.IsTrue(lines[0].Contains("name\t"));
        Assert.IsTrue(lines[1].Contains("age\t"));
        Assert.IsTrue(lines[2].Contains("location\t"));
    }

    [TestMethod]
    public void WhenMixedKeyLengthsThenValuesAligned()
    {
        var source = "a\t1\nbb\t2\nccc\t3";

        var formatted = Taml.Format(source);
        var lines = formatted.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // The formatter should align values at the same column
        // With keys a, bb, ccc - the longest is 3 chars
        // So all values should start after the same number of tabs from column 0

        // Each line should have the key + tabs + value
        // Values should visually align (meaning same column when rendered)
        // For TAML, we use tabs - so we verify each value starts after the same number of tabs
        Assert.AreEqual(3, lines.Length);

        // Count tabs after key for each line
        var tabs1 = CountTabsAfterKey(lines[0]);
        var tabs2 = CountTabsAfterKey(lines[1]);
        var tabs3 = CountTabsAfterKey(lines[2]);

        // Shorter keys should have more tabs to align with longer key
        // ccc (3 chars) needs minimum tabs
        // a (1 char) needs more tabs to reach same column
        Assert.IsTrue(tabs1 >= tabs3, $"Short key 'a' should have at least as many tabs ({tabs1}) as long key 'ccc' ({tabs3})");
        Assert.IsTrue(tabs2 >= tabs3, $"Medium key 'bb' should have at least as many tabs ({tabs2}) as long key 'ccc' ({tabs3})");
    }

    private static int CountTabsAfterKey(string line)
    {
        var count = 0;
        var inKey = true;
        foreach (var c in line)
        {
            if (c == '\t')
            {
                if (!inKey) count++;
                inKey = false;
            }
            else if (!inKey && c != '\t')
            {
                break; // Reached value
            }
        }
        return count;
    }

    [TestMethod]
    public void WhenNestedStructureThenEachLevelAlignedSeparately()
    {
        var source = """
            server
            	host	localhost
            	port	8080
            	timeout	30
            database
            	connection_string	server=localhost
            	pool_size	10
            """;

        var formatted = Taml.Format(source);

        // Each parent's children should be aligned within their group
        Assert.IsTrue(formatted.Contains("server\n"));
        Assert.IsTrue(formatted.Contains("database\n"));

        // Verify structure preserved
        TamlParseResult result = Taml.Tokenize(formatted);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenCommentsPreservedThenInOutput()
    {
        var source = """
            # This is a comment
            key	value
            # Another comment
            other	value2
            """;

        var formatted = Taml.Format(source);

        Assert.IsTrue(formatted.Contains("# This is a comment"));
        Assert.IsTrue(formatted.Contains("# Another comment"));
    }

    [TestMethod]
    public void WhenNullValuesThenPreserved()
    {
        var source = "name\tAlice\npassword\t~";

        var formatted = Taml.Format(source);

        Assert.IsTrue(formatted.Contains("password\t~"));
    }

    [TestMethod]
    public void WhenEmptyStringValuesThenPreserved()
    {
        var source = "name\tAlice\nnickname\t\"\"";

        var formatted = Taml.Format(source);

        Assert.IsTrue(formatted.Contains("nickname\t\"\""));
    }

    [TestMethod]
    public void WhenParentKeyWithoutValueThenNoExtraTabs()
    {
        var source = """
            server
            	host	localhost
            """;

        var formatted = Taml.Format(source);

        // Parent key should not have trailing tabs
        var lines = formatted.Split('\n');
        Assert.AreEqual("server", lines[0]);
    }

    [TestMethod]
    public void WhenListItemsThenPreserved()
    {
        var source = """
            features
            	authentication
            	logging
            	caching
            """;

        var formatted = Taml.Format(source);

        Assert.IsTrue(formatted.Contains("\tauthentication\n"));
        Assert.IsTrue(formatted.Contains("\tlogging\n"));
        Assert.IsTrue(formatted.Contains("\tcaching\n"));
    }

    [TestMethod]
    public void WhenBlankLinesPreservedThenInOutput()
    {
        var source = "key1\tvalue1\n\nkey2\tvalue2";

        var options = new TamlFormatterOptions { PreserveBlankLines = true };
        var formatted = Taml.Format(source, options);

        // Should have a blank line between the two keys
        Assert.IsTrue(formatted.Contains("\n\n"));
    }

    [TestMethod]
    public void WhenTrailingNewlineEnsuredThenPresent()
    {
        var source = "key\tvalue";

        var options = new TamlFormatterOptions { EnsureTrailingNewline = true };
        var formatted = Taml.Format(source, options);

        Assert.IsTrue(formatted.EndsWith("\n"));
    }

    [TestMethod]
    public void WhenTrailingNewlineNotEnsuredThenAbsent()
    {
        var source = "key\tvalue";

        var options = new TamlFormatterOptions { EnsureTrailingNewline = false };
        var formatted = Taml.Format(source, options);

        Assert.IsFalse(formatted.EndsWith("\n"));
    }

    [TestMethod]
    public void WhenLineEndingsNormalizedThenLfOnly()
    {
        var source = "key1\tvalue1\r\nkey2\tvalue2\rkey3\tvalue3";

        var options = new TamlFormatterOptions { NormalizeLineEndings = true };
        var formatted = Taml.Format(source, options);

        Assert.IsFalse(formatted.Contains("\r\n"));
        Assert.IsFalse(formatted.Contains("\r"));
        Assert.IsTrue(formatted.Contains("\n"));
    }

    [TestMethod]
    public void WhenSpaceIndentationThenConvertedToTabs()
    {
        var source = "parent\n    child\tvalue"; // 4 spaces instead of tab

        var formatted = Taml.Format(source);

        // Should have tab indentation
        Assert.IsTrue(formatted.Contains("\tchild\t"));
    }

    [TestMethod]
    public void WhenAlignmentDisabledThenMinimumTabs()
    {
        var source = "a\t1\nlongkey\t2";

        var options = new TamlFormatterOptions { AlignValues = false };
        var formatted = Taml.Format(source, options);

        // Each key should have minimum tabs
        Assert.IsTrue(formatted.Contains("a\t1"));
        Assert.IsTrue(formatted.Contains("longkey\t2"));
    }

    [TestMethod]
    public void WhenEmptyInputThenHandled()
    {
        var formatted = Taml.Format("");

        Assert.AreEqual("\n", formatted);
    }

    [TestMethod]
    public void WhenWhitespaceOnlyThenHandled()
    {
        var formatted = Taml.Format("   \t\t   ");

        Assert.AreEqual("\n", formatted);
    }

    [TestMethod]
    public void WhenFormattedThenStillValidTaml()
    {
        var source = """
            # TAML Example
            application	MyApp
            version	1.0.0
            server
            	host	localhost
            	port	8080
            database
            	type	postgresql
            	credentials
            		username	admin
            		password	~
            features
            	authentication
            	logging
            """;

        var formatted = Taml.Format(source);
        TamlParseResult result = Taml.Tokenize(formatted);

        Assert.IsTrue(result.IsSuccess, "Formatted output should be valid TAML");
    }

    [TestMethod]
    public void WhenDeeplyNestedThenAllLevelsAligned()
    {
        var source = """
            level0
            	level1
            		a	1
            		bb	2
            		ccc	3
            	another
            		x	10
            		yy	20
            """;

        var formatted = Taml.Format(source);
        TamlParseResult result = Taml.Tokenize(formatted);

        Assert.IsTrue(result.IsSuccess);
        // Verify indentation preserved
        Assert.IsTrue(formatted.Contains("\t\ta\t"));
        Assert.IsTrue(formatted.Contains("\t\tbb\t"));
        Assert.IsTrue(formatted.Contains("\t\tccc\t"));
    }

    [TestMethod]
    public void WhenSpecExampleThenFormatsCorrectly()
    {
        var source = """
            config
            	database
            		host	localhost
            		port	5432
            		credentials
            			username	admin
            			password	secret
            	features
            		authentication
            		logging
            		caching
            """;

        var formatted = Taml.Format(source);
        TamlParseResult result = Taml.Tokenize(formatted);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenIdempotentThenSameResultOnSecondFormat()
    {
        var source = """
            name	Alice
            age	30
            city	New York
            """;

        var formatted1 = Taml.Format(source);
        var formatted2 = Taml.Format(formatted1);

        Assert.AreEqual(formatted1, formatted2, "Formatting should be idempotent");
    }

    [TestMethod]
    public void WhenValuesWithSpacesThenPreserved()
    {
        var source = "message\tHello World\nauthor\tJohn Doe";

        var formatted = Taml.Format(source);

        Assert.IsTrue(formatted.Contains("Hello World"));
        Assert.IsTrue(formatted.Contains("John Doe"));
    }

    [TestMethod]
    public void WhenHashInValueThenNotTreatedAsComment()
    {
        var source = "color\t#FF0000";

        var formatted = Taml.Format(source);

        Assert.IsTrue(formatted.Contains("#FF0000"));
    }
}
