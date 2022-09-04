public enum AlarmFlag:byte
{
    Normal = 0,
    BelowLowerLimit = 0x01,
    AboverHigherLimit = 0x02,
    OtherError = 0xF0
}