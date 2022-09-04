[Flags]
public enum AlarmStatus2 : byte
{
    UsingBatteryModulePower = 0b0000_1000,
    DischargeMosfet = 0b0000_0100,
    ChargeMosfet = 0b0000_0010,
    PreMosfet = 0b0000_0001,
}