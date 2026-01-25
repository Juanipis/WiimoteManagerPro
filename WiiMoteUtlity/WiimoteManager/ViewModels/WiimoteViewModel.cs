using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiimoteManager.Models;
using WiimoteManager.Services;

namespace WiimoteManager.ViewModels;

/// <summary>
/// ViewModel for a single Wiimote device.
/// Manages device state, button inputs, and control outputs.
/// </summary>
public partial class WiimoteViewModel : ObservableObject, IDisposable
{
    private readonly WiimoteService _wiimoteService;
    private CancellationTokenSource? _readLoopCancellation;
    private bool _disposed = false;

    /// <summary>
    /// The underlying WiimoteDevice model.
    /// </summary>
    [ObservableProperty]
    public WiimoteDevice device;

    /// <summary>
    /// True if this Wiimote is currently reading data.
    /// </summary>
    [ObservableProperty]
    public bool isReading = false;

    /// <summary>
    /// True if LED1 is currently on.
    /// </summary>
    [ObservableProperty]
    public bool isLed1On = false;

    /// <summary>
    /// True if LED2 is currently on.
    /// </summary>
    [ObservableProperty]
    public bool isLed2On = false;

    /// <summary>
    /// True if LED3 is currently on.
    /// </summary>
    [ObservableProperty]
    public bool isLed3On = false;

    /// <summary>
    /// True if LED4 is currently on.
    /// </summary>
    [ObservableProperty]
    public bool isLed4On = false;

    /// <summary>
    /// Current vibration intensity (0.0 to 1.0).
    /// </summary>
    [ObservableProperty]
    public float vibrationIntensity = 0f;

    /// <summary>
    /// Tilt angle in degrees (X axis).
    /// </summary>
    [ObservableProperty]
    public float tiltX = 0f;

    /// <summary>
    /// Tilt angle in degrees (Y axis).
    /// </summary>
    [ObservableProperty]
    public float tiltY = 0f;

    /// <summary>
    /// Battery percentage.
    /// </summary>
    [ObservableProperty]
    public int batteryPercent = 0;

    /// <summary>
    /// Button press indicators (text display).
    /// </summary>
    [ObservableProperty]
    public string pressedButtons = "None";

    /// <summary>
    /// Status display text.
    /// </summary>
    [ObservableProperty]
    public string statusText = "Disconnected";

    public WiimoteViewModel(WiimoteDevice device, WiimoteService wiimoteService)
    {
        Device = device;
        _wiimoteService = wiimoteService;

        // Subscribe to device property changes
        Device.PropertyChanged += (s, e) =>
        {
            OnDevicePropertyChanged(e.PropertyName);
        };
    }

    /// <summary>
    /// Connects to the Wiimote and starts reading data.
    /// Note: With WiimoteService, connection is already established.
    /// This just updates UI state.
    /// </summary>
    [RelayCommand]
    public async Task Connect()
    {
        try
        {
            StatusText = "Connected";
            IsReading = true;
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusText = $"Connection error: {ex.Message}";
            Device.IsConnected = false;
        }
    }

    /// <summary>
    /// Disconnects from the Wiimote and stops reading data.
    /// Note: WiimoteService handles the actual disconnection.
    /// </summary>
    [RelayCommand]
    public async Task Disconnect()
    {
        try
        {
            StatusText = "Disconnecting...";
            IsReading = false;
            Device.IsConnected = false;
            Device.ResetSensorData();
            StatusText = "Disconnected";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusText = $"Disconnect error: {ex.Message}";
        }
    }

    /// <summary>
    /// Toggles LED1 on/off.
    /// </summary>
    [RelayCommand]
    public async Task ToggleLED1()
    {
        IsLed1On = !IsLed1On;
        await UpdateLEDs();
    }

    /// <summary>
    /// Toggles LED2 on/off.
    /// </summary>
    [RelayCommand]
    public async Task ToggleLED2()
    {
        IsLed2On = !IsLed2On;
        await UpdateLEDs();
    }

    /// <summary>
    /// Toggles LED3 on/off.
    /// </summary>
    [RelayCommand]
    public async Task ToggleLED3()
    {
        IsLed3On = !IsLed3On;
        await UpdateLEDs();
    }

    /// <summary>
    /// Toggles LED4 on/off.
    /// </summary>
    [RelayCommand]
    public async Task ToggleLED4()
    {
        IsLed4On = !IsLed4On;
        await UpdateLEDs();
    }

