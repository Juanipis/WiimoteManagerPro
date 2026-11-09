# WiiMote Manager Pro - Architecture Document

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                  WPF User Interface                      │
│  ┌──────────────────┐       ┌──────────────────────┐   │
│  │  MainWindow      │       │  WiimoteCard         │   │
│  │  - Dashboard     │       │  - Device Display    │   │
│  │  - Device List   │       │  - LED Controls      │   │
│  │  - Scan Button   │       │  - Button Monitor    │   │
│  └────────┬─────────┘       └──────────┬───────────┘   │
└───────────┼─────────────────────────────┼───────────────┘
            │                             │
┌───────────┼─────────────────────────────┼───────────────┐
│   MVVM Layer                            │               │
│  ┌────────▼─────────────┐       ┌──────▼──────────┐   │
│  │ MainViewModel        │       │WiimoteViewModel │   │
│  │ - Device Collection  │       │ - LED Commands  │   │
│  │ - Scan Command       │       │ - Rumble Ctrl   │   │
│  │ - Device Events      │       │ - Display Logic │   │
│  └────────┬─────────────┘       └──────┬──────────┘   │
└───────────┼─────────────────────────────┼───────────────┘
            │                             │
┌───────────┼─────────────────────────────┼───────────────┐
│   Service Layer                         │               │
│  ┌────────▼────────────┐        ┌──────▼──────────┐   │
│  │ BluetoothService    │        │HidCommunication │   │
│  │ - Discovery         │        │ - LED Control   │   │
│  │ - Pairing           │        │ - Rumble Ctrl   │   │
│  │ - Device Events     │        │ - Report Parsing│   │
│  └────────┬────────────┘        └──────┬──────────┘   │
└───────────┼─────────────────────────────┼───────────────┘
            │                             │
┌───────────┼─────────────────────────────┼───────────────┐
│   Data Models                           │               │
│  ┌────────▼────────────┐        ┌──────▼──────────┐   │
│  │ WiimoteDevice       │        │ ButtonState     │   │
│  │ - Properties        │        │ - Enum Flags    │   │
│  │ - State Mgmt        │        │ - Button Bits   │   │
│  └────────┬────────────┘        └──────┬──────────┘   │
│  ┌────────▼────────────┐        ┌──────▼──────────┐   │
│  │WiimoteReports       │        │ NunchukState    │   │
│  │ - Report Constants  │        │ - Extension Data│   │
│  │ - Memory Addresses  │        │ - Extension Ctrl│   │
│  └─────────────────────┘        └─────────────────┘   │
└──────────────────────────────────────────────────────────┘
            │                             │
            └─────────────┬───────────────┘
                          │
            ┌─────────────▼───────────────┐
            │  Windows HID & Bluetooth    │
            │  - USB HID Devices          │
            │  - Bluetooth Adapter        │
            │  - Wiimote Devices          │
            └─────────────────────────────┘
```

## Detailed Component Descriptions

### 1. User Interface Layer (WPF)

#### MainWindow.xaml
**Responsibilities**:
- Display main application window
- Host control panel (Scan, Disconnect, Clear buttons)
- Render device card collection
- Manage window lifecycle

**Key Elements**:
```xaml
<Window>
  <Grid Background="#1E1E1E"> <!-- Dark theme -->
    <StackPanel>
      <!-- Header -->
      <!-- Control Panel with Commands -->
      <!-- ItemsControl with WiimoteCard -->
      <!-- Empty State Placeholder -->
    </StackPanel>
  </Grid>
