using System.Text;

public class PylonManufacturerInfo
{
    private readonly ReadOnlyMemory<byte> _info;

    public PylonManufacturerInfo(ReadOnlyMemory<byte> info)
    {
        _info = info;
    }

    public string Battery => Encoding.ASCII.GetString(_info.Span[0..10].TrimEnd((byte)0));

    public Version SoftwareVersion => new Version(_info.Span[10], _info.Span[11]);

    public string Manufacturer => Encoding.ASCII.GetString(_info.Span.Slice(12, 20).TrimEnd((byte)0));
}