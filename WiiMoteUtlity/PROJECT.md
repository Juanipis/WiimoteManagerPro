Project Goal: Wiimote Manager Pro for Windows 11

Act as a Senior .NET Engineer specializing in HID devices and Bluetooth stacks. Your task is to generate a C# .NET 8 WPF application that allows users to discover, pair, and manage original Nintendo Wiimotes (RVL-001 and RVL-CNT-01-TR) without relying on Windows built-in Bluetooth prompts.

Project Structure

Follow this architecture strictly:

/Models: WiimoteDevice.cs, WiimoteReports.cs (Constants), ButtonState.cs (Flags).

/Services: BluetoothService.cs (Scanning/Pairing via 32feet.NET), HidCommunication.cs (Data I/O via HidSharp).

/ViewModels: MainViewModel.cs (Collection handling), WiimoteViewModel.cs (Per-device logic).

/Views: MainWindow.xaml (Modern UI Dashboard).

Technical Requirements & Constraints

1. Bluetooth Discovery & Pairing (The "No-PIN" Bypass)

Use InTheHand.Net.Bluetooth (32feet.NET).

Implement a scanner that filters for devices named "Nintendo RVL-CNT-01".

Critical Logic: Implement a Pairing Handler that responds to authentication requests with a null/empty PIN. This is the only way to pair Wiimotes on Windows 11 programmatically without manual user intervention in the Control Panel.

Reference the logic used in "WiiPair" or "WiimotePair" tools.

2. HID Communication

Use HidSharp for low-level report handling.

Once paired, identify the device's HID path.

Output Report (0x11): Implement a method to set Player LEDs and Rumble.

Byte structure: [0] = 0x11, [1] = (LEDs | Rumble).

LED Bits: LED1=0x10, LED2=0x20, LED3=0x40, LED4=0x80.

Reporting Mode (0x12): Send this report to configure the data stream (e.g., Button data + Accelerometer).

Input Reports: Implement an asynchronous loop to read bytes from the HID stream. Decode the 16-bit button state and 8-bit/10-bit accelerometer data.

3. Modern UI (WPF)

Use a clean, dark-themed dashboard.

Display connected Wiimotes in a Card layout.

Include:

Real-time battery level percentage (Report 0x15).

Visual indicators for which buttons are being pressed.

Toggle buttons for LEDs (1-4) and a "Vibrate" button.

A "Scan & Sync" button that triggers the BluetoothService.

Reference Implementations & Logic

Data Mapping: Base the bitmask decoding on the WiimoteLib (Brian Peek) documentation.

Connectivity: Replicate the handshake process found in the Dolphin Emulator source code for Wiimote Real-HID.

Code Generation Instructions

Start by generating the Models (Constants and Enums for Reports and Buttons).

Generate the BluetoothService ensuring the PairRequest(address, null) logic is present.

Generate the HidCommunication service with a thread-safe reading loop.

Finally, generate the WPF View and ViewModel using CommunityToolkit.Mvvm (if available) or standard INotifyPropertyChanged.

Ensure all code is well-commented in English for the UI elements and logic explanations, but keep variable names and technical documentation in English.