namespace WiimoteManager.Models;

/// <summary>
/// Represents the button state bitmask for Wiimote core buttons.
/// Maps to the 16-bit button data reported in HID reports 0x30, 0x31, 0x35, etc.
/// 
/// After applying 0x1F masks to both bytes in Report 0x31:
///   Byte 1 (HIGH) bits 0-4: Two, One, B, A, Minus
///   Byte 2 (LOW) bits 0-4: DPadLeft, DPadRight, DPadDown, DPadUp, Plus
/// 
/// Full button word = (Byte1 & 0x1F) << 8 | (Byte 2 & 0x1F)
/// </summary>
[Flags]
public enum ButtonState : ushort
{
    /// <summary>No buttons pressed</summary>
    None = 0x0000,
    
    // LOW BYTE (Byte 2 - based on REAL hardware testing)
    /// <summary>Button 2 (bit 0) - CORRECTED</summary>
    Two = 0x0001,
    
    /// <summary>Button 1 (bit 1) - CORRECTED</summary>
    One = 0x0002,
    
    /// <summary>B button / Trigger (bit 2) - CORRECTED</summary>
    B = 0x0004,
    
    /// <summary>A button (bit 3) - CORRECTED</summary>
    A = 0x0008,
    
    /// <summary>Minus button (bit 4) - CORRECTED</summary>
    Minus = 0x0010,
    
    // HIGH BYTE (Byte 1 - based on REAL hardware testing)
    /// <summary>D-Pad Left (bit 8) - CORRECTED</summary>
    DPadLeft = 0x0100,
    
    /// <summary>D-Pad Right (bit 9) - CORRECTED</summary>
    DPadRight = 0x0200,
    
    /// <summary>D-Pad Down (bit 10) - CORRECTED</summary>
    DPadDown = 0x0400,
    
    /// <summary>D-Pad Up (bit 11) - CORRECTED</summary>
    DPadUp = 0x0800,
    
    /// <summary>Plus button (bit 12) - CORRECTED</summary>
    Plus = 0x1000,
    
    /// <summary>Home button (bit 15 / bit 7 of high byte - BEFORE masking)</summary>
    /// <remarks>
    /// NOTE: Home button is at bit 7 of the high byte (0x8000), but this bit
    /// is cleared by our 0x1F mask used for Report 0x31. To detect Home,
    /// we need to check the RAW byte data before masking.
    /// </remarks>
    Home = 0x8000
}

/// <summary>
/// Represents the button state of Nunchuk extension controller.
/// </summary>
[Flags]
public enum NunchukButtons : byte
{
    /// <summary>No buttons pressed</summary>
    None = 0x00,
    
    /// <summary>C button</summary>
    C = 0x02,
    
    /// <summary>Z button</summary>
    Z = 0x01
}

/// <summary>
/// Represents the button state of Classic Controller extension.
/// </summary>
[Flags]
public enum ClassicControllerButtons : ushort
{
    /// <summary>No buttons pressed</summary>
    None = 0x0000,
    
    /// <summary>D-Pad Up</summary>
    DPadUp = 0x0800,
    
    /// <summary>D-Pad Left</summary>
    DPadLeft = 0x0200,
    
    /// <summary>D-Pad Right</summary>
    DPadRight = 0x0400,
    
    /// <summary>D-Pad Down</summary>
    DPadDown = 0x0100,
    
    /// <summary>Plus button</summary>
    Plus = 0x1000,
    
    /// <summary>Minus button</summary>
    Minus = 0x0010,
    
    /// <summary>Home button</summary>
    Home = 0x2000,
    
    /// <summary>A button</summary>
    A = 0x0010,
    
    /// <summary>B button</summary>
    B = 0x0040,
    
    /// <summary>X button</summary>
    X = 0x0008,
    
    /// <summary>Y button</summary>
    Y = 0x0020,
    
    /// <summary>ZL button</summary>
    ZL = 0x0080,
    
    /// <summary>ZR button</summary>
    ZR = 0x0004,
    
    /// <summary>Left Trigger</summary>
    TriggerL = 0x2000,
    
    /// <summary>Right Trigger</summary>
    TriggerR = 0x0004
}
