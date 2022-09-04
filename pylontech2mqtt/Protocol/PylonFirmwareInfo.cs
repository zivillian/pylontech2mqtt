public class PylonFirmwareInfo
{
    private readonly ReadOnlyMemory<byte> _info;

    public PylonFirmwareInfo(ReadOnlyMemory<byte> info)
    {
        _info = info;
    }

    public byte Address => _info.Span[0];

    public Version ManufactureVersion => new Version(_info.Span[1], _info.Span[2]);

    public Version MainlineVersion => new Version(_info.Span[3], _info.Span[4], _info.Span[5]);
}