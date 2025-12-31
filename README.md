[marketplace]: https://marketplace.visualstudio.com/items?itemName=MadsKristensen.TamlVS
[repo]: https://github.com/madskristensen/TamlTokenizer

# TAML Language Support for Visual Studio

[![Build](https://github.com/madskristensen/TamlTokenizer/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/TamlTokenizer/actions/workflows/build.yaml)

Download this extension from the [Visual Studio Marketplace][marketplace] or get the [CI build](https://www.vsixgallery.com/extension/TamlVS.7b83d024-c84a-4a22-8898-26756a60c934).

--------------------------------------

Full language support for TAML (Tab Annotated Markup Language) files in Visual Studio.

## What is TAML?

TAML is a minimal, tab-based markup language designed for simplicity and readability:

- **Tab-based hierarchy** - One tab per indentation level (no spaces)
- **Tab-separated key-values** - Keys and values separated by a single tab
- **Special values** - `~` for null, `""` for empty string
- **Comments** - Lines starting with `#`
- **Human-readable** - Clean, consistent formatting
- **LLM-optimized** - Reduces token count for AI interactions

### Example

```taml
# User configuration
name	John Doe
email	john@example.com
settings
	theme	dark
	notifications	true
	bio	""
	nickname	~
```

## Features

### Syntax Highlighting
Full syntax highlighting support for `.taml` files that follows the official TAML specification. Colors distinguish between keys, values, comments, null values (`~`), and empty strings (`""`).

### Error Detection
Real-time syntax validation with inline error messages and warnings displayed in the Error List window. Hover over any error to see detailed information about what went wrong. Detects common issues like:
- Space indentation (TAML requires tabs)
- Mixed tabs and spaces
- Invalid characters

### Document Formatting
Format your TAML documents with **Edit > Advanced > Format Document** (Ctrl+K, Ctrl+D) or format just a selection with **Format Selection** (Ctrl+K, Ctrl+F). The formatter ensures consistent tab-based indentation and proper key-value alignment.

### Navigation Bar
The editor navigation bar shows a hierarchical dropdown of all keys in your document. Click any key to jump directly to it. Keys with children are displayed with indentation to show the document structure.

### Editor Features
- Outlining/code folding for nested sections
- Line numbers
- Brace matching
- Same word highlighting
- Preview support
- Full integration with Visual Studio's editor infrastructure

## Getting Started

Simply open any `.taml` file in Visual Studio, and the extension will automatically provide syntax highlighting and error detection.

## Requirements

- Visual Studio 2022 (17.0 or later)
- Supports both x64 and ARM64 architectures

## Learn More

- [TAML Specification](https://github.com/madskristensen/TamlTokenizer/blob/main/TAML-SPEC.md) - Official format specification
- [TamlTokenizer NuGet Package](https://www.nuget.org/packages/TamlTokenizer) - The .NET library powering this extension

## How can I help?
If you enjoy using the extension, please give it a ★★★★★ rating on the [Visual Studio Marketplace][marketplace].

Should you encounter bugs or have feature requests, head over to the [GitHub repo][repo] to open an issue if one doesn't already exist.

Pull requests are also very welcome, as I can't always get around to fixing all bugs myself. This is a personal passion project, so my time is limited.

Another way to help out is to [sponsor me on GitHub](https://github.com/sponsors/madskristensen).
