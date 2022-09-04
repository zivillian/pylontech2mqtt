[Flags]
public enum AlarmStatus3 : byte
{
    EffectiveChargeCurrent = 0b1000_0000,
    EffectiveDischargeCurrent = 0b0100_0000,
    Heater = 0b0010_0000,
    FullyCharged = 0b0000_1000,
    Buzzer = 0b0000_0001,
}