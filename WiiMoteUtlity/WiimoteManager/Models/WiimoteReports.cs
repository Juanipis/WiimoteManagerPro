namespace WiimoteManager.Models;

/// <summary>
/// Wiimote HID report identifiers and constants.
/// Reference: WiimoteLib and Dolphin Emulator documentation.
/// </summary>
public static class WiimoteReports
{
    // ========== OUTPUT REPORTS (Host → Wiimote) ==========
    
    /// <summary>Report ID 0x11: Set Player LEDs and Rumble</summary>
    public const byte ReportSetLEDRumble = 0x11;
    
    /// <summary>Report ID 0x12: Set Data Reporting Mode</summary>
    public const byte ReportSetDataMode = 0x12;
    
    /// <summary>Report ID 0x13: IR Camera Enable/Disable</summary>
    public const byte ReportIRCamera = 0x13;
    
    /// <summary>Report ID 0x14: Speaker Data</summary>
    public const byte ReportSpeaker = 0x14;
    
    /// <summary>Report ID 0x15: Request Status Information</summary>
    public const byte ReportStatusRequest = 0x15;
    
    /// <summary>Report ID 0x16: Write to Wiimote Memory</summary>
    public const byte ReportWriteMemory = 0x16;
    
    /// <summary>Report ID 0x17: Read from Wiimote Memory</summary>
    public const byte ReportReadMemory = 0x17;
    
    /// <summary>Report ID 0x18: Speaker Mute</summary>
    public const byte ReportSpeakerMute = 0x18;
    
    // ========== INPUT REPORTS (Wiimote → Host) ==========
    
    /// <summary>Report ID 0x20: Status Information</summary>
    public const byte ReportStatus = 0x20;
    
    /// <summary>Report ID 0x21: Memory Data (in response to read request)</summary>
    public const byte ReportMemory = 0x21;
    
    /// <summary>Report ID 0x30: Core Buttons only (2 bytes)</summary>
    public const byte ReportCoreButtons = 0x30;
    
    /// <summary>Report ID 0x31: Core Buttons + Accelerometer (6 bytes)</summary>
    public const byte ReportCoreAccel = 0x31;
    
    /// <summary>Report ID 0x32: Core Buttons + Extension (8 bytes)</summary>
    public const byte ReportCoreExt = 0x32;
    
    /// <summary>Report ID 0x33: Core Buttons + Accelerometer + IR (10 bytes)</summary>
    public const byte ReportCoreAccelIR = 0x33;
    
    /// <summary>Report ID 0x34: Extension only (6 bytes)</summary>
    public const byte ReportExtOnly = 0x34;
    
    /// <summary>Report ID 0x35: Core Buttons + Accelerometer + Extension (16 bytes)</summary>
    public const byte ReportCoreAccelExt = 0x35;
    
    /// <summary>Report ID 0x36: IR + Extension (10 bytes)</summary>
    public const byte ReportIRExt = 0x36;
    
    /// <summary>Report ID 0x37: Core Buttons + Accelerometer + IR + Extension (21 bytes)</summary>
    public const byte ReportFullAccelIRExt = 0x37;
    
    /// <summary>Report ID 0x3D: 21-byte extension data only</summary>
    public const byte ReportExt21 = 0x3D;
    
    /// <summary>Report ID 0x3E/0x3F: Interleaved data (6-byte chunks)</summary>
    public const byte ReportInterleaved1 = 0x3E;
    public const byte ReportInterleaved2 = 0x3F;
    
    // ========== LED BITMASKS (Report 0x11, byte 1, bits 4-7) ==========
    
    /// <summary>LED 1 bit position (0x10)</summary>
    public const byte LED1 = 0x10;
    
    /// <summary>LED 2 bit position (0x20)</summary>
    public const byte LED2 = 0x20;
    
    /// <summary>LED 3 bit position (0x40)</summary>
    public const byte LED3 = 0x40;
    
    /// <summary>LED 4 bit position (0x80)</summary>
    public const byte LED4 = 0x80;
    
    /// <summary>Rumble bit position (0x01)</summary>
    public const byte RumbleBit = 0x01;
    
    // ========== MEMORY ADDRESSES ==========
    
    /// <summary>Calibration data starts at 0x16</summary>
    public const ushort CalibrationAddress = 0x16;
    
    /// <summary>Extension identifier address</summary>
    public const uint ExtensionIdentifierAddress = 0xA400FA;
    
    /// <summary>Nunchuk calibration data address</summary>
    public const uint NunchukCalibrationAddress = 0xA40020;
    