</Window>
```

**Data Binding**:
- `MainViewModel.ConnectedWiimotes` → ItemsControl
- `MainViewModel.ScanCommand` → Button.Click
- `MainViewModel.ClearCommand` → Button.Click

#### WiimoteCard.xaml
**Responsibilities**:
- Display individual Wiimote device state
- Provide LED toggle buttons
- Show real-time button presses
- Display battery level and sensors
- Enable device disconnect

**Key Elements**:
- Device Info Header (Name, MAC address)
- Battery Level Progress Bar
- Button State Indicators (highlight on press)
- LED Toggle Buttons (LED1-4)
- Rumble/Vibrate Button
- Accelerometer Display (Tilt angles)
- Disconnect Button

**Data Binding**:
- `WiimoteViewModel.Device.*` → TextBlocks, ProgressBar
- `WiimoteViewModel.IsLed[1-4]On` → Button Background
- `WiimoteViewModel.PressedButtons` → TextBlock
- `WiimoteViewModel.TiltX/Y` → TextBlocks

### 2. MVVM ViewModels Layer

#### MainViewModel : ObservableObject
**Responsibilities**:
- Manage collection of connected Wiimotes
- Orchestrate Bluetooth discovery
- Handle device lifecycle events
- Execute global commands

**Key Properties**:
```csharp
public ObservableCollection<WiimoteViewModel> ConnectedWiimotes { get; }
public bool IsScanning { get; private set; }
public RelayCommand ScanCommand { get; }
public RelayCommand ClearCommand { get; }
public RelayCommand DisconnectAllCommand { get; }
```

**Key Methods**:
- `ScanForDevices()` - Invokes BluetoothService.ScanAsync()
- `OnDeviceDiscovered()` - Event handler, creates WiimoteViewModel
- `OnDeviceDisconnected()` - Removes device from collection
- `ClearAllDevices()` - Disconnects and removes all

**Event Handlers**:
- Subscribes to `BluetoothService.DeviceDiscovered`
- Subscribes to `BluetoothService.PairingCompleted`
- Subscribes to `WiimoteViewModel.DeviceDisconnected` (per device)

#### WiimoteViewModel : ObservableObject
**Responsibilities**:
- Manage single Wiimote device state and control
- Execute LED and rumble commands
- Update UI with sensor data
- Handle device connection/disconnection

**Key Properties**:
```csharp
public WiimoteDevice Device { get; }
public HidCommunicationService HidService { get; }
public RelayCommand<byte> ToggleLEDCommand { get; }
public RelayCommand ToggleRumbleCommand { get; }
public RelayCommand ConnectCommand { get; }
public RelayCommand DisconnectCommand { get; }
```

**Computed Properties** (INotifyPropertyChanged):
```csharp
public int BatteryPercent => Device.BatteryLevel;
public string PressedButtons => DecodedButtonState();
public float TiltX => CalculateTiltAngle(Device.AccelX, Device.AccelY);
public float TiltY => CalculateTiltAngle(Device.AccelY, Device.AccelZ);
```

**Key Methods**:
- `Connect()` - Opens HID stream, starts reading async
- `Disconnect()` - Closes HID stream, clears state
- `ToggleLED(int ledNumber)` - Invokes HidService.SetLEDAsync()
- `ToggleRumble()` - Invokes HidService.SetRumbleAsync()
- `UpdateButtonDisplay()` - Decodes button bitmask
- `UpdateTiltDisplay()` - Calculates angles from accelerometer

**Event Flow**:
1. User clicks LED toggle button → `ToggleLED1Command.Execute()`
2. Command calls `HidService.SetLEDAsync()`
3. HID report sent to Wiimote
4. `Device.IsLed1On` property updated
5. WPF binding updates button appearance

### 3. Service Layer

#### BluetoothService
**Responsibilities**:
- Enumerate Bluetooth devices
- Discover Nintendo Wiimotes
- Pair with Wiimotes (no-PIN)
- Manage connection state

**Architecture Pattern**: Singleton (per application)

**Key Methods**:
```csharp
public event EventHandler<WiimoteDevice> DeviceDiscovered;
public event EventHandler<(WiimoteDevice, bool success, string? error)> PairingCompleted;
public async Task ScanAsync();
public async Task PairAsync(WiimoteDevice device);
public async Task UnpairAsync(WiimoteDevice device);
public async Task<List<WiimoteDevice>> GetSystemBluetoothDevicesAsync();
```

**Implementation Details**:
- Uses `InTheHand.Net.Bluetooth` (32feet.NET) for discovery
- Filters for "Nintendo RVL-CNT-01" devices
- Maintains registry of paired devices
- Handles authentication events with empty PIN

**Thread Safety**:
- All async operations can be called from any thread
- Event marshalling back to UI thread via Dispatcher

#### HidCommunicationService
**Responsibilities**:
- Enumerate HID devices (USB)
- Open/close device streams
- Send LED and rumble commands
- Read and parse Wiimote reports
- Calculate sensor values

**Architecture Pattern**: Per-device instance (1:1 with WiimoteViewModel)

**Key Methods**:
```csharp
public void RegisterDevice(WiimoteDevice device);
public async Task<bool> TryOpenDevice(WiimoteDevice device, out HidStream stream);
public async Task SetLEDAsync(string deviceId, byte ledMask, bool state);
public async Task SetRumbleAsync(string deviceId, bool enable);
public async Task SetDataReportingModeAsync(string deviceId, byte mode, bool continuous);
public async Task RequestStatusAsync(string deviceId);
public async Task StartReadingAsync(string deviceId, CancellationToken ct);
public void ParseReport(byte[] data, WiimoteDevice device);
```

**Report Processing Flow**:
```
HID Input Report (21 bytes)
    ↓
