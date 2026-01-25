using CommunityToolkit.Mvvm.ComponentModel;

namespace WiimoteManager.Models;

/// <summary>
/// Represents a Nintendo Wiimote device with its state and properties.
/// </summary>
public partial class WiimoteDevice : ObservableObject
{
    /// <summary>Unique identifier for this device in the local system</summary>
    [ObservableProperty]
    public string deviceId = string.Empty;

    /// <summary>Bluetooth MAC address (e.g., "00:1A:7D:DA:71:13")</summary>
    [ObservableProperty]
    public string bluetoothAddress = string.Empty;

    /// <summary>Friendly device name (typically "Nintendo RVL-CNT-01")</summary>
    [ObservableProperty]
    public string deviceName = string.Empty;

    /// <summary>HID device path for communication</summary>
    [ObservableProperty]
    public string hidPath = string.Empty;

    /// <summary>True if the device is currently paired</summary>
    [ObservableProperty]
    public bool isPaired = false;

    /// <summary>True if the device is connected and actively communicating</summary>
    [ObservableProperty]
    public bool isConnected = false;

    /// <summary>Battery level as percentage (0-100)</summary>
    [ObservableProperty]
    public int batteryLevel = 0;

    /// <summary>Current LED state (combination of LED1-LED4 bitmasks)</summary>
    [ObservableProperty]
    public byte ledState = 0x00;

    /// <summary>True if rumble/vibration is currently active</summary>
    [ObservableProperty]
    public bool isRumbling = false;

    /// <summary>Current button state (bitmask of pressed buttons)</summary>
    [ObservableProperty]
    public ButtonState currentButtonState = ButtonState.None;

    /// <summary>Accelerometer reading for X axis (typical range -1.0 to 1.0)</summary>
    [ObservableProperty]
    public float accelX = 0f;

    /// <summary>Accelerometer reading for Y axis (typical range -1.0 to 1.0)</summary>
    [ObservableProperty]
    public float accelY = 0f;

    /// <summary>Accelerometer reading for Z axis (typical range -1.0 to 1.0, normally ~+1.0 for gravity)</summary>
    [ObservableProperty]
    public float accelZ = 1f;

    /// <summary>Type of extension controller attached (Nunchuk, Classic, etc.)</summary>
    [ObservableProperty]
    public ExtensionType extensionType = ExtensionType.None;

    /// <summary>Nunchuk state (if extension is Nunchuk)</summary>
    [ObservableProperty]
    public NunchukState? nunchukState;

    /// <summary>Classic Controller state (if extension is ClassicController)</summary>
    [ObservableProperty]
    public ClassicControllerState? classicControllerState;

    /// <summary>Timestamp of last communication</summary>
    [ObservableProperty]
    public DateTime lastCommunication = DateTime.UtcNow;

    /// <summary>Signal strength indicator (typically 0-100, platform-dependent)</summary>
    [ObservableProperty]
    public int signalStrength = 0;

    /// <summary>Optional: User-assigned alias for this Wiimote</summary>
    [ObservableProperty]
    public string? userAlias;

    /// <summary>
    /// Creates a new Wiimote device instance.
    /// </summary>
    public WiimoteDevice()
    {
    }

    /// <summary>
    /// Creates a new Wiimote device with initial Bluetooth address.
    /// </summary>
    public WiimoteDevice(string bluetoothAddress, string deviceName = "Nintendo RVL-CNT-01")
    {
        BluetoothAddress = bluetoothAddress;
        DeviceName = deviceName;
        DeviceId = bluetoothAddress.Replace(":", "").ToLower();
    }

    /// <summary>
    /// Gets the display name, preferring user alias if set.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(UserAlias) 
        ? $"{DeviceName} ({BluetoothAddress})" 
        : $"{UserAlias} ({BluetoothAddress})";

    /// <summary>
    /// Gets a human-readable battery status string.
    /// </summary>
    public string BatteryStatus => BatteryLevel switch
    {
        >= 75 => "Good",
        >= 50 => "Fair",
        >= 25 => "Low",
        _ => "Critical"
    };

    /// <summary>
    /// Gets connection status as a human-readable string.
    /// </summary>
    public string StatusText => IsConnected 
        ? (IsPaired ? "Connected" : "Paired")
        : (IsPaired ? "Disconnected" : "Not Paired");

    /// <summary>
    /// Updates the last communication timestamp to now.
    /// </summary>
    public void UpdateLastCommunication()
    {
        LastCommunication = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets all sensor values to neutral/default states.
    /// </summary>
    public void ResetSensorData()
    {
        CurrentButtonState = ButtonState.None;
        AccelX = 0f;
        AccelY = 0f;
        AccelZ = 1f;
        NunchukState = null;
        ClassicControllerState = null;
    }

    public override string ToString() => DisplayName;
}

/// <summary>
/// Represents the state of a Nunchuk extension controller.
/// </summary>
public partial class NunchukState : ObservableObject
{
    /// <summary>Analog stick X position (typically -1.0 to 1.0)</summary>
    [ObservableProperty]
    public float stickX = 0f;

    /// <summary>Analog stick Y position (typically -1.0 to 1.0)</summary>
    [ObservableProperty]
    public float stickY = 0f;

    /// <summary>Nunchuk accelerometer X axis</summary>
    [ObservableProperty]
    public float accelX = 0f;

    /// <summary>Nunchuk accelerometer Y axis</summary>
    [ObservableProperty]
    public float accelY = 0f;

    /// <summary>Nunchuk accelerometer Z axis</summary>
    [ObservableProperty]
    public float accelZ = 1f;

    /// <summary>Button state (C and Z buttons)</summary>
    [ObservableProperty]
    public NunchukButtons buttonState = NunchukButtons.None;

    /// <summary>True if C button is pressed</summary>
    public bool IsButtonC => ButtonState.HasFlag(NunchukButtons.C);

    /// <summary>True if Z button is pressed</summary>
    public bool IsButtonZ => ButtonState.HasFlag(NunchukButtons.Z);
}

/// <summary>
/// Represents the state of a Classic Controller extension.
/// </summary>
public partial class ClassicControllerState : ObservableObject
{
    /// <summary>Left analog stick X position (-1.0 to 1.0)</summary>
    [ObservableProperty]
    public float leftStickX = 0f;

    /// <summary>Left analog stick Y position (-1.0 to 1.0)</summary>
    [ObservableProperty]
    public float leftStickY = 0f;

    /// <summary>Right analog stick X position (-1.0 to 1.0)</summary>
    [ObservableProperty]
    public float rightStickX = 0f;

    /// <summary>Right analog stick Y position (-1.0 to 1.0)</summary>
    [ObservableProperty]
    public float rightStickY = 0f;

    /// <summary>Left trigger analog value (0.0 to 1.0)</summary>
    [ObservableProperty]
    public float triggerLeft = 0f;

    /// <summary>Right trigger analog value (0.0 to 1.0)</summary>
    [ObservableProperty]
    public float triggerRight = 0f;

    /// <summary>Button state bitmask</summary>
    [ObservableProperty]
    public ClassicControllerButtons buttonState = ClassicControllerButtons.None;
}
