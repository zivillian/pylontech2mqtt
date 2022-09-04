using System.Text;

public class PylonSerialnumber
{
    private readonly ReadOnlyMemory<byte> _info;

    public PylonSerialnumber(ReadOnlyMemory<byte> info)
    {
        _info = info;
    }

    public byte Address => _info.Span[0];

    public string Serialnumber => Encoding.ASCII.GetString(_info.Span.Slice(1));
}