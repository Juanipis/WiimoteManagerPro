using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WiimoteManager.Models;
using WiimoteManager.Services;

namespace WiimoteManager.ViewModels;

public partial class MappingViewModel : ObservableObject
{
    private readonly MappingProfile _profile;
    private readonly Action _saveCallback;
    private readonly WiimoteDevice _device;

    [ObservableProperty]
    private ControlMapping? _selectedMapping;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isListening;

    public string StatusText => IsListening 
        ? $"Press a button on the Wiimote to map to {SelectedMapping?.TargetName}..." 
        : "Select a button above, then click 'Map' to assign.";

    public ObservableCollection<ControlMapping> Mappings { get; }

    public MappingViewModel(MappingProfile profile, WiimoteDevice device, Action saveCallback)
    {
        _profile = profile;
        _device = device;
        _saveCallback = saveCallback;
        Mappings = new ObservableCollection<ControlMapping>(profile.AllMappings);
        
        _device.PropertyChanged += OnDevicePropertyChanged;
    }

    private void OnDevicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (IsListening && e.PropertyName == nameof(WiimoteDevice.CurrentButtonState))
        {
            // IMPORTANT: This event fires from the background read loop!
            // We must marshal to UI thread to update ObservableProperties/Collections
            
            var pressed = _device.CurrentButtonState;
            if (pressed != ButtonState.None && SelectedMapping != null)
            {
                 System.Windows.Application.Current?.Dispatcher.Invoke(() => 
                 {
                    // Double check in case it changed while marshaling
                    if (!IsListening || SelectedMapping == null) return;
                    
                    // Assign the pressed button to the selected mapping
                    SelectedMapping.WiimoteButton = pressed;
                    
                    // Refresh the list item to ensure UI updates
                    // (ObservableObject usually handles property updates, but replacing in collection forces list refresh)
                    var index = Mappings.IndexOf(SelectedMapping);
                    if (index >= 0)
                    {
                       // Hack: Trigger a collection change notification or just rely on INPC of the item
                       // Replacing the item works for listviews that don't track item properties
                       // Mappings[index] = SelectedMapping; 
                    }

                    IsListening = false;
                    _saveCallback?.Invoke();
                 });
            }
        }
    }

    [RelayCommand]
    public void SelectMapping(ControlMapping mapping)
    {
        SelectedMapping = mapping;
        IsListening = false; // Cancel any previous listen
    }

    [RelayCommand]
    public void StartListening()
    {
        if (SelectedMapping == null) return;
        IsListening = true;
    }

    [RelayCommand]
    public void ClearMapping()
    {
        if (SelectedMapping != null)
        {
            SelectedMapping.WiimoteButton = null;
             var index = Mappings.IndexOf(SelectedMapping);
             if (index >= 0) Mappings[index] = SelectedMapping;
             _saveCallback?.Invoke();
        }
    }
}
