using System.Net;

public abstract class PylonResult
{
    protected readonly ReadOnlyMemory<byte> _info;
    
    protected PylonResult(ReadOnlyMemory<byte> info)
    {
        _info = info;
    }

    protected ushort GetUInt16(Range range)
    {
        return (ushort)GetInt16(range);
    }

    protected ushort GetUInt16(ReadOnlySpan<byte> data)
    {
        return (ushort)GetInt16(data);
    }

    protected short GetInt16(Range range)
    {
        return GetInt16(_info.Span[range]);
    }

    protected short GetInt16(ReadOnlySpan<byte> data)
    {
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data));
    }

    protected int GetUInt24(ReadOnlySpan<byte> data)
    {
        return data[0] << 16 | data[1] << 8 | data[2];
    }
}