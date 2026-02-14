using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using WiimoteManager.Models;
using WiimoteManager.Services;
using Microsoft.Win32;

namespace WiimoteManager.ViewModels;

/// <summary>
/// ViewModel for the dedicated Profile Manager window
/// </summary>
public partial class ProfileManagerViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private static readonly HashSet<string> ProtectedProfileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Default",
        "Racing Game (Tilt Steering)",
        "Rocket League (Tilt Pro)"
    };
    
    [ObservableProperty]
    private ObservableCollection<MappingProfile> _profiles = new();
    
    [ObservableProperty]
    private MappingProfile? _selectedProfile;
    
    [ObservableProperty]
    private ProfileSortOrder _sortOrder = ProfileSortOrder.Name;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private bool _showFavoritesOnly = false;

    [ObservableProperty]
    private bool _isSelectedProfileProtected = false;

    [ObservableProperty]
    private string _selectedMappingTarget = "A";

    [ObservableProperty]
    private ButtonState? _selectedWiimoteButton = ButtonState.None;
    
    public ObservableCollection<ProfileTemplate> AvailableTemplates { get; }
    public ObservableCollection<ButtonState> AvailableWiimoteButtons { get; }
    public ObservableCollection<string> MappableTargets { get; }
    
    public ProfileManagerViewModel(ProfileService profileService)
    {
        _profileService = profileService;
        AvailableTemplates = new ObservableCollection<ProfileTemplate>(ProfileTemplates.GetAllTemplates());
        AvailableWiimoteButtons = new ObservableCollection<ButtonState>(Enum.GetValues<ButtonState>());
        MappableTargets = new ObservableCollection<string>(new[]
        {
            "A", "B", "X", "Y", "LB", "RB", "LT", "RT", "Start", "Back", "Guide", "DPad Up", "DPad Down", "DPad Left", "DPad Right"
        });
        RefreshProfiles();
    }
    
    partial void OnSortOrderChanged(ProfileSortOrder value)
    {
        RefreshProfiles();
    }
    
    partial void OnSearchTextChanged(string value)
    {
        FilterProfiles();
    }
    
    partial void OnShowFavoritesOnlyChanged(bool value)
    {
        FilterProfiles();
    }

    partial void OnSelectedProfileChanged(MappingProfile? value)
    {
        IsSelectedProfileProtected = value != null && ProtectedProfileNames.Contains(value.Name);
        SyncSelectedMappingFromProfile();
    }

    partial void OnSelectedMappingTargetChanged(string value)
    {
        SyncSelectedMappingFromProfile();
    }

    partial void OnSelectedWiimoteButtonChanged(ButtonState? value)
    {
        if (SelectedProfile == null || IsSelectedProfileProtected) return;
        var mapping = GetTargetMapping(SelectedProfile, SelectedMappingTarget);
        if (mapping == null) return;
        mapping.WiimoteButton = value == ButtonState.None ? null : value;
    }

    private void SyncSelectedMappingFromProfile()
    {
        if (SelectedProfile == null) return;
        var mapping = GetTargetMapping(SelectedProfile, SelectedMappingTarget);
        SelectedWiimoteButton = mapping?.WiimoteButton ?? ButtonState.None;
    }

    private static ControlMapping? GetTargetMapping(MappingProfile profile, string target) => target switch
    {
        "A" => profile.A,
        "B" => profile.B,
        "X" => profile.X,
        "Y" => profile.Y,
        "LB" => profile.LeftShoulder,
        "RB" => profile.RightShoulder,
        "LT" => profile.LeftTrigger,
        "RT" => profile.RightTrigger,
        "Start" => profile.Start,
        "Back" => profile.Back,
        "Guide" => profile.Guide,
        "DPad Up" => profile.DPadUp,
        "DPad Down" => profile.DPadDown,
        "DPad Left" => profile.DPadLeft,
        "DPad Right" => profile.DPadRight,
        _ => null
    };

    [RelayCommand]
    public void SelectMappingTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return;
        SelectedMappingTarget = target;
    }
    
    [RelayCommand]
    public void RefreshProfiles()
    {
        var allProfiles = _profileService.GetProfiles(SortOrder);
        
        Profiles.Clear();
        foreach (var profile in allProfiles)
        {
            Profiles.Add(profile);
        }
        
        FilterProfiles();
    }
    
    private void FilterProfiles()
    {
        var allProfiles = _profileService.GetProfiles(SortOrder);
        
        // Apply filters
        var filtered = allProfiles.AsEnumerable();
        
        if (ShowFavoritesOnly)
        {
            filtered = filtered.Where(p => p.IsFavorite);
        }
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.ToLowerInvariant().Contains(searchLower) ||
                p.Description.ToLowerInvariant().Contains(searchLower) ||
                p.Tags.Any(t => t.ToLowerInvariant().Contains(searchLower)) ||
                p.AssociatedGames.Any(g => g.ToLowerInvariant().Contains(searchLower))
            );
        }
        
        Profiles.Clear();
        foreach (var profile in filtered)
        {
            Profiles.Add(profile);
        }
    }
    
    [RelayCommand]
    public void CreateNew()
    {
        var newProfile = _profileService.CreateNewProfile("New Profile");
        RefreshProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Name == newProfile.Name);
    }
    
    [RelayCommand]
    public void CreateFromTemplate(ProfileTemplate template)
    {
        if (template == null) return;
        
        var newProfile = _profileService.CreateFromTemplate(template);
        RefreshProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Name == newProfile.Name);
    }
    
    [RelayCommand]
    public void Duplicate()
    {
        if (SelectedProfile == null) return;
        
        var clone = SelectedProfile.Clone();
        clone.Name = $"{SelectedProfile.Name} Copy";
        _profileService.SaveProfile(clone);
        RefreshProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Name == clone.Name);
    }
    
    [RelayCommand]
    public void Delete()
    {
        if (SelectedProfile == null) return;
        
        if (IsSelectedProfileProtected)
        {
            MessageBox.Show("Default system profiles cannot be deleted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SelectedProfile.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );
        
        if (result == MessageBoxResult.Yes)
        {
            _profileService.DeleteProfile(SelectedProfile.Name);
            RefreshProfiles();
            SelectedProfile = Profiles.FirstOrDefault();
        }
    }
    
    [RelayCommand]
    public void Save()
    {
        if (SelectedProfile == null) return;
        if (IsSelectedProfileProtected)
        {
            MessageBox.Show("Default system profiles are read-only. Duplicate one to customize it.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        try
        {
            _profileService.SaveProfile(SelectedProfile);
            MessageBox.Show("Profile saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    [RelayCommand]
    public void ToggleFavorite()
    {
        if (SelectedProfile == null) return;
        if (IsSelectedProfileProtected) return;
        
        SelectedProfile.IsFavorite = !SelectedProfile.IsFavorite;
        _profileService.SaveProfile(SelectedProfile);
        RefreshProfiles();
    }
    
    [RelayCommand]
    public void Export()
    {
        if (SelectedProfile == null) return;
        
        var dialog = new SaveFileDialog
        {
            Title = "Export Profile",
            FileName = $"{SelectedProfile.Name}.json",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = "json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _profileService.ExportProfile(SelectedProfile, dialog.FileName);
                MessageBox.Show($"Profile exported to:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    [RelayCommand]
    public void Import()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Profile",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Multiselect = false
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var imported = _profileService.ImportProfile(dialog.FileName);
                RefreshProfiles();
                SelectedProfile = Profiles.FirstOrDefault(p => p.Name == imported.Name);
                MessageBox.Show($"Profile '{imported.Name}' imported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    [RelayCommand]
    public void OpenProfileFolder()
    {
        var folder = _profileService.GetProfileDirectory();
        if (System.IO.Directory.Exists(folder))
        {
            System.Diagnostics.Process.Start("explorer.exe", folder);
        }
    }
}
