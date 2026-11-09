# Quick Start Guide

## Prerequisites

Before running the application, ensure you have the following installed:

1.  **[ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases)** (Required for Virtual Controller emulation)
    *   Download and install the latest release (Setup.exe).
2.  **.NET 8.0 SDK** (Required to build and run)
3.  **Bluetooth Adapter** (Bluetooth 4.0+ recommended)

> **⚠️ Note**: Use **Original Nintendo Wiimotes** (RVL-001/RVL-036). Third-party clones are known to have Bluetooth compatibility issues.

## How to Run

1.  Open a terminal in the project root.
2.  Run the following command:

    ```powershell
    dotnet run --project WiiMoteUtlity/WiimoteManager/WiimoteManager.csproj
    ```

## Troubleshooting

*   **Pairing Issues (Windows 11)**: If Windows asks for a PIN you don't have:
    1. Press `Win + R`.
    2. Run `devicepairingwizard`.
    3. Pair through this old-school wizard (Leave PIN blank).
*   **No Virtual Controller**: Run `joy.cpl` (Windows Game Controllers) to verify if "Xbox 360 Controller for Windows" appears when emulation is enabled.
*   **Input Not Working**: 
    *   Default Mapping (Horizontal):
        *   **Wiimote '2'** -> **Xbox A**
        *   **Wiimote '1'** -> **Xbox B**
        *   **Wiimote 'A'** -> **Xbox X**
        *   **Wiimote 'B'** -> **Xbox Y**
    *   Check logs at `Desktop/wiimote_debug.log`.
*   **Build Errors**: If you encounter errors, try running `dotnet clean` followed by `dotnet build`.
*   **Virtual Controller Error**: If you see "Failed to initialize ViGEmClient", ensure the ViGEmBus driver is installed.

## Hardware Diagnostic Tool

If you suspect driver or hardware issues, run the included hardware diagnostic tool:

```powershell
dotnet run --project WiiMoteUtlity/WiimoteHardwareTest/WiimoteHardwareTest.csproj
```

This tool validates:
1. ViGEmBus driver installation.
2. Wiimote connectivity via HID.
3. Input forwarding capability (logs inputs to console).
