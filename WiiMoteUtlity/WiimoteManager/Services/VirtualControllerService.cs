using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Text.Json.Serialization;
using WiimoteManager.Models;

namespace WiimoteManager.Services;

public class VirtualControllerService : IDisposable
{
    // Use Singleton for ViGEmClient
    private static ViGEmClient? _client;
    private static readonly object _lock = new();

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IXbox360Controller> _controllers = new();
    private bool _isDisposed;

    public bool IsAvailable => _client != null;

    public string? InitializationError { get; private set; }

    public VirtualControllerService()
    {
        InitializeClient();
    }

    private void InitializeClient()
    {
        lock (_lock)
        {
            if (_client == null)
            {
                try
                {
                    _client = new ViGEmClient();
                    Console.WriteLine("[VIGEM] Client Initialized Successfully");
                }
                catch (Exception ex)
                {
                    // ViGEmBus probably not installed
                    InitializationError = ex.Message;
                    Console.WriteLine($"[ERROR] Failed to initialize ViGEmClient: {ex.Message}");
                }
            }
        }
    }

    public void ConnectController(string deviceKey)
    {
        if (_client == null) 
        {
            InitializeClient();
            if (_client == null) return;
        }
        
        if (!_controllers.ContainsKey(deviceKey))
        {
            try 
            {
                var controller = _client.CreateXbox360Controller();
                controller.Connect();
                _controllers[deviceKey] = controller;
                Console.WriteLine($"[VIGEM] Connected controller for {deviceKey}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VIGEM] Failed to create controller: {ex.Message}");
                throw;
            }
        }
    }

    public void DisconnectController(string deviceKey)
    {
        if (_controllers.TryRemove(deviceKey, out var controller))
        {
            try
            {
                controller.Disconnect();
                Console.WriteLine($"[VIGEM] Disconnected controller for {deviceKey}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VIGEM] Error disconnecting: {ex.Message}");
            }
        }
    }

