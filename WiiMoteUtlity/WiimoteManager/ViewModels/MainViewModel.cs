using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiimoteManager.Models;
using WiimoteManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace WiimoteManager.ViewModels;

/// <summary>
/// Main ViewModel for the application.
/// Manages the list of connected Wiimotes and orchestrates discovery/pairing.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly WiimoteService _wiimoteService;
    private CancellationTokenSource? _discoveryCancellation = new();

    /// <summary>
    /// Collection of currently connected/detected Wiimotes.
    /// </summary>
    [ObservableProperty]
    public ObservableCollection<WiimoteViewModel> connectedWiimotes = new();

    /// <summary>
    /// Current status message displayed to the user.
    /// </summary>
    [ObservableProperty]
    public string statusMessage = "Ready. Click 'Scan & Sync' to discover Wiimotes.";

    /// <summary>
    /// True if discovery is currently in progress.
    /// </summary>
    [ObservableProperty]
    public bool isDiscovering = false;
    
    /// <summary>
    /// Debug log messages for troubleshooting.
    /// </summary>
    [ObservableProperty]
    public string debugLog = "";

    /// <summary>
    /// True if any Wiimotes are connected.
    /// </summary>
    [ObservableProperty]
    public bool hasConnectedDevices = false;

    /// <summary>
    /// Number of discovered but not yet paired devices.
    /// </summary>
    [ObservableProperty]
    public int discoveredDeviceCount = 0;

    private Window? _mainWindow;

    public MainViewModel()
    {
        _wiimoteService = new WiimoteService();

        // Subscribe to service events
        _wiimoteService.WiimoteConnected += OnWiimoteConnected;
        _wiimoteService.WiimoteDisconnected += OnWiimoteDisconnected;
        _wiimoteService.ProgressUpdate += OnProgressUpdate;
        
        AddDebugLog("MainViewModel initialized");
    }
    
    public void SetWindow(Window window)
    {
        _mainWindow = window;
    }
    
    private void AddDebugLog(string? message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var safeMessage = message ?? "null";
        DebugLog += $"[{timestamp}] {safeMessage}\n";
        System.Diagnostics.Debug.WriteLine($"[WiimoteManager] {safeMessage}");
        Console.WriteLine($"[WiimoteManager] {safeMessage}");
    }

    /// <summary>
    /// Initializes the ViewModel and services.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            AddDebugLog("Starting initialization...");
            
            // WiimoteService doesn't need window initialization
            StatusMessage = "Ready. Click 'Scan & Sync' to discover Wiimotes.";
            AddDebugLog("Initialization complete");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Initialization failed: {ex.Message}";
            AddDebugLog($"ERROR: Initialization failed - {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Scans for paired Wiimotes and connects to them.
    /// </summary>
    [RelayCommand]
    public async Task ScanDevices()
    {
        if (IsDiscovering)
        {
            return;
        }

        AddDebugLog("=== STARTING WIIMOTE SCAN ===");
        IsDiscovering = true;
        StatusMessage = "Searching for Wiimotes...";
        DiscoveredDeviceCount = 0;

        try
        {
            AddDebugLog("Connecting to Wiimotes...");
            var devices = await _wiimoteService.StartDiscoveryAsync();
            
            DiscoveredDeviceCount = devices.Count;
            AddDebugLog($"Scan completed. Found {devices.Count} Wiimote(s)");
            
            if (devices.Count > 0)
            {
                StatusMessage = $"Found {devices.Count} Wiimote(s)!";
            }
            else
            {
                StatusMessage = "No Wiimotes found. Press RED SYNC button and try again.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message ?? "Unknown error"}";
            AddDebugLog($"ERROR: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            IsDiscovering = false;
            AddDebugLog("=== SCAN COMPLETE ===");
        }
    }

    /// <summary>
    /// Stops the current discovery scan.
    /// </summary>
    [RelayCommand]
    public async Task StopScan()
    {
        _discoveryCancellation?.Cancel();
        IsDiscovering = false;
        StatusMessage = "Scan stopped.";
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disconnects all connected Wiimotes.
    /// </summary>
    [RelayCommand]
    public async Task DisconnectAll()
    {
        foreach (var vm in ConnectedWiimotes.ToList())
        {
            await vm.Disconnect();
        }

        StatusMessage = "All Wiimotes disconnected.";
        UpdateConnectionStatus();
    }

    /// <summary>
    /// Clears the list of discovered devices.
    /// </summary>
    [RelayCommand]
    public void ClearDiscoveredDevices()
    {
        ConnectedWiimotes.Clear();
        DiscoveredDeviceCount = 0;
        StatusMessage = "Discovered devices cleared.";
        UpdateConnectionStatus();
    }

    /// <summary>
    /// Handles Wiimote connection event.
    /// </summary>
    private void OnWiimoteConnected(object? sender, WiimoteDevice device)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (device == null)
            {
                AddDebugLog("[ERROR] Received null Wiimote device!");
                return;
            }
            
            AddDebugLog($"Wiimote connected: {device.DisplayName}");
            
            // Check if device already exists
            var existingVm = ConnectedWiimotes.FirstOrDefault(
                vm => vm.Device.DeviceId == device.DeviceId);

            if (existingVm == null)
            {
                var vm = new WiimoteViewModel(device, _wiimoteService);
                ConnectedWiimotes.Add(vm);
                StatusMessage = $"Connected: {device.DisplayName}";
                UpdateConnectionStatus();
            }
        });
    }

    /// <summary>
    /// Handles Wiimote disconnection event.
    /// </summary>
    private void OnWiimoteDisconnected(object? sender, WiimoteDevice device)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            AddDebugLog($"Wiimote disconnected: {device.DeviceName}");
            UpdateConnectionStatus();
        });
    }

    /// <summary>
    /// Handles progress updates from WiimoteService.
    /// </summary>
    private void OnProgressUpdate(object? sender, string? message)
    {
        // Safe dispatch to UI thread
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            StatusMessage = message ?? "Unknown progress";
            // AddDebugLog($"{message ?? "null"}"); // Too verbose for UI log
        });
    }

    /// <summary>
    /// Updates the HasConnectedDevices property.
    /// </summary>
    private void UpdateConnectionStatus()
    {
        HasConnectedDevices = ConnectedWiimotes.Any(vm => vm.Device.IsConnected);
    }

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _discoveryCancellation?.Dispose();
        _wiimoteService?.Dispose();

        foreach (var vm in ConnectedWiimotes)
        {
            vm.Dispose();
        }
    }
}
