using System.Text;

class RealStreamReader
{
    private readonly Stream _stream;
    private readonly Encoding _encoding;
    private readonly byte[] _buffer;
    private readonly StringBuilder _sb;

    public RealStreamReader(Stream stream, Encoding encoding)
    {
        _stream = stream;
        _encoding = encoding;
        _buffer = new byte[128];
        _sb = new StringBuilder();
    }

    public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            int length = 0;
            foreach (var chunk in _sb.GetChunks())
            {
                var cr = chunk.Span.IndexOf('\r');
                if (cr >= 0)
                {
                    var result = _sb.ToString(0, length + cr + 1);
                    _sb.Remove(0, length + cr + 1);
                    return result;
                }
                length += chunk.Length;
            }
            var read = await _stream.ReadAsync(_buffer, cancellationToken);
            if (read == 0)
            {
                return _sb.ToString();
            }
            _sb.Append(_encoding.GetString(_buffer, 0, read));
        }
        return String.Empty;
    }
}