ParseReport(data, device)
    ├─ Check report type (0x20, 0x30, 0x31, 0x35)
    ├─ ParseCoreButtons() → device.CurrentButtonState
    ├─ ParseAccelerometer() → device.AccelX/Y/Z
    ├─ ParseExtensionData() → NunchukState
    └─ RaisePropertyChanged() → UI Update
```

**HID Report Types Supported**:
- **0x20** (Status): Battery, LED, extension status
- **0x30** (Core Buttons): Button state only
- **0x31** (Core + Accel): Buttons + accelerometer
- **0x35** (Core + Accel + Ext): Full data with extension

**LED Control Implementation** (Report 0x11):
```csharp
byte ledBits = 0x00;
if (device.IsLed1On) ledBits |= 0x10; // LED1
if (device.IsLed2On) ledBits |= 0x20; // LED2
if (device.IsLed3On) ledBits |= 0x40; // LED3
if (device.IsLed4On) ledBits |= 0x80; // LED4
if (device.IsRumbling) ledBits |= 0x01; // Rumble

byte[] report = new byte[21];
report[0] = 0xA2; // HID output
report[1] = 0x11; // Report type
report[2] = ledBits;
await stream.WriteAsync(report);
```

### 4. Data Models Layer

#### WiimoteDevice : ObservableObject
**Responsibilities**:
- Store device state (properties)
- Provide MVVM notification
- Calculate derived values (battery %)

**Properties**:
```csharp
public string BluetoothAddress { get; set; }      // "00:1A:7D:DA:71:13"
public string DeviceName { get; set; }             // "Nintendo RVL-CNT-01"
public string? Alias { get; set; }                 // User-friendly name
public bool IsConnected { get; set; }              // Connection state
public int BatteryLevel { get; set; }              // 0-200 (maps to 0-100%)
public float AccelX { get; set; }                  // -2G to +2G
public float AccelY { get; set; }
public float AccelZ { get; set; }
public ButtonState CurrentButtonState { get; set; }// 16-bit button bitmask
public bool IsLed1On { get; set; }                 // LED states
public bool IsLed2On { get; set; }
public bool IsLed3On { get; set; }
public bool IsLed4On { get; set; }
public bool IsRumbling { get; set; }               // Rumble state
public ExtensionType AttachedExtension { get; set; }
```

**Display Properties** (computed):
```csharp
public string DisplayName => Alias ?? DeviceName;
public string StatusText => IsConnected ? "Connected" : "Disconnected";
public int BatteryPercent => (int)((BatteryLevel / 200.0) * 100);
```

#### ButtonState : [Flags] enum
**Bitmask Layout** (16 bits):
```
Bit:  0        1      2      3      4      5      6      7
     DPadLeft  1    2  B     A   Minus  Home  Plus   DPadUp

Bit:  8         9       10     11     12    13     14      15
     DPadDown  DPadRight(unused) (unused) Z    C    (unused) (unused)
```

**Values**:
```csharp
None = 0x0000
DPadLeft = 0x0001
Digit1 = 0x0002
Digit2 = 0x0004
B = 0x0008
A = 0x0010
Minus = 0x0010 (typo in original; correct is 0x0040)
Home = 0x0080
Plus = 0x0100
// ... etc for 13 total buttons
```

#### WiimoteReports
**Contains**:
```csharp
// Output Reports (Wiimote ← Host)
public const byte OUTPUT_REPORT_RUMBLE = 0x11;
public const byte OUTPUT_REPORT_MODE = 0x12;
public const byte OUTPUT_REPORT_IR = 0x13;
public const byte OUTPUT_REPORT_STATUS = 0x15;

// Input Reports (Wiimote → Host)
public const byte INPUT_REPORT_STATUS = 0x20;
public const byte INPUT_REPORT_MEMORY = 0x21;
public const byte INPUT_REPORT_BUTTONS = 0x30;
public const byte INPUT_REPORT_BUTTONS_ACCEL = 0x31;

// LED Bitmasks
public const byte LED1 = 0x10;
public const byte LED2 = 0x20;
public const byte LED3 = 0x40;
public const byte LED4 = 0x80;

// Rumble bit
public const byte RUMBLE = 0x01;

// Memory addresses
public const uint CALIBRATION_ADDR = 0xA40000;
public const uint EXTENSION_ID_ADDR = 0xA400FA;

