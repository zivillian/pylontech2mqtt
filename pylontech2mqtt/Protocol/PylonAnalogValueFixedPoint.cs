public class PylonAnalogValueFixedPoint : PylonResult
{
    public PylonAnalogValueFixedPoint(ReadOnlyMemory<byte> info) : base(info)
    {
    }

    public PylonInfoFlag InfoFlags => (PylonInfoFlag)_info.Span[0];

    public byte Address => _info.Span[1];

    public byte CellCount => _info.Span[2];

    public IEnumerable<decimal> Voltages
    {
        get
        {
            var index = 3;
            for (int i = 0; i < CellCount; i++)
            {
                yield return GetUInt16(index..(index + 2)) * 0.001M;
                index += 2;
            }
        }
    }

    public byte TemperatureCount => _info.Slice(3 + CellCount + CellCount).Span[0];

    public decimal BmsTemperature => (GetInt16(_info.Slice(3 + 2 * CellCount + 1).Span) - 2731) * 0.1M;

    public decimal AvgTemperatureCell1to4 => (GetInt16(_info.Slice(3 + 2 * CellCount + 3).Span) - 2731) * 0.1M;

    public decimal AvgTemperatureCell5to8 => (GetInt16(_info.Slice(3 + 2 * CellCount + 5).Span) - 2731) * 0.1M;

    public decimal AvgTemperatureCell9to12 => (GetInt16(_info.Slice(3 + 2 * CellCount + 7).Span) - 2731) * 0.1M;

    public decimal AvgTemperatureCell13to15 => (GetInt16(_info.Slice(3 + 2 * CellCount + 9).Span) - 2731) * 0.1M;

    public decimal Current => GetInt16(_info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 1).Span)*0.1M;

    public decimal ModuleVoltage => GetUInt16(_info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 3).Span)*0.001M;

    public decimal RemainingCapacity1 => GetUInt16(_info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 5).Span)*0.001M;

    public byte UserDefined => _info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 7).Span[0];

    public decimal TotalCapacity1 => GetUInt16(_info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 8).Span)*0.001M;

    public ushort CycleNumber => GetUInt16(_info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 10).Span);

    public decimal RemainingCapacity2 => GetUInt24(_info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 12).Span)*0.001M;

    public decimal TotalCapacity2 => GetUInt24(_info.Slice(3 + 2 * CellCount + 2 * TemperatureCount + 15).Span)*0.001M;
}