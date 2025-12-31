using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using TamlTokenizer;

namespace TamlVS
{
    /// <summary>
    /// Represents a parsed TAML document that automatically re-parses when the buffer changes.
    /// </summary>
    public class Document : IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly object _lockObject = new();
        private bool _isDisposed;
        private volatile bool _isParsing;
        private TamlParseResult _result;

        public Document(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += OnBufferChanged;

            FileName = _buffer.GetFileName();
            ParseAsync().FireAndForget();
        }

        /// <summary>
        /// Gets the file name of the document.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets a value indicating whether the document is currently being parsed.
        /// </summary>
        public bool IsParsing => _isParsing;

        /// <summary>
        /// Gets the result of the most recent parse operation.
        /// </summary>
        public TamlParseResult Result
        {
            get
            {
                lock (_lockObject)
                {
                    return _result;
                }
            }
            private set
            {
                lock (_lockObject)
                {
                    _result = value;
                }
            }
        }

        /// <summary>
        /// Occurs when the document has been successfully parsed.
        /// </summary>
        public event Action<Document> Parsed;

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ParseAsync().FireAndForget();
        }

        private async Task ParseAsync()
        {
            if (_isParsing)
            {
                return;
            }

            _isParsing = true;
            var success = false;

            try
            {
                await TaskScheduler.Default; // move to a background thread

                var text = _buffer.CurrentSnapshot.GetText();
                var options = new TamlParserOptions { StrictMode = GeneralOptions.Instance.StrictMode };
                Result = Taml.Tokenize(text, options);
                success = true;
            }
            finally
            {
                _isParsing = false;

                if (success)
                {
                    Parsed?.Invoke(this);
                }
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _buffer.Changed -= OnBufferChanged;
                _result = null;
            }
        }
    }
}
