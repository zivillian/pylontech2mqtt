[Flags]
public enum ChargeDischargeStatus : byte
{
    ChargeEnabled = 0b1000_0000,
    DischargeEnabled = 0b0100_0000,
    ChargeImmediately1 = 0b0010_0000,
    ChargeImmediately2 = 0b0001_0000,
    FullChargeRequest = 0b0000_1000,
    Reserved1 = 0b0000_0100,
    Reserved2 = 0b0000_0010,
    Reserved3 = 0b0000_0001,
}