public enum ResponseInformation : byte
{
    Normal = 0x0,
    VersionError = 0x1,
    ChecksumError = 0x2,
    LChecksumError = 0x3,
    InvalidCid2 = 0x4,
    CommandFormatError = 0x5,
    InvalidData = 0x6,
    AdrError = 0x90,
    CommunicationError = 0x91
}