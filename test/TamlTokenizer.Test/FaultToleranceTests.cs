namespace TamlTokenizer.Test;

/// <summary>
/// Tests for fault tolerance and error recovery.
/// Critical for VS editor extension support where partial/incomplete documents are common.
/// </summary>
[TestClass]
public sealed class FaultToleranceTests
{
    [TestMethod]
    public void WhenIncompleteDocumentThenStillTokenizes()
    {
        // User is typing - document is incomplete
        var source = "key\t";

        var result = Taml.Tokenize(source);

        // Should produce tokens even if incomplete
        Assert.IsTrue(result.Tokens.Count > 0);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Key));
    }

    [TestMethod]
    public void WhenOnlyKeyThenTokenizes()
    {
        var source = "key";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.Tokens.Count > 0);
        // Single word should be tokenized
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Key || t.Type == TamlTokenType.Value));
    }

    [TestMethod]
    public void WhenPartialNestedStructureThenTokenizes()
    {
        // User started nested structure but didn't finish
        var source = """
            parent
            	child
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.Tokens.Count > 0);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Indent));
    }

    [TestMethod]
    public void WhenMalformedLinesThenContinues()
    {
        // Mix of valid and invalid content
        var source = """
            valid	value
            	orphan
            another	valid
            """;

        var result = Taml.Tokenize(source);

        // Should still find all the key-value pairs
        var keys = result.Tokens.Where(t => t.Type == TamlTokenType.Key).ToList();
        Assert.IsTrue(keys.Count >= 2);
    }

    [TestMethod]
    public void WhenGetTokensIgnoresErrorsThenReturnsAll()
    {
        // GetTokens is for IDE use - should return tokens regardless of errors
        var source = """
            valid	value
            	orphaned
            more	content
            """;

        var tokens = Taml.GetTokens(source);

        Assert.IsTrue(tokens.Count > 0);
        Assert.IsTrue(tokens.Any(t => t.Type == TamlTokenType.EndOfFile));
    }

    [TestMethod]
    public void WhenTypingInProgressThenPartialResults()
    {
        // Simulate user typing character by character
        var partials = new[]
        {
            "k",
            "ke",
            "key",
            "key\t",
            "key\tv",
            "key\tva",
            "key\tval",
            "key\tvalu",
            "key\tvalue"
        };

        foreach (var source in partials)
        {
            var result = Taml.Tokenize(source);
            Assert.IsTrue(result.Tokens.Count > 0, $"Failed for: '{source}'");
        }
    }

    [TestMethod]
    public void WhenTrailingNewlinesThenHandled()
    {
        var source = "key\tvalue\n\n\n";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.EndOfFile));
    }

    [TestMethod]
    public void WhenLeadingNewlinesThenHandled()
    {
        var source = "\n\n\nkey\tvalue";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.Key));
    }

    [TestMethod]
    public void WhenOnlyWhitespaceThenHandled()
    {
        var source = "   \t\t   \n   ";

        var result = Taml.Tokenize(source);

        // Should not crash, should produce EOF
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.EndOfFile));
    }

    [TestMethod]
    public void WhenOnlyCommentsThenHandled()
    {
        var source = """
            # Just comments
            # More comments
            # End
            """;

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Where(t => t.Type == TamlTokenType.Comment).Count() == 3);
    }

    [TestMethod]
    public void WhenUnexpectedCharactersThenContinues()
    {
        // Unusual but should not crash
        var source = "key\tvalue with émojis 🎉 and üñíçödé";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.Tokens.Count > 0);
    }

    [TestMethod]
    public void WhenVeryLongLineThenHandled()
    {
        var longValue = new string('x', 10000);
        var source = $"key\t{longValue}";

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.Tokens.Count > 0);
    }

    [TestMethod]
    public void WhenManyLinesThenHandled()
    {
        var lines = Enumerable.Range(1, 1000)
            .Select(i => $"key{i}\tvalue{i}");
        var source = string.Join("\n", lines);

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.Tokens.Count > 0);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.EndOfFile));
    }

    [TestMethod]
    public void WhenDeeplyNestedWithinLimitsThenHandled()
    {
        var indent = "";
        var lines = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            lines.Add($"{indent}level{i}");
            indent += "\t";
        }
        lines.Add($"{indent}leaf\tvalue");

        var source = string.Join("\n", lines);

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.Tokens.Count > 0);
    }

    [TestMethod]
    public void WhenNullInputThenThrowsArgumentNull()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => Taml.Tokenize(null!));
    }

    [TestMethod]
    public void WhenPartialResultThenBothTokensAndErrors()
    {
        var source = "valid\tvalue\n\t\t\tskipped_levels\tvalue";

        var result = Taml.Tokenize(source);

        // Should have both tokens AND errors
        Assert.IsTrue(result.Tokens.Count > 1);
        Assert.IsTrue(result.HasErrors);
    }

    [TestMethod]
    public void WhenEditorScenarioThenResilient()
    {
        // Simulate common editor scenarios
        var scenarios = new[]
        {
            "",                          // Empty
            "\n",                        // Just newline
            "k",                         // Single char
            "key\t",                     // Key with tab, no value
            "key\tval\n",               // Complete line
            "key\tval\n\t",             // Starting indent
            "parent\n\tchild",          // Nested, incomplete
            "# comment\n",              // Just comment
            "\t\t\t",                   // Just tabs
            "   ",                      // Just spaces
        };

        foreach (var source in scenarios)
        {
            var result = Taml.Tokenize(source);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Tokens);
            Assert.IsTrue(result.Tokens.Count > 0, $"No tokens for scenario: '{source.Replace("\n", "\\n").Replace("\t", "\\t")}'");
        }
    }

    [TestMethod]
    public void WhenTokenizingRepeatedlyThenConsistent()
    {
        // Same input should always produce same output
        var source = """
            key	value
            parent
            	child	value
            """;

        var result1 = Taml.Tokenize(source);
        var result2 = Taml.Tokenize(source);

        Assert.AreEqual(result1.Tokens.Count, result2.Tokens.Count);
        Assert.AreEqual(result1.IsSuccess, result2.IsSuccess);
    }

    [TestMethod]
    public void WhenSpecialValuesAtEndOfFileThenHandled()
    {
        // No trailing newline
        var scenarios = new[]
        {
            "key\t~",       // Null at EOF
            "key\t\"\"",    // Empty string at EOF
            "key\tvalue",   // Normal value at EOF
            "# comment"     // Comment at EOF
        };

        foreach (var source in scenarios)
        {
            var result = Taml.Tokenize(source);
            Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.EndOfFile),
                $"Missing EOF for: {source}");
        }
    }

    [TestMethod]
    public void WhenGetTokensWithNullInputThenThrowsArgumentNull()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => Taml.GetTokens(null!));
    }

    [TestMethod]
    public void WhenFormatWithNullInputThenThrowsArgumentNull()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => Taml.Format(null!));
    }

    [TestMethod]
    public void WhenTokenizeWithNullOptionsThenUsesDefaults()
    {
        var source = "key\tvalue";

        var result = Taml.Tokenize(source, null);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Tokens.Count > 0);
    }

    [TestMethod]
    public void WhenTamlTokenEndPositionThenCorrect()
    {
        var source = "key\tvalue";
        var result = Taml.Tokenize(source);

        var keyToken = result.Tokens.First(t => t.Type == TamlTokenType.Key);

        Assert.AreEqual(keyToken.Position + keyToken.Length, keyToken.EndPosition);
        Assert.AreEqual(0, keyToken.Position);
        Assert.AreEqual(3, keyToken.Length);
        Assert.AreEqual(3, keyToken.EndPosition);
    }

    [TestMethod]
    public void WhenTamlErrorThenEndPositionCorrect()
    {
        var source = "key\n\t\t\tbad"; // Skip levels error

        var result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var error = result.Errors.First();
            Assert.AreEqual(error.Position + error.Length, error.EndPosition);
        }
    }

    [TestMethod]
    public void WhenParserOptionsInvalidValuesThenThrows()
    {
        var options = new TamlParserOptions();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxInputSize = 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxInputSize = -1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxNestingDepth = 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxTokenCount = 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxStringLength = 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxLineCount = 0);
    }

    [TestMethod]
    public void WhenParserOptionsValidValuesThenAccepted()
    {
        var options = new TamlParserOptions
        {
            MaxInputSize = 1000,
            MaxNestingDepth = 10,
            MaxTokenCount = 500,
            MaxStringLength = 100,
            MaxLineCount = 50,
            StrictMode = true
        };

        Assert.AreEqual(1000, options.MaxInputSize);
        Assert.AreEqual(10, options.MaxNestingDepth);
        Assert.AreEqual(500, options.MaxTokenCount);
        Assert.AreEqual(100, options.MaxStringLength);
        Assert.AreEqual(50, options.MaxLineCount);
        Assert.IsTrue(options.StrictMode);
    }
}
