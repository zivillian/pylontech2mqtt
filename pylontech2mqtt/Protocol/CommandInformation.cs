public enum CommandInformation : byte
{
    AnalogValueFixedPoint = 0x42,
    AlarmInfo = 0x44,
    SystemParameterFixedPoint = 0x47,
    ProtocolVersion = 0x4f,
    ManufacturerInfo = 0x51,
    GetChargeDischargeManagementInfo = 0x92,
    Serialnumber = 0x93,
    SetChargeDischargeManagementInfo = 0x94,
    Turnoff = 0x95,
    FirmwareInfo = 0x96,
}