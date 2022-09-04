public class PylonSystemParameter:PylonResult
{
    public PylonSystemParameter(ReadOnlyMemory<byte> info)
        :base(info)
    {
    }

    public PylonInfoFlag InfoFlags => (PylonInfoFlag)_info.Span[0];

    public decimal CellHighVoltageLimit => GetInt16(1..3) * 0.001M;

    public decimal CellLowVoltageLimit => GetInt16(3..5) * 0.001M;

    public decimal CellUnderVoltageLimit => GetInt16(5..7) * 0.001M;

    public decimal ChargeHighTemperatureLimit => (GetInt16(7..9) - 2731) * 0.1M;

    public decimal ChargeLowTemperatureLimit => (GetInt16(9..11) - 2731) * 0.1M;

    public decimal ChargeCurrentLimit => GetInt16(11..13) * 0.01M;

    public decimal ModuleHighVoltageLimit => GetUInt16(13..15) * 0.001M;

    public decimal ModuleLowVoltageLimit => GetUInt16(15..17) * 0.001M;

    public decimal ModuleUnderVoltageLimit => GetUInt16(17..19) * 0.001M;

    public decimal DischargeHighTemperatureLimit => (GetInt16(19..21) - 2731) * 0.1M;

    public decimal DischargeLowTemperatureLimit => (GetInt16(21..23) - 2731) * 0.1M;

    public decimal DischargeCurrentLimit => GetInt16(23..25) * 0.01M;
}