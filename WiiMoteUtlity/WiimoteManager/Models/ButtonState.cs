namespace WiimoteManager.Models;

/// <summary>
/// Represents the button state bitmask for Wiimote core buttons.
/// Maps to the 16-bit button data reported in HID reports 0x30, 0x31, 0x35, etc.
/// </summary>
[Flags]
public enum ButtonState : ushort
{
    /// <summary>No buttons pressed</summary>
    None = 0x0000,
    
    /// <summary>D-Pad Left (bit 0, byte 0)</summary>
    DPadLeft = 0x0001,
    
    /// <summary>D-Pad Right (bit 1, byte 0)</summary>
    DPadRight = 0x0002,
    
    /// <summary>D-Pad Down (bit 2, byte 0)</summary>
    DPadDown = 0x0004,
    
    /// <summary>D-Pad Up (bit 3, byte 0)</summary>
    DPadUp = 0x0008,
    
    /// <summary>Plus button (bit 4, byte 0)</summary>
    Plus = 0x0010,
    
    /// <summary>Reserved (bit 5-6, byte 0)</summary>
    Reserved1 = 0x0060,
    
    /// <summary>Two button (bit 7, byte 0)</summary>
    Two = 0x0100,
    
    /// <summary>One button (bit 0, byte 1)</summary>
    One = 0x0200,
    
    /// <summary>B button (bit 1, byte 1)</summary>
    B = 0x0400,
    
    /// <summary>A button (bit 2, byte 1)</summary>
    A = 0x0800,
    
    /// <summary>Minus button (bit 3, byte 1)</summary>
    Minus = 0x1000,
    
    /// <summary>Reserved (bit 4-5, byte 1)</summary>
    Reserved2 = 0x6000,
    
    /// <summary>Home button (bit 7, byte 1)</summary>
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
