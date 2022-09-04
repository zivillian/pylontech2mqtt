using System;
using System.Runtime.CompilerServices;

class Pylonframe
{
    public Version Version { get; set; }

    public byte Address { get; set; }

    public ControlIdentifyCode ControlIdentifyCode { get; private set; } = ControlIdentifyCode.Default;

    public CommandInformation CommandInformation { get; set; }

    public ResponseInformation ResponseInformation { get; private set; }

    public ushort Length { get; private set; }

    public ReadOnlyMemory<byte> Info { get; set; } = Array.Empty<byte>();

    public string GetData()
    {
        var data = new char[1 + 2 + 2 + 2 + 2 + 4 + (2 * Info.Length) + 4 + 1];
        data[0] = '~';
        data[^1] = '\r';
        data[1] = Version.Major.ToString()[0];
        data[2] = Version.Minor.ToString()[0];
        Address.ToString("X2").CopyTo(data.AsSpan(3));
        ((byte)ControlIdentifyCode).ToString("X2").CopyTo(data.AsSpan(5));
        ((byte)CommandInformation).ToString("X2").CopyTo(data.AsSpan(7));
        var lchksum = CalculateLchksum((ushort)(Info.Length * 2));
        lchksum.ToString("X1").CopyTo(data.AsSpan(9));
        (Info.Length*2).ToString("X3").CopyTo(data.AsSpan(10));
        BytesToHex(Info.Span, data.AsSpan(13, 2 * Info.Length));
        var chksum = CalculateChecksum(data.AsSpan()[1..^5]);
        chksum.ToString("X2").CopyTo(data.AsSpan()[^5..^1]);
        return new string(data);
    }

    public static Pylonframe Parse(string frame)
    {
        if (String.IsNullOrEmpty(frame)) throw new ArgumentNullException(nameof(frame));
        if (frame[0] != '~') throw new InvalidDataException("SOI missing");
        if (frame[^1] != '\r') throw new InvalidDataException("EOI missing");
        if (frame.Length < 16) throw new ArgumentOutOfRangeException(nameof(frame), "frame too short");
        var data = HexToByte(frame.AsSpan(1, frame.Length - 2));
        var result = new Pylonframe();
        result.Version = new Version(data[0] / 16, data[0] % 16);
        result.Address = data[1];
        result.ControlIdentifyCode = (ControlIdentifyCode)data[2];
        result.ResponseInformation = (ResponseInformation)data[3];
        var length = (ushort)(data[4] << 8 | data[5]);
        var lchksum = length >> 12;
        var lenid = (ushort)(length & 0b1111_1111_1111);
        if (lchksum != CalculateLchksum(lenid)) throw new InvalidDataException("invalid LCHKSUM");
        result.Length = lenid;
        result.Info = data.AsMemory(6, lenid / 2);
        var chksumdata = data.AsSpan(6).Slice(lenid / 2);
        if (chksumdata.Length != 2) throw new ArgumentException("unsuspected data between INFO and CHKSUM", nameof(frame));
        var chksum = (ushort)(chksumdata[0] << 8 | chksumdata[1]);
        if (chksum != CalculateChecksum(frame.AsSpan(1, frame.Length - 6)))throw new InvalidDataException("invalid CHKSUM");
        return result;
    }

    private static ushort CalculateChecksum(ReadOnlySpan<char> data)
    {
        var result = 0;
        foreach (var c in data)
        {
            result += c;
        }
        var remainder = result % 65536;
        return (ushort)((~remainder) + 1);
    }

    private static byte CalculateLchksum(ushort length)
    {
        if (length == 0) return 0;
        var sum = (length & 0xf) +((length >> 4) & 0xf) + ((length >> 8) & 0xf);
        var modulus = sum % 16;
        return (byte)(0b1111 - modulus + 1);
    }

    private static byte[] HexToByte(ReadOnlySpan<char> hex)
    {
        if ((hex.Length & 1) != 0)
            throw new ArgumentException("uneven hex length");
        var r = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length - 1; i += 2)
        {
            var a = GetHex(hex[i]);
            var b = GetHex(hex[i + 1]);
            r[i / 2] = (byte)(a * 16 + b);
        }
        return r;
    }

    private static void BytesToHex(ReadOnlySpan<byte> source, Span<char> target)
    {
        if (target.Length < 2 * source.Length) throw new ArgumentOutOfRangeException(nameof(target));
        for (int i = 0; i < source.Length; i++)
        {
            source[i].ToString("X2").CopyTo(target);
            target = target.Slice(2);
        }
    }

    //converts a single hex character to it's decimal value
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetHex(char x)
    {
        if (x <= '9' && x >= '0')
        {
            return (byte)(x - '0');
        }
        else if (x <= 'z' && x >= 'a')
        {
            return (byte)(x - 'a' + 10);
        }
        else if (x <= 'Z' && x >= 'A')
        {
            return (byte)(x - 'A' + 10);
        }
        return 0;
    }
}