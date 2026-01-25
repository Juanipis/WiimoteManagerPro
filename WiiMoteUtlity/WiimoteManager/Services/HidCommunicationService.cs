using HidSharp;
using System.Collections.Concurrent;
using WiimoteManager.Models;

namespace WiimoteManager.Services;

/// <summary>
/// Manages low-level HID (Human Interface Device) communication with Wiimote devices.
/// Handles report parsing, LED/rumble control, and data stream reading.
/// </summary>
public class HidCommunicationService : IDisposable
{
    private readonly Dictionary<string, HidDevice?> _deviceCache = new();
    private readonly ConcurrentDictionary<string, HidStream?> _streams = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _readCancellationTokens = new();
    private readonly ConcurrentDictionary<string, WiimoteDevice> _connectedDevices = new();
    private bool _disposed = false;

    /// <summary>
    /// Event triggered when a report is received from a Wiimote.
    /// </summary>
    public event EventHandler<(string deviceId, byte[] reportData)>? ReportReceived;

    /// <summary>
    /// Event triggered when a device connection is lost.
    /// </summary>
    public event EventHandler<string>? DeviceDisconnected;

    /// <summary>
    /// Registers a device for HID communication monitoring.
    /// </summary>
    public void RegisterDevice(WiimoteDevice device)
    {
        if (!_connectedDevices.TryAdd(device.DeviceId, device))
        {
            _connectedDevices[device.DeviceId] = device;
        }
    }

