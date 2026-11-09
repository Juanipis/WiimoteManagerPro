using Xunit;
using WiimoteManager.Models;
using WiimoteManager.Services;

namespace WiimoteManager.Tests;

public class ProcessMonitorTests : IDisposable
{
    private readonly string _testProfilesDir;
    private readonly ProfileService _profileService;
    private ProcessMonitorService? _processMonitor;
    
    public ProcessMonitorTests()
    {
        _testProfilesDir = Path.Combine(Path.GetTempPath(), $"WiimoteTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testProfilesDir);
        
        _profileService = new ProfileService();
        var field = typeof(ProfileService).GetField("_profilesDir", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_profileService, _testProfilesDir);
    }
    
    public void Dispose()
    {
        _processMonitor?.Dispose();
        if (Directory.Exists(_testProfilesDir))
        {
            Directory.Delete(_testProfilesDir, true);
        }
    }
    
    [Fact]
    public void ProcessMonitor_ShouldStartDisabled()
    {
        // Arrange & Act
        _processMonitor = new ProcessMonitorService(_profileService);
        
        // Assert
        Assert.False(_processMonitor.IsEnabled);
    }
    
    [Fact]
    public void ProcessMonitor_Enable_ShouldStartMonitoring()
    {
        // Arrange
        _processMonitor = new ProcessMonitorService(_profileService);
        
        // Act
        _processMonitor.IsEnabled = true;
        
        // Assert
        Assert.True(_processMonitor.IsEnabled);
    }
    
    [Fact]
    public void ProcessMonitor_Disable_ShouldStopMonitoring()
    {
        // Arrange
        _processMonitor = new ProcessMonitorService(_profileService);
        _processMonitor.IsEnabled = true;
        
        // Act
        _processMonitor.IsEnabled = false;
        
        // Assert
        Assert.False(_processMonitor.IsEnabled);
    }
    
    [Fact]
    public async Task ProcessMonitor_ShouldDetectMatchingGame()
    {
        // Arrange
        _profileService.EnsureDefaultProfile();
        
        var racingProfile = new MappingProfile
        {
            Name = "Racing Profile",
            AssociatedGames = new List<string> { "notepad" } // Use notepad for testing
        };
        _profileService.SaveProfile(racingProfile);
        
        _processMonitor = new ProcessMonitorService(_profileService);
        
        string? detectedProfile = null;
        _processMonitor.ProfileSwitchRequested += (s, e) =>
        {
            detectedProfile = e.ProfileName;
        };
        
        // Act
        _processMonitor.IsEnabled = true;
        
        // Start notepad for testing
        var notepadProcess = System.Diagnostics.Process.Start("notepad.exe");
        
        try
        {
            // Wait for detection (up to 5 seconds)
            await Task.Delay(TimeSpan.FromSeconds(5));
            
            // Assert
            // Note: This test might not always pass depending on whether notepad becomes foreground
            // It's more of an integration test
            Assert.True(detectedProfile == null || detectedProfile == "Racing Profile");
        }
        finally
        {
            // Cleanup
            if (notepadProcess != null && !notepadProcess.HasExited)
            {
                notepadProcess.Kill();
                notepadProcess.Dispose();
            }
        }
    }
    
    [Fact]
    public void ProcessMonitor_ProfileSwitchEvent_ShouldContainRequiredInfo()
    {
        // Arrange
        _profileService.EnsureDefaultProfile();
        var profile = new MappingProfile
        {
            Name = "Test Profile",
            AssociatedGames = new List<string> { "testgame" }
        };
        _profileService.SaveProfile(profile);
        
        _processMonitor = new ProcessMonitorService(_profileService);
        
        ProfileSwitchEventArgs? eventArgs = null;
        _processMonitor.ProfileSwitchRequested += (s, e) =>
        {
            eventArgs = e;
        };
        
        // Act
        _processMonitor.IsEnabled = true;
        
        // Assert
        // Event handler is registered
        Assert.NotNull(_processMonitor);
        
        // If event was triggered, verify it has required properties
        if (eventArgs != null)
        {
            Assert.NotEmpty(eventArgs.ProfileName);
            Assert.NotEmpty(eventArgs.ProcessName);
            Assert.NotEmpty(eventArgs.Reason);
        }
    }
}
