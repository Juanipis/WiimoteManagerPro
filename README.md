# UCHWiiRemoteMod (Wiimote Manager)

A Wii Remote management tool aimed at providing Virtual Controller support (Xbox 360 emulation) for games.

## Prerequisites

Before running the application, ensure you have the following installed:

1.  **[ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases)** (Required for Virtual Controller emulation)
    *   Download and install the latest release (Setup.exe).
2.  **.NET 8.0 SDK** (Required to build and run)
3.  **Bluetooth Adapter** (Bluetooth 4.0+ recommended)

## How to Run

1.  Open a terminal in the project root.
2.  Run the following command:

    ```powershell
    dotnet run --project WiiMoteUtlity/WiimoteManager/WiimoteManager.csproj
    ```

## Project Structure

*   `WiiMoteUtlity/WiimoteManager`: Main WPF Application
*   `WiiMoteUtlity/WiimoteManager.Tests`: Unit and Integration Tests

## Troubleshooting
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

## Diagnostic Tool

If you suspect driver or hardware issues, run the included hardware diagnostic tool:

```powershell
dotnet run --project WiiMoteUtlity/WiimoteHardwareTest/WiimoteHardwareTest.csproj
```

This tool validates:
1. ViGEmBus driver installation.
2. Wiimote connectivity via HID.
3. Input forwarding capability (logs inputs to console).


For more detailed documentation, see [WiiMoteUtlity/README.md](WiiMoteUtlity/README.md).
