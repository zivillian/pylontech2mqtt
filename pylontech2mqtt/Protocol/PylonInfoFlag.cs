[Flags]
public enum PylonInfoFlag:byte
{
    UnreadAlarmValueChange = 0b0000_0001,
    Reserved1 = 0b0000_0010,
    Reserved2 = 0b0000_0100,
    Reserved3 = 0b0000_1000,
    UnreadSwitchingValueChange = 0b0001_0000,
    Reserved4 = 0b0010_0000,
    Reserved5 = 0b0100_0000,
    Reserved6 = 0b1000_0000,
}