    /// <summary>Classic Controller calibration data address</summary>
    public const uint ClassicCalibrationAddress = 0xA40020;
    
    // ========== EXTENSION TYPE IDENTIFIERS ==========
    
    /// <summary>Extension not connected (all zeros)</summary>
    public static readonly byte[] NoExtension = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    
    /// <summary>Nunchuk extension identifier</summary>
    public static readonly byte[] NunchukIdentifier = { 0x00, 0x00, 0xA4, 0x20, 0x00, 0x00 };
    
    /// <summary>Classic Controller identifier</summary>
    public static readonly byte[] ClassicIdentifier = { 0x00, 0x00, 0xA4, 0x20, 0x01, 0x01 };
    
    /// <summary>Guitar Hero 3 Wireless Guitar identifier</summary>
    public static readonly byte[] GuitarIdentifier = { 0x00, 0x00, 0xA4, 0x20, 0x03, 0x03 };
    
    /// <summary>Wii Motion Plus identifier</summary>
    public static readonly byte[] MotionPlusIdentifier = { 0x00, 0x00, 0xA6, 0x20, 0x00, 0x05 };
    
    // ========== ACCELEROMETER CONSTANTS ==========
    
    /// <summary>Accelerometer X neutral/zero gravity position</summary>
    public const byte AccelXZero = 0x80;
    
    /// <summary>Accelerometer Y neutral/zero gravity position</summary>
    public const byte AccelYZero = 0x80;
    
    /// <summary>Accelerometer Z neutral/zero gravity position (gravity pointing down)</summary>
    public const byte AccelZZero = 0xB3;
    
    /// <summary>Accelerometer sensitivity factor</summary>
    public const float AccelSensitivity = 256.0f;
    
    // ========== HID CONSTANTS ==========
    
    /// <summary>Wiimote Vendor ID</summary>
    public const ushort WiimoteVendorID = 0x057E;
    
    /// <summary>Wiimote Product ID (RVL-003)</summary>
    public const ushort WiimoteProductID = 0x0306;
    
    /// <summary>Expected HID report size for most Wiimote reports</summary>
    public const int StandardReportSize = 21;
    
    /// <summary>Wiimote HID input report descriptor prefix (typically starts with report ID)</summary>
    public const byte HIDReportIDIndex = 0;
}

/// <summary>
/// Wiimote data reporting modes (Report 0x12).
/// These modes control what data is included in each input report.
/// </summary>
public enum DataReportingMode : byte
{
    /// <summary>0x30: Core buttons only (2 bytes button data)</summary>
    ButtonsOnly = 0x30,
    
    /// <summary>0x31: Core buttons + accelerometer (6 bytes)</summary>
    ButtonsAccelerometer = 0x31,
    
    /// <summary>0x32: Core buttons + extension data (8 bytes)</summary>
    ButtonsExtension = 0x32,
    
    /// <summary>0x33: Core buttons + accelerometer + IR data (10 bytes)</summary>
    ButtonsAccelerometerIR = 0x33,
    
    /// <summary>0x34: Extension data only (6 bytes)</summary>
    ExtensionOnly = 0x34,
    
    /// <summary>0x35: Core buttons + accelerometer + extension data (16 bytes)</summary>
    ButtonsAccelerometerExtension = 0x35,
    
    /// <summary>0x36: IR data + extension data (10 bytes)</summary>
    IRExtension = 0x36,
    
    /// <summary>0x37: Core buttons + accelerometer + IR + extension (21 bytes)</summary>
    FullData = 0x37,
    
    /// <summary>0x3D: Extension data only, 21 bytes</summary>
    ExtensionOnly21 = 0x3D,
    
    /// <summary>0x3E/0x3F: Interleaved data (6-byte chunks alternating)</summary>
    Interleaved = 0x3E
}

/// <summary>
/// Supported extension types connected to the Wiimote.
/// </summary>
public enum ExtensionType : byte
{
    /// <summary>No extension connected</summary>
    None = 0,
    
    /// <summary>Nunchuk controller</summary>
    Nunchuk = 1,
    
    /// <summary>Classic Controller</summary>
    ClassicController = 2,
    
    /// <summary>Guitar Hero 3 Wireless Guitar</summary>
    Guitar = 3,
    
    /// <summary>Wii MotionPlus (accelerometer addon)</summary>
    MotionPlus = 4,
    
    /// <summary>Unknown extension type</summary>
    Unknown = 255
}
