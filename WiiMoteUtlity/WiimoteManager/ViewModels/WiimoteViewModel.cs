using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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
    private readonly VirtualControllerService _virtualControllerService;
    private readonly ProfileService _profileService;
    private readonly ProcessMonitorService _processMonitor;
    private CancellationTokenSource? _readLoopCancellation = new();
    private bool _disposed = false;
    private MappingProfile _mappingProfile = new();

    /// <summary>
    /// The underlying WiimoteDevice model.
    /// </summary>
    [ObservableProperty]
    public WiimoteDevice device;

    /// <summary>
    /// Available profiles.
    /// </summary>
    [ObservableProperty]
    public ObservableCollection<string> profileNames = new();

    /// <summary>
    /// Currently selected profile name.
    /// </summary>
    [ObservableProperty]
    public string selectedProfileName = "Default";

    /// <summary>
    /// True if virtual Xbox controller emulation is enabled.
    /// </summary>
    [ObservableProperty]
    public bool isEmulationEnabled = false;

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
        _virtualControllerService = new VirtualControllerService(); // New service instance per Wiimote for now
        _profileService = new ProfileService();
        _processMonitor = new ProcessMonitorService(_profileService);
        
        // Subscribe to auto-profile switching
        _processMonitor.ProfileSwitchRequested += OnProfileSwitchRequested;
        _processMonitor.IsEnabled = true; // Enable by default

        RefreshProfiles();
        
        // Load default or first available
        if (ProfileNames.Contains("Default"))
            SelectedProfileName = "Default";
        else if (ProfileNames.Count > 0)
            SelectedProfileName = ProfileNames[0];

        // FIX: Initialize StatusText with current connection state
        StatusText = Device.IsConnected ? "Connected" : "Disconnected";

            // Subscribe to device property changes
        Device.PropertyChanged += (s, e) =>
        {
            // 1. FAST PATH: Update Virtual Controller (Background Thread)
            if (e.PropertyName == nameof(WiimoteDevice.CurrentButtonState) && IsEmulationEnabled)
            {
                try
                {
                    _virtualControllerService.UpdateController(Device.DeviceId, Device, _mappingProfile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ViewModel] Controller update error: {ex.Message}");
                }
            }

            // 2. SLOW PATH: Update UI (Main Thread)
            Application.Current?.Dispatcher.Invoke(() => 
            {
                OnDevicePropertyChanged(e.PropertyName);
            });
        };
    }

    partial void OnSelectedProfileNameChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _mappingProfile = _profileService.LoadProfile(value);
            _mappingProfile.RecordUsage(); // Track usage
            _profileService.SaveProfile(_mappingProfile); // Save usage stats
        }
    }

    private void RefreshProfiles()
    {
        var current = SelectedProfileName;
        ProfileNames.Clear();
        foreach (var name in _profileService.GetProfileNames())
        {
            ProfileNames.Add(name);
        }
        
        if (ProfileNames.Contains(current))
            SelectedProfileName = current;
        else if (ProfileNames.Contains("Default"))
            SelectedProfileName = "Default";
    }

    [RelayCommand]
    public void CreateProfile()
    {
        // Create a copy of the current profile
        var newProfile = _profileService.CreateNewProfile(SelectedProfileName + " Copy");
        RefreshProfiles();
        SelectedProfileName = newProfile.Name;
    }

    [RelayCommand]
    public void DeleteProfile()
    {
        if (SelectedProfileName == "Default") return;

        _profileService.DeleteProfile(SelectedProfileName);
        RefreshProfiles();
    }

    private void SaveCurrentProfile()
    {
        if (_mappingProfile != null)
        {
            // Ensure the profile name matches the selection (if we allow renaming later)
            // For now, we assume the file name is the source of truth for the list
            _profileService.SaveProfile(_mappingProfile);
        }
    }

    /// <summary>
    /// Toggles virtual controller emulation.
    /// </summary>
    [RelayCommand]
    public async Task ToggleEmulation()
    {
        // FIX: Double-toggle issue
        // Since ToggleButton is bound to IsEmulationEnabled (TwoWay) AND calls this Command,
        // clicking it toggles the property once (via binding) and then running the command
        // might toggle it AGAIN if we do `IsEmulationEnabled = !IsEmulationEnabled`.
        
        // Instead, we should rely on the property setter to trigger the logic, 
        // OR decouple the command from the toggle button if the property binding handles it.
        
        // BUT, since we need to handle "Driver Unavailable" logic which might revert the toggle,
        // it's safer to handle the logic in the property setter or a dedicated method called by the setter.
        
        // For now, let's assume the binding toggled it. We just need to sync the state.
        // Wait... standard ToggleButton with Command usually fires Command AFTER changing IsChecked.
        
        // Let's force the state based on the current value, which should be the DESIRED state
        bool desiredState = IsEmulationEnabled;
        
        if (!_virtualControllerService.IsAvailable)
        {
            StatusText = $"Emulation Unavailable: {_virtualControllerService.InitializationError ?? "ViGEmBus driver not found."}";
            if (desiredState) IsEmulationEnabled = false; // Revert
            return;
        }

        // Logic is now effectively handled by the setter property change, 
        // OR we need to invoke it here if the logic wasn't in the setter.
        
        // Let's MOVE the logic to a separate method called HandleEmulationStateChange
        // and call it here to ensure it runs.
        
        await HandleEmulationStateChange(desiredState);
    }

    partial void OnIsEmulationEnabledChanged(bool value)
    {
        // This method is automatically generated by CommunityToolkit.Mvvm
        // when [ObservableProperty] is used. We use it to trigger logic.
        _ = HandleEmulationStateChange(value);
    }

    private async Task HandleEmulationStateChange(bool enabled)
    {
        if (enabled)
        {
            try 
            {
                Console.WriteLine($"[ViewModel] Connecting Virtual Controller for {Device.DeviceId}...");
                _virtualControllerService.ConnectController(Device.DeviceId);
                StatusText = "Virtual Controller Connected";
                Console.WriteLine($"[ViewModel] Emulation Started for {Device.DeviceId}");
            }
            catch (Exception ex)
            {
                 StatusText = $"Emulation Error: {ex.Message}";
                 // If we failed, we must set it back to false, but be careful of infinite loops
                 if (IsEmulationEnabled) IsEmulationEnabled = false;
                 Console.WriteLine($"[ViewModel] Emulation Failed: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[ViewModel] Disconnecting Virtual Controller for {Device.DeviceId}...");
            _virtualControllerService.DisconnectController(Device.DeviceId);
            StatusText = "Virtual Controller Disconnected";
        }
        
        await Task.CompletedTask;
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

        _wiimoteService.SetRumble(Device.DeviceId, shouldRumble);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Opens the mapping configuration window.
    /// </summary>
    [RelayCommand]
    public void OpenMapping()
    {
        var mappingViewModel = new MappingViewModel(_mappingProfile, Device, () => 
        {
            SaveCurrentProfile();
        });
        
        var mappingWindow = new Views.MappingWindow(mappingViewModel);
        mappingWindow.ShowDialog();
    }

    /// <summary>
    /// Opens the button test diagnostic window.
    /// </summary>
    [RelayCommand]
    public void OpenButtonTest()
    {
        var testViewModel = new ButtonTestViewModel(Device, _wiimoteService);
        var testWindow = new Views.ButtonTestWindow(testViewModel);
        testWindow.Show();
    }

    /// <summary>
    /// Updates LEDs based on current toggle states.
    /// </summary>
    private async Task UpdateLEDs()
    {
        byte ledMask = CalculateLEDMask();
        _wiimoteService.SetLED(Device.DeviceId, ledMask);
        await Task.CompletedTask;
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
                // Controller update is now handled in the background thread directly
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
    /// Handles automatic profile switching when a game is detected
    /// </summary>
    private void OnProfileSwitchRequested(object? sender, ProfileSwitchEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (ProfileNames.Contains(e.ProfileName))
            {
                SelectedProfileName = e.ProfileName;
                StatusText = $"Auto-switched to: {e.ProfileName}";
                Console.WriteLine($"[WiimoteViewModel] Auto-switched to profile '{e.ProfileName}' for {e.ProcessName}");
            }
        });
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _processMonitor?.Dispose();
        _virtualControllerService?.Dispose();
        _readLoopCancellation?.Dispose();
        _disposed = true;
    }
}
