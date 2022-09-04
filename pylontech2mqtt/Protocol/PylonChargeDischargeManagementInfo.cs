public class PylonChargeDischargeManagementInfo:PylonResult
{
    public PylonChargeDischargeManagementInfo(ReadOnlyMemory<byte> info)
        : base(info)
    {
    }

    public byte Address => _info.Span[0];

    public decimal ChargeVoltageLimit => GetUInt16(1..3) * 0.001M;

    public decimal DischargeVoltageLimit => GetUInt16(3..5) * 0.001M;

    public decimal ChargeCurrentLimit => GetInt16(5..7) * 0.1M;

    public decimal DischargeCurrentLimit => GetInt16(7..9) * 0.1M;

    public ChargeDischargeStatus Status => (ChargeDischargeStatus)_info.Span[9];
}