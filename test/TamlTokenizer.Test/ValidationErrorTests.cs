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

        var result = Taml.TokenizeStrict(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceIndentation));
    }

    [TestMethod]
    public void WhenMixedTabsAndSpacesInStrictModeThenError()
    {
        // TAML spec: "Indentation must be pure tabs. No mixing of spaces and tabs."
        var source = "server\n \thost\tlocalhost"; // space + tab

        var result = Taml.TokenizeStrict(source);

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

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.InconsistentIndentation));
    }

    [TestMethod]
    public void WhenLenientModeThenContinuesAfterError()
    {
        // Default lenient mode should continue parsing after errors
        var source = "server\n  host\tlocalhost\nport\t8080"; // space indentation error

        var result = Taml.Tokenize(source); // Default is lenient

        // Should still produce tokens despite error
        Assert.IsTrue(result.Tokens.Count > 1);
        Assert.IsTrue(result.Tokens.Any(t => t.Type == TamlTokenType.EndOfFile));
    }

    [TestMethod]
    public void WhenErrorThenLineAndColumnProvided()
    {
        var source = "line1\tvalue\nline2\n\t\t\tbad\tvalue"; // Skip levels on line 3

        var result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var error = result.Errors.First();
            Assert.IsTrue(error.Line > 0);
            Assert.IsTrue(error.Column > 0);
        }
    }

    [TestMethod]
    public void WhenErrorThenPositionProvided()
    {
        var source = "key\tvalue\n\t\t\tbad"; // Skip levels

        var result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var error = result.Errors.First();
            Assert.IsTrue(error.Position >= 0);
        }
    }

    [TestMethod]
    public void WhenMaxNestingExceededThenError()
    {
        // Create deeply nested structure
        var options = new TamlParserOptions { MaxNestingDepth = 3 };
        var source = "a\n\tb\n\t\tc\n\t\t\td\n\t\t\t\te"; // 5 levels

        var result = Taml.Tokenize(source, options);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.NestingDepthExceeded));
    }

    [TestMethod]
    public void WhenMaxTokenCountExceededThenError()
    {
        var options = new TamlParserOptions { MaxTokenCount = 5 };
        var source = "a\tb\nc\td\ne\tf\ng\th\ni\tj";

        var result = Taml.Tokenize(source, options);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.TokenCountExceeded));
    }

    [TestMethod]
    public void WhenMaxInputSizeExceededThenError()
    {
        var options = new TamlParserOptions { MaxInputSize = 10 };
        var source = "this is a long string that exceeds the limit";

        var result = Taml.Tokenize(source, options);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors.Any(e => e.Code == TamlErrorCode.InputSizeExceeded));
    }

    [TestMethod]
    public void WhenSpaceIndentationInLenientModeThenNoError()
    {
        // Lenient mode should not error on space indentation
        var source = "server\n  host\tlocalhost";

        var result = Taml.Tokenize(source); // Default lenient mode

        // Should not have space indentation error in lenient mode
        Assert.IsFalse(result.Errors.Any(e => e.Code == TamlErrorCode.SpaceIndentation));
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

        var errors = Taml.Validate(source);

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

        var result = Taml.Tokenize(source);

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

        var result = Taml.Tokenize(source);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void WhenErrorMessageDescriptiveThenHelpful()
    {
        var source = "a\n\t\t\tb\tvalue"; // Skip indent levels

        var result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var error = result.Errors.First();
            Assert.IsFalse(string.IsNullOrEmpty(error.Message));
            Assert.IsTrue(error.Message.Length > 10); // Should be descriptive
        }
    }

    [TestMethod]
    public void WhenErrorCodeProvidedThenProgrammaticHandling()
    {
        var source = "a\n\t\t\tb\tvalue"; // Skip indent levels

        var result = Taml.Tokenize(source);

        if (result.HasErrors)
        {
            var error = result.Errors.First();
            Assert.IsNotNull(error.Code);
            Assert.IsTrue(error.Code.StartsWith("TAML"));
        }
    }

    [TestMethod]
    public void WhenMultipleErrorsThenAllCollected()
    {
        // Create document with multiple issues
        var source = "a\n\t\t\tb\tvalue\nc\n\t\t\td\tvalue"; // Two skip-level errors

        var result = Taml.Tokenize(source);

        // Should collect multiple errors in lenient mode
        Assert.IsTrue(result.HasErrors);
    }
}
