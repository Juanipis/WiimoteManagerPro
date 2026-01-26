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

            // Map Buttons
            if (IsPressed(profile.A, buttons)) controller.SetButtonState(Xbox360Button.A, true);
            if (IsPressed(profile.B, buttons)) controller.SetButtonState(Xbox360Button.B, true);
            if (IsPressed(profile.X, buttons)) controller.SetButtonState(Xbox360Button.X, true);
            if (IsPressed(profile.Y, buttons)) controller.SetButtonState(Xbox360Button.Y, true);
            
            if (IsPressed(profile.LeftShoulder, buttons)) controller.SetButtonState(Xbox360Button.LeftShoulder, true);
            if (IsPressed(profile.RightShoulder, buttons)) controller.SetButtonState(Xbox360Button.RightShoulder, true);
            
            if (IsPressed(profile.Start, buttons)) controller.SetButtonState(Xbox360Button.Start, true);
            if (IsPressed(profile.Back, buttons)) controller.SetButtonState(Xbox360Button.Back, true);
            if (IsPressed(profile.Guide, buttons)) controller.SetButtonState(Xbox360Button.Guide, true);
            
            if (IsPressed(profile.DPadUp, buttons)) controller.SetButtonState(Xbox360Button.Up, true);
            if (IsPressed(profile.DPadDown, buttons)) controller.SetButtonState(Xbox360Button.Down, true);
            if (IsPressed(profile.DPadLeft, buttons)) controller.SetButtonState(Xbox360Button.Left, true);
            if (IsPressed(profile.DPadRight, buttons)) controller.SetButtonState(Xbox360Button.Right, true);

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
