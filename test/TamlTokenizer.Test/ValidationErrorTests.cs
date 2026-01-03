namespace TamlTokenizer.Test;

/// <summary>
/// Tests for TAML validation errors.
/// Based on TAML spec validation rules for invalid documents.
/// </summary>
[TestClass]
public sealed class ValidationErrorTests
{
    [TestMethod]
    public void WhenSpaceIndentationInStrictModeThenError()
    {
        // TAML spec: "Only tab characters may be used for indentation. Spaces at the start of a line are invalid."
        var source = "server\n  host\tlocalhost"; // 2 spaces instead of tab

        TamlParseResult result = Taml.TokenizeStrict(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceIndentation));
    }

    [TestMethod]
    public void WhenMixedTabsAndSpacesInStrictModeThenError()
    {
        // TAML spec: "Indentation must be pure tabs. No mixing of spaces and tabs."
        var source = "server\n \thost\tlocalhost"; // space + tab

        TamlParseResult result = Taml.TokenizeStrict(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e =>
            e.Code == TamlErrorCode.MixedIndentation ||
            e.Code == TamlErrorCode.SpaceIndentation));
    }

    [TestMethod]
    public void WhenSkippedIndentLevelThenError()
    {
        // TAML spec: "Each nesting level must increase indentation by exactly one tab. Skipping levels is invalid."
        var source = "server\n\t\t\thost\tlocalhost"; // 3 tabs instead of 1

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.InconsistentIndentation));
    }

    [TestMethod]
    public void WhenSpaceSeparatorThenError()
    {
        // TAML spec: Only tabs can separate keys from values, not spaces
        var source = "key value\treal_value"; // "key value" is the key, tab, then value
        // This catches the case where a key contains spaces (which suggests user meant to use tab)

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceSeparator));
    }

    [TestMethod]
    public void WhenMultipleValuesOnLineThenError()
    {
        // TAML spec: Each line can have at most one key-value pair
        var source = "key\tvalue\tsomething\telse"; // Multiple tab separators

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.MultipleValuesOnLine));
    }


    [TestMethod]
    public void WhenLenientModeThenContinuesAfterError()
    {
        // Default lenient mode should continue parsing after errors
        var source = "server\n  host\tlocalhost\nport\t8080"; // space indentation error

        TamlParseResult result = Taml.Tokenize(source); // Default is lenient

        // Should still produce tokens despite error
        Assert.IsTrue(result.Tokens.Count > 1);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.EndOfFile));
    }

    [TestMethod]
    public void WhenErrorThenLineAndColumnProvided()
    {
        var source = "line1\tvalue\nline2\n\t\t\tbad\tvalue"; // Skip levels on line 3

        TamlParseResult result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            TamlError error = result.Errors[0];
            Assert.IsTrue(error.Line > 0);
            Assert.IsTrue(error.Column > 0);
        }
    }

    [TestMethod]
    public void WhenErrorThenPositionProvided()
    {
        var source = "key\tvalue\n\t\t\tbad"; // Skip levels

        TamlParseResult result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            TamlError error = result.Errors[0];
            Assert.IsTrue(error.Position >= 0);
        }
    }

    [TestMethod]
    public void WhenMaxNestingExceededThenError()
    {
        // Create deeply nested structure
        var options = new TamlParserOptions { MaxNestingDepth = 3 };
        var source = "a\n\tb\n\t\tc\n\t\t\td\n\t\t\t\te"; // 5 levels

        TamlParseResult result = Taml.Tokenize(source, options);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.NestingDepthExceeded));
    }

    [TestMethod]
    public void WhenMaxTokenCountExceededThenError()
    {
        var options = new TamlParserOptions { MaxTokenCount = 5 };
        var source = "a\tb\nc\td\ne\tf\ng\th\ni\tj";

        TamlParseResult result = Taml.Tokenize(source, options);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.TokenCountExceeded));
    }

    [TestMethod]
    public void WhenMaxInputSizeExceededThenError()
    {
        var options = new TamlParserOptions { MaxInputSize = 10 };
        var source = "this is a long string that exceeds the limit";

        TamlParseResult result = Taml.Tokenize(source, options);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.InputSizeExceeded));
    }

    [TestMethod]
    public void WhenSpaceIndentationThenAlwaysError()
    {
        // Space indentation is always an error - TAML requires tabs
        var source = "server\n  host\tlocalhost";

        TamlParseResult result = Taml.Tokenize(source); // Default lenient mode

        // Should have space indentation error even in lenient mode
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceIndentation));
    }

    [TestMethod]
    public void WhenIsValidWithValidDocumentThenTrue()
    {
        var source = """
            key	value
            parent
            	child	value
            """;

        Assert.IsTrue(Taml.IsValid(source));
    }

    [TestMethod]
    public void WhenIsValidWithInvalidDocumentThenFalse()
    {
        var source = "a\n\t\t\tb\tvalue"; // Skip indent levels

        Assert.IsFalse(Taml.IsValid(source));
    }

    [TestMethod]
    public void WhenValidateReturnsErrorsThenCorrect()
    {
        var source = "a\n\t\t\tb\tvalue"; // Skip indent levels

        IReadOnlyList<TamlError> errors = Taml.Validate(source);

        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public void WhenValidDocumentThenNoErrors()
    {
        var source = """
            # Valid TAML
            application	MyApp
            version	1.0.0
            server
            	host	localhost
            	port	8080
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void WhenSpecValidExampleThenNoErrors()
    {
        // From TAML spec valid examples
        var source = """
            # Valid: Simple key-value
            name	value
            # Valid: Parent key with children
            server
            	host	localhost
            	port	8080
            # Valid: Multiple tabs for alignment
            short	value1
            long_key	value2
            # Valid: Lists
            items
            	item1
            	item2
            # Valid: Nested structure
            parent
            	child
            		grandchild	value
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenErrorMessageDescriptiveThenHelpful()
    {
        var source = "a\n\t\t\tb\tvalue"; // Skip indent levels

        TamlParseResult result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            TamlError error = result.Errors[0];
            Assert.IsFalse(string.IsNullOrEmpty(error.Message));
            Assert.IsTrue(error.Message.Length > 10); // Should be descriptive
        }
    }

    [TestMethod]
    public void WhenErrorCodeProvidedThenProgrammaticHandling()
    {
        var source = "a\n\t\t\tb\tvalue"; // Skip indent levels

        TamlParseResult result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            TamlError error = result.Errors[0];
            Assert.IsNotNull(error.Code);
            Assert.IsTrue(error.Code.StartsWith("TAML"));
        }
    }

    [TestMethod]
    public void WhenMultipleErrorsThenAllCollected()
    {
        // Create document with multiple issues
        var source = "a\n\t\t\tb\tvalue\nc\n\t\t\td\tvalue"; // Two skip-level errors

        TamlParseResult result = Taml.Tokenize(source);

        // Should collect multiple errors in lenient mode
        Assert.IsTrue(result.HasErrors);
    }

    [TestMethod]
    public void WhenOrphanedLineThenError()
    {
        // TAML spec: "Indented lines must have a parent. You cannot increase indentation after a key-value pair."
        var source = "name\tvalue\n\torphan\tvalue";

        TamlParseResult result = Taml.TokenizeStrict(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.OrphanedLine));
    }

    [TestMethod]
    public void WhenParentKeyWithValueAndChildrenThenError()
    {
        // TAML spec: "A key with children (parent key) must not have a value on the same line."
        var source = "server\tlocalhost\n\tport\t8080";

        TamlParseResult result = Taml.TokenizeStrict(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.ParentWithValue));
    }

    [TestMethod]
    public void WhenParentKeyWithoutValueThenNoError()
    {
        // Valid: Parent key without value followed by children
        var source = "server\n\thost\tlocalhost\n\tport\t8080";

        TamlParseResult result = Taml.TokenizeStrict(source);

        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.ParentWithValue));
    }

    [TestMethod]
    public void WhenKeyValueWithoutChildrenThenNoOrphanError()
    {
        // Valid: Key-value pairs without children don't create orphan errors
        var source = "name\tvalue\nother\tvalue2";

        TamlParseResult result = Taml.TokenizeStrict(source);

        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.OrphanedLine));
    }

    [TestMethod]
    public void WhenValidNestedStructureThenNoErrors()
    {
        // Valid nested structure should not trigger any structural errors
        var source = """
            server
            	host	localhost
            	port	8080
            	settings
            		timeout	30
            		retry	3
            """;

        TamlParseResult result = Taml.TokenizeStrict(source);

        Assert.IsFalse(result.Errors.Any(e =>
            e.Code == TamlErrorCode.OrphanedLine ||
            e.Code == TamlErrorCode.ParentWithValue ||
            e.Code == TamlErrorCode.EmptyKey));
    }

    // Edge case tests

    [TestMethod]
    public void WhenOnlyWhitespaceThenNoContentErrors()
    {
        // Lines with only whitespace should be ignored
        var source = "key\tvalue\n\t\t\n\nother\tvalue2";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.EmptyKey));
    }

    [TestMethod]
    public void WhenTrailingTabsOnLineThenNoError()
    {
        // Trailing tabs after value should not cause errors (just whitespace)
        var source = "key\tvalue\t\t\t";

        TamlParseResult result = Taml.Tokenize(source);

        // Trailing tabs are harmless - no MultipleValuesOnLine error
        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.MultipleValuesOnLine));
    }

    [TestMethod]
    public void WhenDeepNestingWithinLimitThenNoError()
    {
        // Deep but valid nesting
        var source = "a\n\tb\n\t\tc\n\t\t\td\n\t\t\t\te\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.InconsistentIndentation));
    }

    [TestMethod]
    public void WhenDedentSkipsLevelsThenValid()
    {
        // Dedenting multiple levels at once is valid
        var source = """
            root
            	level1
            		level2
            			level3	value
            other	value
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.InconsistentIndentation));
    }

    [TestMethod]
    public void WhenTabOnlyLineThenHandledGracefully()
    {
        // Line with only tabs (no content)
        var source = "key\tvalue\n\t\t\t\nother\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        // Should not crash, tabs-only line treated as blank
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void WhenValueContainsSpecialCharsThenValid()
    {
        // Values can contain special characters (except tab)
        var source = "url\thttps://example.com/path?query=value&other=123#anchor";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var value = result.Tokens.First(t => t.Type == TamlTokenType.Value);
        Assert.AreEqual("https://example.com/path?query=value&other=123#anchor", value.Value);
    }

    [TestMethod]
    public void WhenKeyContainsHyphenAndUnderscoreThenValid()
    {
        // Keys with hyphens and underscores are valid
        var source = "my-key_name\tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
        var key = result.Tokens.First(t => t.Type == TamlTokenType.Key);
        Assert.AreEqual("my-key_name", key.Value);
    }

    [TestMethod]
    public void WhenEmptyDocumentThenNoErrors()
    {
        var source = "";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenOnlyCommentsThenNoErrors()
    {
        var source = """
            # Comment 1
            # Comment 2
            # Comment 3
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenConsecutiveParentKeysThenValid()
    {
        // Multiple parent keys in a row (each with their own children)
        var source = """
            parent1
            	child1	value1
            parent2
            	child2	value2
            parent3
            	child3	value3
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenSiblingAfterNestedChildrenThenValid()
    {
        // Sibling at same level after deeply nested children
        var source = """
            server
            	database
            		host	localhost
            		port	5432
            	cache
            		enabled	true
            """;

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenSpaceBetweenTabsThenError()
    {
        // Spaces between tabs (e.g., for alignment) are invalid
        var source = "key\t \tvalue";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceSeparator));
    }

    [TestMethod]
    public void WhenSpaceAfterTabBeforeValueThenError()
    {
        // Space after tab separator but before value is invalid
        var source = "key\t value";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceSeparator));
    }

    [TestMethod]
    public void WhenTrailingSpacesOnLineThenNoError()
    {
        // Trailing spaces at end of line are harmless
        var source = "key\tvalue   ";

        TamlParseResult result = Taml.Tokenize(source);

        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceSeparator));
    }
}
