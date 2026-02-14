using System.IO;
using System.Text.Json;
using WiimoteManager.Models;

namespace WiimoteManager.Services;

public class ProfileService
{
    private readonly string _profilesDir;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProfileService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _profilesDir = Path.Combine(appData, "WiimoteManager", "Profiles");
        
        if (!Directory.Exists(_profilesDir))
        {
            Directory.CreateDirectory(_profilesDir);
        }
        
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        
        EnsureDefaultProfile();
        EnsureRacingProfile();
        EnsureRocketLeagueProfile();
    }

    public void EnsureDefaultProfile()
    {
        var defaultPath = Path.Combine(_profilesDir, "Default.json");
        if (!File.Exists(defaultPath))
        {
            var defaultProfile = new MappingProfile 
            { 
                Name = "Default",
                Description = "Classic NES-style horizontal Wiimote mapping",
                Tags = new List<string> { "Default", "Classic" },
                Author = "System"
            };
            SaveProfile(defaultProfile);
        }
    }

    private void EnsureRacingProfile()
    {
        var racingTemplate = new RacingGameTemplate();
        var racingPath = Path.Combine(_profilesDir, $"{racingTemplate.Name}.json");
        if (!File.Exists(racingPath))
        {
            try
            {
                SaveProfile(racingTemplate.CreateProfile());
            }
            catch (IOException)
            {
                // Another process/test may create the profile at the same time.
            }
        }
    }

    private void EnsureRocketLeagueProfile()
    {
        var rocketTemplate = new RocketLeagueTemplate();
        var rocketPath = Path.Combine(_profilesDir, $"{rocketTemplate.Name}.json");
        try
        {
            if (!File.Exists(rocketPath))
            {
                SaveProfile(rocketTemplate.CreateProfile());
            }
            else
            {
                var profile = LoadProfile(rocketTemplate.Name);
                // Always keep Rocket League profile aligned with required baseline mapping.
                var configured = rocketTemplate.CreateProfile();
                profile.Description = configured.Description;
                profile.Tags = configured.Tags;
                profile.AssociatedGames = configured.AssociatedGames;
                profile.IconEmoji = configured.IconEmoji;
                profile.UseAccelerometer = true;
                profile.AccelMapping = configured.AccelMapping;
                profile.A = configured.A;
                profile.B = configured.B;
                profile.X = configured.X;
                profile.Y = configured.Y;
                profile.LeftShoulder = configured.LeftShoulder;
                profile.RightShoulder = configured.RightShoulder;
                profile.LeftTrigger = configured.LeftTrigger;
                profile.RightTrigger = configured.RightTrigger;
                profile.Start = configured.Start;
                profile.Back = configured.Back;
                profile.Guide = configured.Guide;
                SaveProfile(profile);
            }
        }
        catch (IOException)
        {
            // Another process/test may create or write the profile at the same time.
        }
    }

    public List<string> GetProfileNames()
    {
        return Directory.GetFiles(_profilesDir, "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .OrderBy(n => n == "Default" ? "" : n) // Default first
            .ToList();
    }
    
    /// <summary>
    /// Gets profiles sorted by various criteria
    /// </summary>
    public List<MappingProfile> GetProfiles(ProfileSortOrder sortOrder = ProfileSortOrder.Name)
    {
        var profiles = GetProfileNames()
            .Select(name => LoadProfile(name))
            .Where(p => p != null)
            .ToList();
        
        return sortOrder switch
        {
            ProfileSortOrder.Name => profiles.OrderBy(p => p.Name == "Default" ? "" : p.Name).ToList(),
            ProfileSortOrder.LastUsed => profiles.OrderByDescending(p => p.LastUsedAt).ToList(),
            ProfileSortOrder.MostUsed => profiles.OrderByDescending(p => p.UsageCount).ToList(),
            ProfileSortOrder.Favorites => profiles.OrderByDescending(p => p.IsFavorite).ThenBy(p => p.Name).ToList(),
            ProfileSortOrder.CreatedDate => profiles.OrderByDescending(p => p.CreatedAt).ToList(),
            _ => profiles
        };
    }

    public MappingProfile LoadProfile(string name)
    {
        var path = Path.Combine(_profilesDir, $"{name}.json");
        if (!File.Exists(path)) 
        {
            return new MappingProfile { Name = name };
        }
        
        try 
        {
            var json = File.ReadAllText(path);
            var profile = JsonSerializer.Deserialize<MappingProfile>(json, _jsonOptions);
            
            if (profile == null)
            {
                return new MappingProfile { Name = name };
            }
            
            // Ensure name matches filename to avoid confusion
            profile.Name = name;
            
            // Migrate old profiles if needed
            if (profile.Version < MappingProfile.CurrentVersion)
            {
                profile = MigrateProfile(profile);
            }
            
            // Validate profile
            if (!profile.IsValid(out var errors))
            {
                Console.WriteLine($"[ProfileService] Profile '{name}' has validation errors: {string.Join(", ", errors)}");
                // Return it anyway, but log the issues
            }
            
            return profile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProfileService] Error loading profile '{name}': {ex.Message}");
            return new MappingProfile { Name = name };
        }
    }

    public void SaveProfile(MappingProfile profile)
    {
        // Update timestamps
        profile.ModifiedAt = DateTime.Now;
        if (profile.CreatedAt == DateTime.MinValue)
        {
            profile.CreatedAt = DateTime.Now;
        }
        
        // Ensure current version
        profile.Version = MappingProfile.CurrentVersion;
        
        // Validate before saving
        if (!profile.IsValid(out var errors))
        {
            throw new InvalidOperationException($"Cannot save invalid profile: {string.Join(", ", errors)}");
        }
        
        // Sanitize filename
        var safeName = string.Join("_", profile.Name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_profilesDir, $"{safeName}.json");
        
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(path, json);
    }
    
    public void DeleteProfile(string name)
    {
        if (name.Equals("Default", StringComparison.OrdinalIgnoreCase)) 
        {
            throw new InvalidOperationException("Cannot delete the Default profile");
        }
        
        var path = Path.Combine(_profilesDir, $"{name}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
    
    public MappingProfile CreateNewProfile(string baseName)
    {
        string name = baseName;
        int counter = 1;
        while (File.Exists(Path.Combine(_profilesDir, $"{name}.json")))
        {
            name = $"{baseName} ({counter++})";
        }
        
        var profile = new MappingProfile 
        { 
            Name = name,
            Description = $"Copy of {baseName}",
            Author = "User"
        };
        SaveProfile(profile);
        return profile;
    }
    
    /// <summary>
    /// Creates a profile from a template
    /// </summary>
    public MappingProfile CreateFromTemplate(ProfileTemplate template)
    {
        var profile = template.CreateProfile();
        
        // Ensure unique name
        string name = profile.Name;
        int counter = 1;
        while (File.Exists(Path.Combine(_profilesDir, $"{name}.json")))
        {
            name = $"{profile.Name} ({counter++})";
        }
        profile.Name = name;
        
        SaveProfile(profile);
        return profile;
    }
    
    /// <summary>
    /// Exports a profile to a file
    /// </summary>
    public void ExportProfile(MappingProfile profile, string destinationPath)
    {
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(destinationPath, json);
    }
    
    /// <summary>
    /// Imports a profile from a file
    /// </summary>
    public MappingProfile ImportProfile(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Profile file not found", sourcePath);
        }
        
        var json = File.ReadAllText(sourcePath);
        var profile = JsonSerializer.Deserialize<MappingProfile>(json, _jsonOptions);
        
        if (profile == null)
        {
            throw new InvalidOperationException("Failed to deserialize profile");
        }
        
        // Validate imported profile
        if (!profile.IsValid(out var errors))
        {
            throw new InvalidOperationException($"Imported profile is invalid: {string.Join(", ", errors)}");
        }
        
        // Ensure unique name
        string originalName = profile.Name;
        string name = originalName;
        int counter = 1;
        while (File.Exists(Path.Combine(_profilesDir, $"{name}.json")))
        {
            name = $"{originalName} (Imported {counter++})";
        }
        profile.Name = name;
        
        SaveProfile(profile);
        return profile;
    }
    
    /// <summary>
    /// Gets the directory where profiles are stored
    /// </summary>
    public string GetProfileDirectory() => _profilesDir;
    
    /// <summary>
    /// Migrates old profile versions to current version
    /// </summary>
    private MappingProfile MigrateProfile(MappingProfile profile)
    {
        // Migration from v1 to v2 (added metadata)
        if (profile.Version < 2)
        {
            profile.Description = profile.Description ?? string.Empty;
            profile.Tags = profile.Tags ?? new List<string>();
            profile.AssociatedGames = profile.AssociatedGames ?? new List<string>();
            profile.Author = "User"; // Default author for migrated profiles
            profile.IconEmoji = "🎮";
            profile.CreatedAt = File.GetCreationTime(Path.Combine(_profilesDir, $"{profile.Name}.json"));
            profile.ModifiedAt = File.GetLastWriteTime(Path.Combine(_profilesDir, $"{profile.Name}.json"));
            profile.Version = 2;
        }
        
        return profile;
    }
}

public enum ProfileSortOrder
{
    Name,
    LastUsed,
    MostUsed,
    Favorites,
    CreatedDate
}
