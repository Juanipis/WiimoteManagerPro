using System.Diagnostics;
using WiimoteManager.Models;

namespace WiimoteManager.Services;

/// <summary>
/// Monitors running processes to automatically switch profiles based on active game
/// </summary>
public class ProcessMonitorService : IDisposable
{
    private readonly ProfileService _profileService;
    private readonly Timer? _monitorTimer;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);
    private string? _lastDetectedProcess;
    private bool _isEnabled;
    private bool _disposed;
    
    public event EventHandler<ProfileSwitchEventArgs>? ProfileSwitchRequested;
    
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (!_isEnabled)
            {
                _lastDetectedProcess = null;
            }
        }
    }
    
    public ProcessMonitorService(ProfileService profileService)
    {
        _profileService = profileService;
        _monitorTimer = new Timer(MonitorProcesses, null, TimeSpan.Zero, _pollInterval);
        _isEnabled = false; // Start disabled
    }
    
    private void MonitorProcesses(object? state)
    {
        if (!_isEnabled || _disposed) return;
        
        try
        {
            // Get foreground window process
            var foregroundProcess = GetForegroundProcess();
            if (foregroundProcess == null) return;
            
            string processName = foregroundProcess.ProcessName.ToLowerInvariant();
            
            // Skip if same as last detected
            if (_lastDetectedProcess == processName) return;
            
            // Find matching profile
            var profiles = _profileService.GetProfiles();
            var matchingProfile = profiles.FirstOrDefault(p => 
                p.AssociatedGames.Any(game => 
                    processName.Contains(game.ToLowerInvariant().Replace(" ", "")) ||
                    game.ToLowerInvariant().Replace(" ", "").Contains(processName)
                )
            );
            
            if (matchingProfile != null)
            {
                _lastDetectedProcess = processName;
                ProfileSwitchRequested?.Invoke(this, new ProfileSwitchEventArgs
                {
                    ProfileName = matchingProfile.Name,
                    ProcessName = foregroundProcess.ProcessName,
                    Reason = $"Detected running game: {foregroundProcess.MainWindowTitle}"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessMonitor] Error monitoring processes: {ex.Message}");
        }
    }
    
    private Process? GetForegroundProcess()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;
            
            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return null;
            
            return Process.GetProcessById((int)processId);
        }
        catch
        {
            return null;
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _monitorTimer?.Dispose();
        _disposed = true;
    }
    
    #region Win32 API
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    #endregion
}

public class ProfileSwitchEventArgs : EventArgs
{
    public required string ProfileName { get; init; }
    public required string ProcessName { get; init; }
    public required string Reason { get; init; }
}