// Extension IDs
public const uint NUNCHUK_ID = 0x0000A430;
public const uint CLASSIC_ID = 0x0000A720;
```

#### NunchukState : ObservableObject
**Properties**:
```csharp
public float StickX { get; set; }          // -1.0 to +1.0
public float StickY { get; set; }
public float AccelX { get; set; }          // Gravity-adjusted
public float AccelY { get; set; }
public float AccelZ { get; set; }
public bool IsZPressed { get; set; }
public bool IsCPressed { get; set; }
```

## Data Flow Examples

### Example 1: User Toggles LED1

```
UI Thread:
  User clicks LED1 Toggle Button
         ↓
  WiimoteViewModel.ToggleLED1Command.Execute()
         ↓
  ToggleLED1() method
    └─ device.IsLed1On = !device.IsLed1On
    └─ HidService.SetLEDAsync(device.Id, 0x10, state)
                                    ↓
  [Thread Pool]
  HidCommunicationService.SetLEDAsync()
    ├─ Get current LED bits
    ├─ XOR with LED1 bit (0x10)
    ├─ Build HID Report 0x11
    ├─ stream.WriteAsync(report)
    └─ Return success/failure
                                    ↓
  UI Thread via Event Marshalling
  Device.IsLed1On notifies property change
         ↓
  WPF Data Binding updates button background color
         ↓
  Button appears toggled on UI
```

### Example 2: Wiimote Button Press

```
HID Background Thread (async read loop):
  HidStream.ReadAsync(buffer)
    ↓
  Data received: [0xA1, 0x31, ...button_bits..., ...accel...]
    ↓
  HidCommunicationService.OnDataReceived()
    ├─ ParseReport(buffer, device)
    │  ├─ Extract button bitmask from bytes 0-1
    │  ├─ device.CurrentButtonState = bitmask
    │  ├─ Extract accel from bytes 2-4
    │  ├─ device.AccelX/Y/Z = values
    │  └─ RaisePropertyChanged()
    │
    └─ [Back to Main Thread via Dispatcher]
           ↓
  WiimoteViewModel.UpdateButtonDisplay()
    └─ Decode bitmask into button names
    └─ Device.CurrentButtonState property changed
           ↓
  WPF Data Binding triggers
    └─ TextBlock.Text = "A, B, Plus"
           ↓
  UI updates with pressed buttons
```

### Example 3: Bluetooth Discovery

```
MainViewModel.ScanCommand.Execute()
       ↓
MainViewModel.ScanForDevices()
       ↓
BluetoothService.ScanAsync()
  ├─ Enumerate Windows Bluetooth devices
  ├─ Filter for "RVL-CNT-01" (Wiimote) devices
  ├─ For each discovered device:
  │   └─ Raise DeviceDiscovered event
  │      └─ (Service thread)
  │
  └─ MainViewModel.OnDeviceDiscovered() [Event handler]
       ├─ Create WiimoteDevice model
       ├─ Create WiimoteViewModel
       ├─ Add to ConnectedWiimotes collection
       └─ [Property changed event back to UI]
              ↓
         WPF ItemsControl detects collection change
              ↓
         Creates new WiimoteCard control instance
              ↓
         Data binds WiimoteViewModel to card
              ↓
         Card appears on dashboard
```

## Design Patterns Used

### 1. MVVM (Model-View-ViewModel)
- **View**: XAML (MainWindow, WiimoteCard)
- **ViewModel**: Observable objects (MainViewModel, WiimoteViewModel)
- **Model**: Data classes (WiimoteDevice, ButtonState)
- **Binding**: Two-way data binding via `INotifyPropertyChanged`

**Benefits**:
- UI logic separate from presentation
- Easy to test ViewModels
- Decoupling of layers
- Data binding reduces code-behind

### 2. Observer Pattern (Events)
- `BluetoothService.DeviceDiscovered` event
- `BluetoothService.PairingCompleted` event
- `WiimoteDevice` property change notifications
- Loose coupling between services and consumers

### 3. Singleton Pattern
- `BluetoothService` (single instance for whole app)
- Ensures only one Bluetooth scan at a time

### 4. Factory Pattern (implicit)
- `MainViewModel.OnDeviceDiscovered()` creates `WiimoteViewModel` instances
- Could be extracted to explicit factory for testing

### 5. Command Pattern
- `RelayCommand` for button clicks
- Encapsulates actions as objects
- Easy to bind from XAML

### 6. Async/Await Pattern
- All I/O operations are non-blocking
- UI thread never blocks on HID/Bluetooth operations
- Thread pool handles long-running work

## Thread Safety Considerations

### Safe Operations
```csharp
// Safe: Properties automatically marshal to UI thread
device.BatteryLevel = 75; // Causes MVVM notification

