using System.Runtime.InteropServices;

public class PylonAlarmInfo : PylonResult
{
    public PylonAlarmInfo(ReadOnlyMemory<byte> info)
        : base(info)
    {
    }

    public PylonInfoFlag InfoFlags => (PylonInfoFlag)_info.Span[0];

    public byte Address => _info.Span[1];

    public byte CellCount => _info.Span[2];

    public ReadOnlySpan<AlarmFlag> CellVoltage => MemoryMarshal.Cast<byte, AlarmFlag>(_info.Slice(3, CellCount).Span);

    public byte TemperatureCount => _info.Slice(3 + CellCount).Span[0];

    public AlarmFlag BmsTemperature => (AlarmFlag)_info.Slice(3 + CellCount).Span[1];

    public AlarmFlag TemperatureCell1to4 => (AlarmFlag)_info.Slice(3 + CellCount).Span[2];

    public AlarmFlag TemperatureCell5to8 => (AlarmFlag)_info.Slice(3 + CellCount).Span[3];

    public AlarmFlag TemperatureCell9to12 => (AlarmFlag)_info.Slice(3 + CellCount).Span[4];

    public AlarmFlag TemperatureCell13to15 => (AlarmFlag)_info.Slice(3 + CellCount).Span[5];

    public AlarmFlag? MosfetTemperature => TemperatureCount > 5 ? (AlarmFlag)_info.Slice(3 + CellCount).Span[6] : null;

    public AlarmFlag ChargeCurrent => (AlarmFlag)_info.Slice(3 + CellCount + TemperatureCount).Span[1];

    public AlarmFlag ModuleVoltage => (AlarmFlag)_info.Slice(3 + CellCount + TemperatureCount).Span[2];

    public AlarmFlag DischargeCurrent => (AlarmFlag)_info.Slice(3 + CellCount + TemperatureCount).Span[3];

    public AlarmStatus1 Status1 => (AlarmStatus1)_info.Slice(3 + CellCount + TemperatureCount).Span[4];

    public AlarmStatus2 Status2 => (AlarmStatus2)_info.Slice(3 + CellCount + TemperatureCount).Span[5];

    public AlarmStatus3 Status3 => (AlarmStatus3)_info.Slice(3 + CellCount + TemperatureCount).Span[6];

    public AlarmCellError4 Status4 => (AlarmCellError4)_info.Slice(3 + CellCount + TemperatureCount).Span[7];

    public AlarmCellError5 Status5 => (AlarmCellError5)_info.Slice(3 + CellCount + TemperatureCount).Span[8];
}