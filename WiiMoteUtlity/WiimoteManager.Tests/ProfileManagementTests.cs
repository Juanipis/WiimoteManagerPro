using Xunit;
using WiimoteManager.Models;
using WiimoteManager.Services;

namespace WiimoteManager.Tests;

public class ProfileManagementTests : IDisposable
{
    private readonly string _testProfilesDir;
    private readonly ProfileService _profileService;
    
    public ProfileManagementTests()
    {
        // Create temp directory for test profiles
        _testProfilesDir = Path.Combine(Path.GetTempPath(), $"WiimoteTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testProfilesDir);
        
        // Use reflection to override the profiles directory
        _profileService = new ProfileService();
        var field = typeof(ProfileService).GetField("_profilesDir", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_profileService, _testProfilesDir);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testProfilesDir))
        {
            Directory.Delete(_testProfilesDir, true);
        }
    }
    
    [Fact]
    public void MappingProfile_ShouldHaveMetadata()
    {
        // Arrange & Act
        var profile = new MappingProfile
        {
            Name = "Test Profile",
            Description = "Test Description",
            Tags = new List<string> { "Test", "Racing" },
            AssociatedGames = new List<string> { "Need for Speed" },
            IconEmoji = "🏎️"
        };
        
        // Assert
        Assert.Equal("Test Profile", profile.Name);
        Assert.Equal("Test Description", profile.Description);
        Assert.Contains("Racing", profile.Tags);
        Assert.Contains("Need for Speed", profile.AssociatedGames);
        Assert.Equal("🏎️", profile.IconEmoji);
    }
    
    [Fact]
    public void MappingProfile_ShouldValidateCorrectly()
    {
        // Arrange
        var validProfile = new MappingProfile
        {
            Name = "Valid Profile",
            Version = MappingProfile.CurrentVersion
        };
        
        var invalidProfile = new MappingProfile
        {
            Name = "", // Invalid: empty name
            Version = MappingProfile.CurrentVersion
        };
        
        // Act
        bool validResult = validProfile.IsValid(out var validErrors);
        bool invalidResult = invalidProfile.IsValid(out var invalidErrors);
        
        // Assert
        Assert.True(validResult);
        Assert.Empty(validErrors);
        
        Assert.False(invalidResult);
        Assert.NotEmpty(invalidErrors);
        Assert.Contains(invalidErrors, e => e.Contains("name"));
    }
    
    [Fact]
    public void MappingProfile_WithAccelerometer_ShouldValidate()
    {
        // Arrange
        var profile = new MappingProfile
        {
            Name = "Racing Profile",
            UseAccelerometer = true,
            AccelMapping = new AccelerometerMapping
            {
                TiltSteeringEnabled = true,
                Sensitivity = 1.5f,
                DeadZone = 0.2f
            }
        };
        
        // Act
        bool isValid = profile.IsValid(out var errors);
        
        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }
    
    [Fact]
    public void MappingProfile_WithInvalidAccelerometer_ShouldFail()
    {
        // Arrange
        var profile = new MappingProfile
        {
            Name = "Invalid Racing",
            UseAccelerometer = true,
            AccelMapping = new AccelerometerMapping
            {
                Sensitivity = -1.0f, // Invalid
                DeadZone = 2.0f // Invalid (must be 0-1)
            }
        };
        
        // Act
        bool isValid = profile.IsValid(out var errors);
        
        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("sensitivity"));
        Assert.Contains(errors, e => e.Contains("dead zone"));
    }
    
    [Fact]
    public void MappingProfile_Clone_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new MappingProfile
        {
            Name = "Original",
            Description = "Original Description",
            Tags = new List<string> { "Tag1" }
        };
        
        // Act
        var clone = original.Clone();
        clone.Name = "Clone";
        clone.Tags.Add("Tag2");
        
        // Assert
        Assert.Equal("Original", original.Name);
        Assert.Single(original.Tags);
        Assert.Equal("Clone", clone.Name);
        Assert.Equal(2, clone.Tags.Count);
    }
    
    [Fact]
    public void MappingProfile_RecordUsage_ShouldUpdateStats()
    {
        // Arrange
        var profile = new MappingProfile { Name = "Test" };
        var initialUsageCount = profile.UsageCount;
        
        // Act
        profile.RecordUsage();
        
        // Assert
        Assert.Equal(initialUsageCount + 1, profile.UsageCount);
        Assert.NotEqual(DateTime.MinValue, profile.LastUsedAt);
    }
    
    [Fact]
    public void ProfileService_SaveAndLoad_ShouldPreserveMetadata()
    {
        // Arrange
        _profileService.EnsureDefaultProfile();
        var profile = new MappingProfile
        {
            Name = "TestProfile",
            Description = "Test Description",
            Tags = new List<string> { "Test", "Racing" },
            AssociatedGames = new List<string> { "Game1", "Game2" },
            IconEmoji = "🎮",
            IsFavorite = true,
            UseAccelerometer = true,
            AccelMapping = new AccelerometerMapping
            {
                TiltSteeringEnabled = true,
                Sensitivity = 1.5f,
                DeadZone = 0.2f,
                TargetControl = "LeftStickX"
            }
        };
        
        // Act
        _profileService.SaveProfile(profile);
        var loaded = _profileService.LoadProfile("TestProfile");
        
        // Assert
        Assert.Equal(profile.Name, loaded.Name);
        Assert.Equal(profile.Description, loaded.Description);
        Assert.Equal(profile.Tags.Count, loaded.Tags.Count);
        Assert.Equal(profile.AssociatedGames.Count, loaded.AssociatedGames.Count);
        Assert.Equal(profile.IconEmoji, loaded.IconEmoji);
        Assert.Equal(profile.IsFavorite, loaded.IsFavorite);
        Assert.Equal(profile.UseAccelerometer, loaded.UseAccelerometer);
        Assert.NotNull(loaded.AccelMapping);
        Assert.Equal(profile.AccelMapping.Sensitivity, loaded.AccelMapping.Sensitivity);
    }
    
    [Fact]
    public void ProfileService_GetProfiles_ShouldSortCorrectly()
    {
        // Arrange
        _profileService.EnsureDefaultProfile();
        
        var profile1 = new MappingProfile { Name = "A_Profile", UsageCount = 5 };
        var profile2 = new MappingProfile { Name = "B_Profile", UsageCount = 10, IsFavorite = true };
        var profile3 = new MappingProfile { Name = "C_Profile", UsageCount = 3 };
        
        _profileService.SaveProfile(profile1);
        _profileService.SaveProfile(profile2);
        _profileService.SaveProfile(profile3);
        
        // Act
        var byName = _profileService.GetProfiles(ProfileSortOrder.Name);
        var byMostUsed = _profileService.GetProfiles(ProfileSortOrder.MostUsed);
        var byFavorites = _profileService.GetProfiles(ProfileSortOrder.Favorites);
        
        // Assert
        Assert.Equal("Default", byName[0].Name); // Default always first
        Assert.Equal("A_Profile", byName[1].Name);
        
        Assert.Equal("B_Profile", byMostUsed[0].Name); // Most used first
        Assert.Equal("A_Profile", byMostUsed[1].Name);
        
        Assert.True(byFavorites[0].IsFavorite); // Favorites first
    }
    
    [Fact]
    public void ProfileService_ImportExport_ShouldWork()
    {
        // Arrange
        _profileService.EnsureDefaultProfile();
        var profile = new MappingProfile
        {
            Name = "ExportTest",
            Description = "Export Description",
            Tags = new List<string> { "Export", "Test" }
        };
        _profileService.SaveProfile(profile);
        
        var exportPath = Path.Combine(_testProfilesDir, "exported.json");
        
        // Act
        _profileService.ExportProfile(profile, exportPath);
        
        // Delete original
        _profileService.DeleteProfile("ExportTest");
        Assert.DoesNotContain("ExportTest", _profileService.GetProfileNames());
        
        // Import it back
        var imported = _profileService.ImportProfile(exportPath);
        
        // Assert
        Assert.Contains("ExportTest", imported.Name);
        Assert.Equal(profile.Description, imported.Description);
        Assert.Equal(profile.Tags.Count, imported.Tags.Count);
    }
    
    [Fact]
    public void ProfileService_DeleteDefault_ShouldThrow()
    {
        // Arrange
        _profileService.EnsureDefaultProfile();
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _profileService.DeleteProfile("Default"));
    }
    
    [Fact]
    public void ProfileService_CreateFromTemplate_ShouldWork()
    {
        // Arrange
        _profileService.EnsureDefaultProfile();
        var template = new RacingGameTemplate();
        
        // Act
        var profile = _profileService.CreateFromTemplate(template);
        
        // Assert
        Assert.Contains("Racing Game", profile.Name);
        Assert.True(profile.UseAccelerometer);
        Assert.NotNull(profile.AccelMapping);
        Assert.True(profile.AccelMapping.TiltSteeringEnabled);
        Assert.Contains("Racing", profile.Tags);
    }
    
    [Fact]
    public void RacingGameTemplate_ShouldHaveAccelerometer()
    {
        // Arrange & Act
        var template = new RacingGameTemplate();
        var profile = template.CreateProfile();
        
        // Assert
        Assert.True(profile.UseAccelerometer);
        Assert.NotNull(profile.AccelMapping);
        Assert.True(profile.AccelMapping.TiltSteeringEnabled);
        Assert.Equal("LeftStick (Steering + Pitch)", profile.AccelMapping.TargetControl);
        Assert.Equal("Auto (X+Y for Joystick)", profile.AccelMapping.TiltAxis);
        Assert.Contains("🏎️", profile.IconEmoji);
    }

    [Fact]
    public void RocketLeagueTemplate_ShouldUseRequestedLayout()
    {
        // Arrange & Act
        var template = new RocketLeagueTemplate();
        var profile = template.CreateProfile();

        // Assert
        Assert.True(profile.UseAccelerometer);
        Assert.Equal("LeftStick (Steering + Pitch)", profile.AccelMapping.TargetControl);
        Assert.Equal(ButtonState.Two, profile.RightTrigger.WiimoteButton); // 2 accelerate
        Assert.Equal(ButtonState.One, profile.A.WiimoteButton);            // 1 jump
        Assert.Equal(ButtonState.B, profile.B.WiimoteButton);              // B nitro
        Assert.Equal(ButtonState.A, profile.LeftTrigger.WiimoteButton);    // A brake
    }
    
    [Fact]
    public void ProfileTemplates_ShouldHaveMultipleTemplates()
    {
        // Act
        var templates = ProfileTemplates.GetAllTemplates();
        
        // Assert
        Assert.NotEmpty(templates);
        Assert.Contains(templates, t => t is RacingGameTemplate);
        Assert.Contains(templates, t => t is PlatformerTemplate);
        Assert.Contains(templates, t => t is FightingGameTemplate);
        Assert.Contains(templates, t => t is ShooterTemplate);
        Assert.Contains(templates, t => t is SportsTemplate);
        Assert.Contains(templates, t => t is RocketLeagueTemplate);
        Assert.Contains(templates, t => t is UCHProfileTemplate);
    }

    [Fact]
    public void UCHProfileTemplate_ShouldUseRequestedLayout()
    {
        var template = new UCHProfileTemplate();
        var profile = template.CreateProfile();

        Assert.Equal("UCH profile", profile.Name);
        Assert.Equal(ButtonState.Two, profile.A.WiimoteButton);          // Jump
        Assert.Equal(ButtonState.One, profile.X.WiimoteButton);          // Sprint
        Assert.Equal(ButtonState.Plus, profile.Start.WiimoteButton);     // Pause
        Assert.Equal(ButtonState.Minus, profile.Y.WiimoteButton);        // Dance
        Assert.Equal(ButtonState.A, profile.LeftShoulder.WiimoteButton); // Rotate left
        Assert.Equal(ButtonState.B, profile.RightShoulder.WiimoteButton);// Rotate right
        Assert.Equal(ButtonState.DPadRight, profile.DPadUp.WiimoteButton);
        Assert.Equal(ButtonState.DPadLeft, profile.DPadDown.WiimoteButton);
        Assert.Equal(ButtonState.DPadUp, profile.DPadLeft.WiimoteButton);
        Assert.Equal(ButtonState.DPadDown, profile.DPadRight.WiimoteButton);
    }
    
    [Fact]
    public void AllTemplates_ShouldCreateValidProfiles()
    {
        // Arrange
        var templates = ProfileTemplates.GetAllTemplates();
        
        // Act & Assert
        foreach (var template in templates)
        {
            var profile = template.CreateProfile();
            bool isValid = profile.IsValid(out var errors);
            
            Assert.True(isValid, $"Template {template.Name} created invalid profile: {string.Join(", ", errors)}");
            Assert.NotEmpty(profile.Name);
            Assert.NotEmpty(profile.Description);
            Assert.NotEmpty(profile.IconEmoji);
        }
    }
}