// Safe: RelayCommand checks dispatcher before invoking
button.Click → Command.Execute() → Dispatcher.Invoke()

// Safe: Async operations on thread pool
await HidService.SetLEDAsync(...); // Doesn't block UI
```

### Potential Issues & Mitigations
```csharp
// UNSAFE: Direct event invoke from background thread
service.DeviceDiscovered?.Invoke(...); // ❌ Cross-thread exception

// SAFE: Dispatch to UI thread first
Application.Current.Dispatcher.Invoke(() => {
    service.DeviceDiscovered?.Invoke(...);
}); // ✅ Correct

// UNSAFE: Collections modified from multiple threads
ConnectedWiimotes.Add(device); // ❌ If called from non-UI thread

// SAFE: ObservableCollection with Dispatcher
Application.Current.Dispatcher.Invoke(() => {
    ConnectedWiimotes.Add(device);
}); // ✅ Correct
```

## Extension Points

### Adding a New Wiimote Button
1. Add enum value to `ButtonState`
2. Add bitmask constant to `WiimoteReports`
3. Update `ParseReport()` in `HidCommunicationService`
4. Update `UpdateButtonDisplay()` in `WiimoteViewModel`
5. Update UI in `WiimoteCard.xaml` if needed

### Adding a New Sensor
1. Add property to `WiimoteDevice` model
2. Add parsing logic to `HidCommunicationService.ParseReport()`
3. Add display property to `WiimoteViewModel`
4. Bind in `WiimoteCard.xaml` view
5. Add unit test in `ModelTests.cs`

### Adding a New Command
1. Add `RelayCommand<T>` property to ViewModel
2. Implement execution method
3. Add corresponding service method if needed
4. Bind button to command in XAML
5. Test in `IntegrationTests.cs`

## Performance Optimizations

### Current
- ✅ Async/await for all I/O (non-blocking)
- ✅ ConcurrentDictionary for thread-safe collections
- ✅ Memory pooling via HID library (reusable buffers)
- ✅ Single Bluetooth scan to avoid repeated enumeration

### Future Improvements
- ⚠️ Throttle UI updates (debounce accelerometer)
- ⚠️ Object pooling for report parsing
- ⚠️ Lazy loading of extension data
- ⚠️ Parallel processing for 4+ devices
- ⚠️ Direct P/Invoke for Bluetooth (faster than managed wrapper)

## Testing Architecture

### Unit Tests (ModelTests.cs)
- **Scope**: Models, enums, calculations
- **Isolation**: No dependencies on services or UI
- **Technique**: Direct object instantiation
- **Coverage**: ButtonState, WiimoteDevice, WiimoteReports

### Integration Tests (IntegrationTests.cs)
- **Scope**: Services, ViewModels, data flow
- **Isolation**: Real services (no mocks, services work standalone)
- **Technique**: Create service instances, invoke methods
- **Coverage**: HidCommunicationService, BluetoothService, ViewModels

### UI Tests (future)
- **Scope**: XAML binding, control rendering
- **Tool**: Would require XUnit.Wpf runner or similar
- **Technique**: Instantiate Window, verify controls, simulate interaction
- **Coverage**: MainWindow, WiimoteCard layout and binding

## Known Limitations & Future Work

### Architectural Debt
1. **P/Invoke Pairing**: Scaffolded but not complete
2. **Extension I2C**: Structure ready, memory reads needed
3. **Error Handling**: Basic try/catch; needs robust Result type
4. **Logging**: No centralized logging (could add Serilog)
5. **DI Container**: No IoC; could add Microsoft.Extensions.DependencyInjection

### Performance Debt
1. **UI Refresh Rate**: Accelerometer updates might be too frequent
2. **Memory Leaks**: HidStream cleanup edge cases
3. **Bluetooth Enumeration**: Synchronous, blocks on slow adapters
4. **CPU Usage**: Continuous reading might spike CPU with 4+ devices

### Code Quality Debt
1. **Magic Numbers**: HID report bytes hardcoded; could use named constants
2. **Duplicated Logic**: Button parsing similar to Nunchuk; could extract
3. **Comments**: Complex accelerometer math needs explanation
4. **Type Safety**: Report types are bytes; could use enum

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-24  
**Architect**: Senior C# Engineer  
**Status**: Ready for Code Review
