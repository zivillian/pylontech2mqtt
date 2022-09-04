[Flags]
public enum AlarmStatus1 : byte
{
    ModuleUnderVoltage = 0b1000_0000,
    ChargeOverTemperature = 0b0100_0000,
    DischargeOverTemperature = 0b0010_0000,
    DischargeOverCurrent = 0b0001_0000,
    ChargeOverCurrent = 0b0000_0100,
    CellUnderVoltage = 0b0000_0010,
    ModuleOverVoltage = 0b0000_0001,
}