    public void UpdateController(string deviceKey, WiimoteDevice model, MappingProfile profile)
    {
        if (!_controllers.TryGetValue(deviceKey, out var controller)) return;

        try
        {
            // Reset report
            controller.ResetReport();

            var buttons = model.CurrentButtonState;
            
            // Debug Log (throttled)
            if (model.CurrentButtonState != ButtonState.None && DateTime.Now.Millisecond < 50) 
            {
                 Console.WriteLine($"[VIGEM] Update {deviceKey} -> Buttons: {model.CurrentButtonState}");
            }

            var isRocketLeagueProfile = profile.Name.Equals("Rocket League (Tilt Pro)", StringComparison.OrdinalIgnoreCase);

            // Map Buttons
            if (isRocketLeagueProfile)
            {
                // Hard-locked bindings requested by user:
                // Wiimote 1 -> A (Jump), B -> B (Boost), A -> LT (Brake), 2 -> RT (Accelerate)
                if ((buttons & ButtonState.One) != 0) controller.SetButtonState(Xbox360Button.A, true);
                if ((buttons & ButtonState.B) != 0) controller.SetButtonState(Xbox360Button.B, true);
                if ((buttons & ButtonState.A) != 0) controller.SetSliderValue(Xbox360Slider.LeftTrigger, 255);
                if ((buttons & ButtonState.Two) != 0) controller.SetSliderValue(Xbox360Slider.RightTrigger, 255);
            }
            else
            {
                if (IsPressed(profile.A, buttons)) controller.SetButtonState(Xbox360Button.A, true);
                if (IsPressed(profile.B, buttons)) controller.SetButtonState(Xbox360Button.B, true);
                if (IsPressed(profile.X, buttons)) controller.SetButtonState(Xbox360Button.X, true);
                if (IsPressed(profile.Y, buttons)) controller.SetButtonState(Xbox360Button.Y, true);
                
                if (IsPressed(profile.LeftShoulder, buttons)) controller.SetButtonState(Xbox360Button.LeftShoulder, true);
                if (IsPressed(profile.RightShoulder, buttons)) controller.SetButtonState(Xbox360Button.RightShoulder, true);
                if (IsPressed(profile.LeftTrigger, buttons)) controller.SetSliderValue(Xbox360Slider.LeftTrigger, 255);
                if (IsPressed(profile.RightTrigger, buttons)) controller.SetSliderValue(Xbox360Slider.RightTrigger, 255);
            }
            
            if (IsPressed(profile.Start, buttons)) controller.SetButtonState(Xbox360Button.Start, true);
            if (IsPressed(profile.Back, buttons)) controller.SetButtonState(Xbox360Button.Back, true);
            if (IsPressed(profile.Guide, buttons)) controller.SetButtonState(Xbox360Button.Guide, true);
            
            if (IsPressed(profile.DPadUp, buttons)) controller.SetButtonState(Xbox360Button.Up, true);
            if (IsPressed(profile.DPadDown, buttons)) controller.SetButtonState(Xbox360Button.Down, true);
            if (IsPressed(profile.DPadLeft, buttons)) controller.SetButtonState(Xbox360Button.Left, true);
            if (IsPressed(profile.DPadRight, buttons)) controller.SetButtonState(Xbox360Button.Right, true);

            // Accelerometer Mapping
            if (profile.UseAccelerometer && profile.AccelMapping != null)
            {
                // Rocket League / Racing Mode logic (Horizontal Hold)
                // Tilt Left/Right (Steering) -> Accel Y
                // Tilt Up/Down -> Accel Z (centered around 0)
                
                // Convert acceleration to tilt-angle-based values (more stable than raw accel).
                // For your orientation:
                // - Left tilt:  AccelY positive
                // - Right tilt: AccelY negative
                // - Front tilt: AccelX positive
                // - Back tilt:  AccelX negative
                const float expectedTiltMax = 0.20f;
                float steeringValue = Math.Clamp(model.AccelY / expectedTiltMax, -1.0f, 1.0f);
                float pitchValue = Math.Clamp(model.AccelZ / expectedTiltMax, -1.0f, 1.0f);

                // Apply Sensitivity
                steeringValue *= profile.AccelMapping.Sensitivity;
                pitchValue *= profile.AccelMapping.Sensitivity;

                // Apply Deadzone
                if (Math.Abs(steeringValue) < profile.AccelMapping.DeadZone) steeringValue = 0f;
                if (Math.Abs(pitchValue) < profile.AccelMapping.DeadZone) pitchValue = 0f;

                // Clamp after sensitivity/deadzone
                steeringValue = Math.Clamp(steeringValue, -1.0f, 1.0f);
                pitchValue = Math.Clamp(pitchValue, -1.0f, 1.0f);

                // Smooth response: softer near center, still reaches full deflection at limits.
                steeringValue = MathF.Sign(steeringValue) * MathF.Pow(MathF.Abs(steeringValue), 1.8f);
                pitchValue = MathF.Sign(pitchValue) * MathF.Pow(MathF.Abs(pitchValue), 1.8f);

                if (profile.AccelMapping.InvertAxis)
                {
                    steeringValue = -steeringValue;
                    pitchValue = -pitchValue;
                }

                // Map to Left Stick (Standard for movement/steering)
                // "LeftStick (Steering + Pitch)" is the display string, but binding might just set the string directly.
                // We should check if it starts with "LeftStick (" or is just "LeftStick"
                if (profile.AccelMapping.TargetControl.StartsWith("LeftStick"))
                {
                    // Full Joystick Mode (Steering + Pitch)
                    if (profile.AccelMapping.TargetControl.Contains("Steering + Pitch"))
                    {
                        controller.SetAxisValue(Xbox360Axis.LeftThumbX, (short)(-steeringValue * 32767));
                        controller.SetAxisValue(Xbox360Axis.LeftThumbY, (short)(pitchValue * 32767));
                    }
                    else
                    {
                        // Single-axis LeftStick modes
                        if (profile.AccelMapping.TargetControl.StartsWith("LeftStickX"))
                        {
                            controller.SetAxisValue(Xbox360Axis.LeftThumbX, (short)(-steeringValue * 32767));
                        }
                        else if (profile.AccelMapping.TargetControl.StartsWith("LeftStickY"))
                        {
                            controller.SetAxisValue(Xbox360Axis.LeftThumbY, (short)(pitchValue * 32767));
                        }
                    }
                }
                else
                {
                    // Generic single-axis fallback for RightStick and triggers
                    float rawValue = 0f;
                    string axis = profile.AccelMapping.TiltAxis;
                    if (axis == null || axis.StartsWith("Auto")) axis = "X";

                    switch (axis.Substring(0, 1).ToUpper())
                    {
                        case "X": rawValue = model.AccelX; break;
                        case "Y": rawValue = model.AccelY; break;
                        case "Z": rawValue = model.AccelZ; break;
                        default: rawValue = model.AccelX; break;
                    }

                    if (Math.Abs(rawValue) < profile.AccelMapping.DeadZone) rawValue = 0f;
                    rawValue = Math.Clamp(rawValue * profile.AccelMapping.Sensitivity, -1.0f, 1.0f);
                    if (profile.AccelMapping.InvertAxis) rawValue = -rawValue;

                    string target = profile.AccelMapping.TargetControl.Split(' ')[0];
                    switch (target)
                    {
                        case "RightStickX":
                            controller.SetAxisValue(Xbox360Axis.RightThumbX, (short)(rawValue * 32767));
                            break;
                        case "RightStickY":
                            controller.SetAxisValue(Xbox360Axis.RightThumbY, (short)(rawValue * 32767));
                            break;
                        case "LeftTrigger":
                            controller.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(Math.Max(0, rawValue) * 255));
                            break;
                        case "RightTrigger":
                            controller.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(Math.Max(0, rawValue) * 255));
                            break;
                    }
                }
            }

            // Submit Report
            controller.SubmitReport();
        }
        catch (Exception ex)
        {
             // Log error but don't crash
             Console.WriteLine($"[VIGEM] Update failed: {ex.Message}");
        }
    }

    private bool IsPressed(ControlMapping mapping, ButtonState currentButtons)
    {
        if (mapping.WiimoteButton.HasValue)
        {
            return (currentButtons & mapping.WiimoteButton.Value) != 0;
        }
        return false;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        foreach (var controller in _controllers.Values)
        {
            try { controller.Disconnect(); } catch {}
        }
        _controllers.Clear();
        
        // Don't dispose static client here as it might be shared
        // _client?.Dispose();
        
        _isDisposed = true;
    }
}
