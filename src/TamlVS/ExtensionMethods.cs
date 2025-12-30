using Microsoft.VisualStudio.Text;
using TamlTokenizer;

namespace TamlVS
{
    public static class ExtensionMethods
    {
        public static Span ToSpan(this TamlToken token)
        {
            return new Span(token.Position, token.Length);
        }

        public static Document GetDocument(this ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new Document(buffer));
        }
    }
}
