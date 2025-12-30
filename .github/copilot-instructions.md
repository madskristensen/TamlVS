# Copilot Instructions
- This is a Visual Studio extension project with a supporting tokenizer library
- It uses the latest version of C# that is supported on .NET Framework 4.8 for the VS extension
- The TamlTokenizer library targets multiple frameworks: netstandard2.0, net462, net8.0, net10.0
 
# Team Best Practices
- A lot of developers with no experience in Visual Studio extensions will be reading the code. 
- The code must be readable and maintainable especially for new team members
- Simplicity is key.

# Project Structure
- `src/TamlTokenizer/` - The core tokenizer library (multi-targeted)
- `src/TamlVS/` - The Visual Studio extension
- `src/TamlTokenizer.Tests/` - Unit tests for the tokenizer