    /// <summary>
    /// Sends a rumble/vibration command.
    /// </summary>
    [RelayCommand]
    public async Task ToggleRumble()
    {
        bool shouldRumble = VibrationIntensity == 0f;
        VibrationIntensity = shouldRumble ? 1.0f : 0f;

        byte ledMask = CalculateLEDMask();
        await _wiimoteService.SetLEDAsync(Device.DeviceId, ledMask, shouldRumble);
    }

    /// <summary>
    /// Updates LEDs based on current toggle states.
    /// </summary>
    private async Task UpdateLEDs()
    {
        byte ledMask = CalculateLEDMask();
        await _wiimoteService.SetLEDAsync(Device.DeviceId, ledMask, Device.IsRumbling);
    }

    /// <summary>
    /// Calculates the LED bitmask from current toggle states.
    /// </summary>
    private byte CalculateLEDMask()
    {
        byte mask = 0x00;
        if (IsLed1On) mask |= WiimoteReports.LED1;
        if (IsLed2On) mask |= WiimoteReports.LED2;
        if (IsLed3On) mask |= WiimoteReports.LED3;
        if (IsLed4On) mask |= WiimoteReports.LED4;
        return mask;
    }

    /// <summary>
    /// Updates button display text based on current button state.
    /// </summary>
    private void UpdateButtonDisplay()
    {
        var buttons = new List<string>();

        if (Device.CurrentButtonState.HasFlag(ButtonState.A)) buttons.Add("A");
        if (Device.CurrentButtonState.HasFlag(ButtonState.B)) buttons.Add("B");
        if (Device.CurrentButtonState.HasFlag(ButtonState.One)) buttons.Add("1");
        if (Device.CurrentButtonState.HasFlag(ButtonState.Two)) buttons.Add("2");
        if (Device.CurrentButtonState.HasFlag(ButtonState.Plus)) buttons.Add("+");
        if (Device.CurrentButtonState.HasFlag(ButtonState.Minus)) buttons.Add("-");
        if (Device.CurrentButtonState.HasFlag(ButtonState.Home)) buttons.Add("Home");
        if (Device.CurrentButtonState.HasFlag(ButtonState.DPadUp)) buttons.Add("↑");
        if (Device.CurrentButtonState.HasFlag(ButtonState.DPadDown)) buttons.Add("↓");
        if (Device.CurrentButtonState.HasFlag(ButtonState.DPadLeft)) buttons.Add("←");
        if (Device.CurrentButtonState.HasFlag(ButtonState.DPadRight)) buttons.Add("→");

        PressedButtons = buttons.Count > 0 ? string.Join(", ", buttons) : "None";
    }

    /// <summary>
    /// Updates tilt angles based on accelerometer data.
    /// </summary>
    private void UpdateTiltDisplay()
    {
        // Convert accelerometer values (-1 to 1) to tilt angle (-90 to 90 degrees)
        // Using simplified atan2 approximation
        TiltX = (float)Math.Atan2(Device.AccelX, Device.AccelZ) * 57.2958f; // Convert radians to degrees
        TiltY = (float)Math.Atan2(Device.AccelY, Device.AccelZ) * 57.2958f;
    }

    /// <summary>
    /// Handles property changes on the underlying device.
    /// </summary>
    private void OnDevicePropertyChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(WiimoteDevice.BatteryLevel):
                BatteryPercent = Device.BatteryLevel;
                break;

            case nameof(WiimoteDevice.CurrentButtonState):
                UpdateButtonDisplay();
                break;

            case nameof(WiimoteDevice.AccelX):
            case nameof(WiimoteDevice.AccelY):
            case nameof(WiimoteDevice.AccelZ):
                UpdateTiltDisplay();
                break;

            case nameof(WiimoteDevice.IsConnected):
                StatusText = Device.IsConnected ? "Connected" : "Disconnected";
                break;

            case nameof(WiimoteDevice.LedState):
                IsLed1On = (Device.LedState & WiimoteReports.LED1) != 0;
                IsLed2On = (Device.LedState & WiimoteReports.LED2) != 0;
                IsLed3On = (Device.LedState & WiimoteReports.LED3) != 0;
                IsLed4On = (Device.LedState & WiimoteReports.LED4) != 0;
                break;

            case nameof(WiimoteDevice.IsRumbling):
                VibrationIntensity = Device.IsRumbling ? 1.0f : 0f;
                break;
        }
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _readLoopCancellation?.Dispose();
        _disposed = true;
    }
}