    /// <summary>
    /// Unregisters a device and stops monitoring.
    /// </summary>
    public void UnregisterDevice(string deviceId)
    {
        _connectedDevices.TryRemove(deviceId, out _);
        StopReadingAsync(deviceId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Enumerates all connected Wiimote HID devices on the system.
    /// </summary>
    public IEnumerable<HidDevice> EnumerateWiimoteDevices()
    {
        var deviceList = DeviceList.Local;
        return deviceList.GetHidDevices()
            .Where(d => d.VendorID == WiimoteReports.WiimoteVendorID && 
                       d.ProductID == WiimoteReports.WiimoteProductID)
            .ToList();
    }

    /// <summary>
    /// Opens an HID stream for the specified device.
    /// </summary>
    public bool TryOpenDevice(WiimoteDevice device, out HidStream? stream)
    {
        stream = null;

        // First, try to find the device by HID path if available
        HidDevice? hidDevice = null;

        if (!string.IsNullOrEmpty(device.HidPath))
        {
            hidDevice = EnumerateWiimoteDevices()
                .FirstOrDefault(d => d.DevicePath == device.HidPath);
        }

        // Fall back to finding by path matching Bluetooth address
        if (hidDevice == null)
        {
            hidDevice = EnumerateWiimoteDevices()
                .FirstOrDefault(d => d.DevicePath.Contains(device.BluetoothAddress.Replace(":", "")));
        }

        if (hidDevice == null)
        {
            return false;
        }

        try
        {
            device.HidPath = hidDevice.DevicePath;
            stream = hidDevice.Open();

            if (stream != null)
            {
                _streams[device.DeviceId] = stream;
                _deviceCache[device.DeviceId] = hidDevice;
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open HID device: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Sets the LED state for the Wiimote (Report 0x11).
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="ledMask">LED bitmask (combination of LED1-LED4)</param>
    /// <param name="rumbleEnable">Enable or disable rumble</param>
    public async Task<bool> SetLEDAsync(string deviceId, byte ledMask, bool rumbleEnable = false)
    {
        if (!_streams.TryGetValue(deviceId, out var stream) || stream == null)
        {
            return false;
        }

        try
        {
            // Report 0x11: Set LED & Rumble
            // Byte 0: Report ID (0x11)
            // Byte 1: Rumble bit (0x01) | LED bits (0xF0)
            byte rumbleBit = rumbleEnable ? WiimoteReports.RumbleBit : (byte)0x00;
            byte reportByte = (byte)(rumbleBit | (ledMask & 0xF0));

            byte[] report = new byte[WiimoteReports.StandardReportSize];
            report[0] = WiimoteReports.ReportSetLEDRumble;
            report[1] = reportByte;

            await stream.WriteAsync(report, 0, report.Length);

            // Update device state
            if (_connectedDevices.TryGetValue(deviceId, out var device))
            {
                device.LedState = ledMask;
                device.IsRumbling = rumbleEnable;
                device.UpdateLastCommunication();
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting LEDs: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets the data reporting mode (Report 0x12).
    /// </summary>
    public async Task<bool> SetDataReportingModeAsync(string deviceId, DataReportingMode mode, bool continuous = true)
    {
        if (!_streams.TryGetValue(deviceId, out var stream) || stream == null)
        {
            return false;
        }

        try
        {
            // Report 0x12: Set Data Reporting Mode
            // Byte 0: Report ID (0x12)
            // Byte 1: Continuous flag (0x04 for continuous, 0x00 for once)
            // Byte 2: Reporting mode
            byte continuousFlag = continuous ? (byte)0x04 : (byte)0x00;

            byte[] report = new byte[WiimoteReports.StandardReportSize];
            report[0] = WiimoteReports.ReportSetDataMode;
            report[1] = continuousFlag;
            report[2] = (byte)mode;

            await stream.WriteAsync(report, 0, report.Length);

            if (_connectedDevices.TryGetValue(deviceId, out var device))
            {
                device.UpdateLastCommunication();
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting reporting mode: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Requests status information from the Wiimote (Report 0x15).
    /// </summary>
    public async Task<bool> RequestStatusAsync(string deviceId)
    {
        if (!_streams.TryGetValue(deviceId, out var stream) || stream == null)
        {
            return false;
        }

        try
        {
            byte[] report = new byte[WiimoteReports.StandardReportSize];
            report[0] = WiimoteReports.ReportStatusRequest;

            await stream.WriteAsync(report, 0, report.Length);

            if (_connectedDevices.TryGetValue(deviceId, out var device))
            {
                device.UpdateLastCommunication();
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Starts an async loop to read HID reports from the device.
    /// </summary>
    public async Task StartReadingAsync(string deviceId, CancellationToken externalCancellationToken = default)
    {
        if (!_streams.TryGetValue(deviceId, out var stream) || stream == null || !_connectedDevices.ContainsKey(deviceId))
        {
            return;
        }

        // Create a linked cancellation token source
        var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        _readCancellationTokens[deviceId] = cts;

        try
        {
            // Set initial reporting mode and LEDs
            await SetDataReportingModeAsync(deviceId, DataReportingMode.ButtonsAccelerometerExtension, continuous: true);
            await SetLEDAsync(deviceId, WiimoteReports.LED1); // Indicate connection with LED1
            await RequestStatusAsync(deviceId);

            byte[] buffer = new byte[WiimoteReports.StandardReportSize];
            int bytesRead;

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (bytesRead > 0)
                    {
                        ReportReceived?.Invoke(this, (deviceId, (byte[])buffer.Clone()));
                        ParseReport(deviceId, buffer, bytesRead);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading from device {deviceId}: {ex.Message}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in read loop for {deviceId}: {ex.Message}");
        }
        finally
        {
            _readCancellationTokens.TryRemove(deviceId, out _);
        }
    }

    /// <summary>
    /// Stops reading from the specified device.
    /// </summary>
    public async Task StopReadingAsync(string deviceId)
    {
        if (_readCancellationTokens.TryRemove(deviceId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Parses incoming HID reports and updates device state.
    /// </summary>
    private void ParseReport(string deviceId, byte[] reportData, int length)
    {
        if (length < 1 || !_connectedDevices.TryGetValue(deviceId, out var device))
        {
            return;
        }

        byte reportId = reportData[0];

        try
        {
            switch (reportId)
            {
                case WiimoteReports.ReportStatus: // 0x20
                    ParseStatusReport(device, reportData);
                    break;

                case WiimoteReports.ReportCoreButtons: // 0x30
                    ParseCoreButtonsReport(device, reportData);
                    break;

                case WiimoteReports.ReportCoreAccel: // 0x31
                    ParseCoreAccelerometerReport(device, reportData);
                    break;

                case WiimoteReports.ReportCoreExt: // 0x32
                    ParseCoreExtensionReport(device, reportData);
                    break;

                case WiimoteReports.ReportCoreAccelExt: // 0x35
                    ParseCoreAccelerometerExtensionReport(device, reportData);
                    break;

                default:
                    // Other report types not yet implemented
                    break;
            }

            device.UpdateLastCommunication();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing report {reportId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses Report 0x20: Status Information.
    /// </summary>
    private void ParseStatusReport(WiimoteDevice device, byte[] data)
    {
        // Byte 0: Report ID (0x20)
        // Bytes 1-2: Button state
        // Byte 3: Battery level
        // Byte 4: Connection info & Extension

        if (data.Length >= 7)
        {
            ushort buttons = (ushort)((data[2] << 8) | data[1]);
            device.CurrentButtonState = (ButtonState)buttons;

            int batteryRaw = data[6];
            device.BatteryLevel = Math.Min(100, (int)((batteryRaw / 200.0f) * 100));

            // Check for extension
            byte extStatus = (byte)(data[4] & 0x02);
            if (extStatus != 0)
            {
                // Extension connected, but type detection requires further I2C reads
            }
        }
    }

    /// <summary>
    /// Parses Report 0x30: Core Buttons Only.
    /// </summary>
    private void ParseCoreButtonsReport(WiimoteDevice device, byte[] data)
    {
        // Byte 0: Report ID (0x30)
        // Bytes 1-2: Button state (little-endian)
        if (data.Length >= 3)
        {
            ushort buttons = (ushort)((data[2] << 8) | data[1]);
            device.CurrentButtonState = (ButtonState)buttons;
        }
    }

    /// <summary>
    /// Parses Report 0x31: Core Buttons + Accelerometer.
    /// </summary>
    private void ParseCoreAccelerometerReport(WiimoteDevice device, byte[] data)
    {
        // Bytes 1-2: Buttons
        ParseCoreButtonsReport(device, data);

        // Bytes 3-5: Accelerometer (8-bit resolution)
        if (data.Length >= 6)
        {
            device.AccelX = (data[3] - WiimoteReports.AccelXZero) / WiimoteReports.AccelSensitivity;
            device.AccelY = (data[4] - WiimoteReports.AccelYZero) / WiimoteReports.AccelSensitivity;
            device.AccelZ = (data[5] - WiimoteReports.AccelZZero) / WiimoteReports.AccelSensitivity;
        }
    }

    /// <summary>
    /// Parses Report 0x32: Core Buttons + Extension.
    /// </summary>
    private void ParseCoreExtensionReport(WiimoteDevice device, byte[] data)
    {
        ParseCoreButtonsReport(device, data);

        // Bytes 3-8: Extension data (6 bytes)
        if (data.Length >= 9)
        {
            byte[] extData = new byte[6];
            Array.Copy(data, 3, extData, 0, 6);
            ParseExtensionData(device, extData);
        }
    }

    /// <summary>
    /// Parses Report 0x35: Core Buttons + Accelerometer + Extension.
    /// </summary>
    private void ParseCoreAccelerometerExtensionReport(WiimoteDevice device, byte[] data)
    {
        ParseCoreAccelerometerReport(device, data);

        // Bytes 6-16: Extension data (11 bytes in this report)
        if (data.Length >= 17)
        {
            byte[] extData = new byte[11];
            Array.Copy(data, 6, extData, 0, 11);
            ParseExtensionData(device, extData);
        }
    }

    /// <summary>
    /// Parses extension controller data.
    /// </summary>
    private void ParseExtensionData(WiimoteDevice device, byte[] extData)
    {
        if (device.ExtensionType == ExtensionType.Nunchuk)
        {
            ParseNunchukData(device, extData);
        }
        else if (device.ExtensionType == ExtensionType.ClassicController)
        {
            ParseClassicControllerData(device, extData);
        }
    }

    /// <summary>
    /// Parses Nunchuk extension data.
    /// </summary>
    private void ParseNunchukData(WiimoteDevice device, byte[] extData)
    {
        if (extData.Length < 6)
            return;

        var nunchuk = device.NunchukState ?? new NunchukState();

        // Analog stick (bytes 0-1)
        nunchuk.StickX = ((extData[0] & 0xFF) - 128) / 128.0f;
        nunchuk.StickY = ((extData[1] & 0xFF) - 128) / 128.0f;

        // Buttons (byte 5, bits 0-1)
        nunchuk.ButtonState = (NunchukButtons)(extData[5] & 0x03);

        // Accelerometer would be in bytes 2-4 (if in accelerometer mode)
        // Decoding depends on which report mode is active

        device.NunchukState = nunchuk;
    }

    /// <summary>
    /// Parses Classic Controller extension data.
    /// </summary>
    private void ParseClassicControllerData(WiimoteDevice device, byte[] extData)
    {
        if (extData.Length < 6)
            return;

        var classic = device.ClassicControllerState ?? new ClassicControllerState();

        // Left analog stick (bytes 0-1)
        classic.LeftStickX = ((extData[0] & 0x3F) - 32) / 32.0f;
        classic.LeftStickY = ((extData[1] & 0x3F) - 32) / 32.0f;

        // Right analog stick (bytes 2-4, with bit packing)
        classic.RightStickX = (((extData[2] & 0xC0) >> 3) | (extData[4] & 0x0F)) / 32.0f - 1.0f;
        classic.RightStickY = ((extData[3] & 0x1F)) / 32.0f;

        // Triggers (byte 2 bits 4-5, byte 4 bits 4-7)
        classic.TriggerLeft = ((extData[2] & 0x60) >> 2) / 255.0f;
        classic.TriggerRight = (extData[3] & 0x1F) / 31.0f;

        // Buttons (bytes 4-5)
        ushort buttons = (ushort)((extData[5] << 8) | (extData[4] & 0xFF));
        classic.ButtonState = (ClassicControllerButtons)buttons;

        device.ClassicControllerState = classic;
    }

    /// <summary>
    /// Closes the HID stream for a device.
    /// </summary>
    public void CloseDevice(string deviceId)
    {
        if (_streams.TryRemove(deviceId, out var stream))
        {
            stream?.Dispose();
        }

        if (_readCancellationTokens.TryRemove(deviceId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        _deviceCache.Remove(deviceId);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var deviceId in _streams.Keys.ToList())
        {
            CloseDevice(deviceId);
        }

        _disposed = true;
    }
}
