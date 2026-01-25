using HidSharp;
using WiimoteManager.Models;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace WiimoteManager.Services;

/// <summary>
/// FINAL SOLUTION: Windows Wiimote Service using HidStream.Write()
/// Based on research from Dolphin Emulator and Julian Löhr
/// 
/// KEY INSIGHTS:
/// - Windows 8+: HidStream.Write() works perfectly for Wiimotes
/// - Windows 7: Has bug in HID Class Driver, WriteFile doesn't work for "-TR" Wiimotes
/// - HidD_SetOutputReport() causes Wiimotes to turn off (don't use!)
/// - Buffer MUST be exactly 22 bytes for Bluetooth Wiimotes
/// 
/// NO DRIVERS REQUIRED - works on Windows 8+ out-of-the-box with HidSharp
/// </summary>
public class WiimoteService : IDisposable
{
    private const int NINTENDO_VENDOR_ID = 0x057E;
    private const int WIIMOTE_PRODUCT_ID = 0x0306;
    private const int WIIMOTE_PLUS_PRODUCT_ID = 0x0330;
    
    // NOT NEEDED - Using HidStream.Write() directly works on Windows 8+
    
    private readonly Dictionary<string, WiimoteConnection> _connections = new();
    private bool _disposed = false;
    private StreamWriter? _logWriter;

    public event EventHandler<string>? ProgressUpdate;
    public event EventHandler<WiimoteDevice>? WiimoteConnected;
    public event EventHandler<WiimoteDevice>? WiimoteDisconnected;

    public WiimoteService()
    {
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "wiimote_debug.log");
            _logWriter = new StreamWriter(logPath, false) { AutoFlush = true };
            _logWriter.WriteLine($"=== WIIMOTE DEBUG LOG STARTED {DateTime.Now} ===");
        }
        catch { }
    }

    private class WiimoteConnection
    {
        public HidDevice Device { get; set; } = null!;
        public HidStream Stream { get; set; } = null!;
        public WiimoteDevice Model { get; set; } = null!;
        public Task? ReadTask { get; set; }
        public CancellationTokenSource CancellationToken { get; set; } = new();
    }

    /// <summary>
    /// Discovers and connects to all Wiimotes
    /// </summary>
    public async Task<List<WiimoteDevice>> StartDiscoveryAsync()
    {
        return await Task.Run(() =>
        {
            var devices = new List<WiimoteDevice>();
            
            try
            {
                ProgressUpdate?.Invoke(this, "Searching for Wiimotes via HID...");
                ProgressUpdate?.Invoke(this, $"[DEBUG] Looking for VID=0x{NINTENDO_VENDOR_ID:X4}, PID=0x{WIIMOTE_PRODUCT_ID:X4} or 0x{WIIMOTE_PLUS_PRODUCT_ID:X4}");

                var deviceList = DeviceList.Local.GetHidDevices(NINTENDO_VENDOR_ID).Where(d =>
                    d.ProductID == WIIMOTE_PRODUCT_ID || d.ProductID == WIIMOTE_PLUS_PRODUCT_ID
                ).ToList();

                var allDevices = DeviceList.Local.GetHidDevices().ToList();
                ProgressUpdate?.Invoke(this, $"[DEBUG] Found {allDevices.Count} total HID devices");
                ProgressUpdate?.Invoke(this, $"Found {deviceList.Count} Wiimote HID device(s)");

                for (int i = 0; i < deviceList.Count; i++)
                {
                    var hidDevice = deviceList[i];
                    string deviceKey = $"wiimote_{i}";

                    ProgressUpdate?.Invoke(this, $"[INFO] Connecting to Wiimote {i + 1}...");

                    try
                    {
                        HidStream? stream = hidDevice.Open();
                        if (stream != null)
                        {
                            ProgressUpdate?.Invoke(this, "[SUCCESS] Opened HID stream");

                            var model = new WiimoteDevice
                            {
                                DeviceId = deviceKey,
                                DeviceName = hidDevice.GetProductName() ?? "Nintendo RVL-CNT-01",
                                HidPath = hidDevice.DevicePath ?? "",
                                BluetoothAddress = "",
                                IsPaired = true,
                                IsConnected = true
                            };

                            var connection = new WiimoteConnection
                            {
                                Device = hidDevice,
                                Stream = stream,
                                Model = model
                            };

                            _connections[deviceKey] = connection;

                            // Start reading
                            connection.ReadTask = Task.Run(() => ReadLoop(deviceKey), connection.CancellationToken.Token);

                            // Configure wiimote to send accelerometer data
                            RequestAccelerometerData(deviceKey);

                            // Turn on LED1 to show connection
                            SetLED(deviceKey, 0x10);

                            devices.Add(model);
                            ProgressUpdate?.Invoke(this, $"✓ Wiimote {i + 1} connected!");
                            WiimoteConnected?.Invoke(this, model);
                        }
                    }
                    catch (Exception ex)
                    {
                        ProgressUpdate?.Invoke(this, $"[ERROR] Failed to connect to Wiimote {i + 1}: {ex.Message}");
                    }
                }

                ProgressUpdate?.Invoke(this, $"Complete. {devices.Count} connected.");
            }
            catch (Exception ex)
            {
                ProgressUpdate?.Invoke(this, $"[ERROR] Discovery failed: {ex.Message}");
            }

            return devices;
        });
    }

    /// <summary>
    /// Sends output report using HidStream.Write() - Works on Windows 8+
    /// Report buffer must be 22 bytes for Bluetooth Wiimote
    /// </summary>
    private bool SendOutputReport(string deviceKey, byte[] reportData)
    {
        if (!_connections.TryGetValue(deviceKey, out var connection))
            return false;

        try
        {
            // CRITICAL: Wiimote over Bluetooth expects EXACTLY 22 bytes
            // Report ID is at index 0, followed by data
            if (reportData.Length != 22)
            {
                ProgressUpdate?.Invoke(this, $"[ERROR] Invalid report size: {reportData.Length}, expected 22");
                return false;
            }

            // Use HidStream.Write() - this works on Windows 8+ according to research
            connection.Stream.Write(reportData);
            return true;
        }
        catch (Exception ex)
        {
            ProgressUpdate?.Invoke(this, $"[ERROR] SendOutputReport failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets LED state on Wiimote
    /// </summary>
    public bool SetLED(string deviceKey, int ledMask)
    {
        byte[] report = new byte[22];
        report[0] = 0x11; // LED/Rumble report ID
        report[1] = (byte)((ledMask & 0xF0) | (_connections.ContainsKey(deviceKey) && _connections[deviceKey].Model.IsRumbling ? 0x01 : 0x00));

        bool success = SendOutputReport(deviceKey, report);
        if (!success)
        {
            ProgressUpdate?.Invoke(this, $"[ERROR] SetLED failed");
        }

        return success;
    }

    /// <summary>
    /// Sets rumble state on Wiimote
    /// </summary>
    public bool SetRumble(string deviceKey, bool enabled)
    {
        if (!_connections.TryGetValue(deviceKey, out var connection))
            return false;

        connection.Model.IsRumbling = enabled;

        // Preserve LED state
        byte ledState = connection.Model.LedState;
        
        byte[] report = new byte[22];
        report[0] = 0x11;
        report[1] = (byte)(ledState | (enabled ? 0x01 : 0x00));

        bool success = SendOutputReport(deviceKey, report);
        if (!success)
        {
            ProgressUpdate?.Invoke(this, $"[ERROR] SetRumble failed");
        }

        return success;
    }

    /// <summary>
    /// Requests continuous accelerometer data (Report 0x31)
    /// </summary>
    public bool RequestAccelerometerData(string deviceKey)
    {
        byte[] report = new byte[22];
        report[0] = 0x12; // Data reporting mode
        report[1] = 0x00; // No rumble
        report[2] = 0x31; // Report 0x31 - Buttons + Accelerometer

        bool success = SendOutputReport(deviceKey, report);
        if (!success)
        {
            ProgressUpdate?.Invoke(this, $"[WARN] Request data failed");
        }

        return success;
    }

    /// <summary>
    /// Reads data from Wiimote continuously
    /// </summary>
    private void ReadLoop(string deviceKey)
    {
        if (!_connections.TryGetValue(deviceKey, out var connection))
            return;

        ProgressUpdate?.Invoke(this, "[DEBUG] Starting read loop (buffer size: 22)");

        byte[] buffer = new byte[22];

        try
        {
            while (!connection.CancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = connection.Stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        ProcessInputReport(deviceKey, buffer, bytesRead);
                    }
                }
                catch (TimeoutException)
                {
                    // Normal - just keep reading
                }
                catch (Exception ex)
                {
                    ProgressUpdate?.Invoke(this, $"[ERROR] Read loop ended: {ex.Message}");
                    break;
                }
            }
        }
        finally
        {
            ProgressUpdate?.Invoke(this, $"Wiimote disconnected: {connection.Model.DeviceName}");
            connection.Model.IsConnected = false;
            WiimoteDisconnected?.Invoke(this, connection.Model);
        }
    }

    /// <summary>
    /// Processes incoming HID report from Wiimote
    /// </summary>
    private void ProcessInputReport(string deviceKey, byte[] data, int length)
    {
        if (!_connections.TryGetValue(deviceKey, out var connection))
            return;

        if (length < 2)
            return;

        byte reportId = data[0];
        
        // Log to file only (not UI)
        try
        {
            _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] RAW: Len={length} ID=0x{reportId:X2} Bytes={BitConverter.ToString(data, 0, Math.Min(length, 10))}");
        }
        catch { }
        
        // Parse button data - present in reports 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37
        if (length >= 4 && reportId >= 0x30 && reportId <= 0x3F)
        {
            // Button data is in bytes 1-2, BIG-ENDIAN (byte 1 = high, byte 2 = low)
            // NOTE: In report 0x31, accelerometer LSBs are embedded:
            //   - Byte 1 bits 5-7: Must be cleared (only bits 0-4 are buttons)
            //   - Byte 2 bits 5-7: Must be cleared (only bits 0-4 are buttons)
            // Total 10 bits for buttons: bits 0-4 from each byte
            
            ushort byte1 = data[1];
            ushort byte2 = data[2];
            
            // For report 0x31, mask out accelerometer bits from BOTH button bytes
            if (reportId == 0x31)
            {
                byte1 = (ushort)(byte1 & 0x1F); // Clear bits 5-7 (keep only 0-4)
                byte2 = (ushort)(byte2 & 0x1F); // Clear bits 5-7 (keep only 0-4)
            }
            
            ushort buttons = (ushort)((byte1 << 8) | byte2);
            
            // Only log when buttons change
            if (buttons != (ushort)connection.Model.CurrentButtonState)
            {
                connection.Model.CurrentButtonState = (ButtonState)buttons;
                ProgressUpdate?.Invoke(this, $"[INPUT] Buttons: 0x{buttons:X4}");
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BUTTONS: 0x{buttons:X4}");
            }
        }

        // Parse accelerometer data (Report 0x31)
        if (reportId == 0x31 && length >= 6)
        {
            try
            {
                // Accelerometer data: 10 bits per axis
                // Upper 8 bits in bytes 3-5, lower 2 bits embedded in button bytes:
                //   - X LSBs: byte 2 bits 6-7  
                //   - Y LSBs: byte 1 bits 6-7
                //   - Z LSBs: byte 2 bits 5 and byte 1 bit 5
                
                byte accelXHigh = data[3];
                byte accelYHigh = data[4];
                byte accelZHigh = data[5];
                
                // Extract LSBs from button bytes (before masking removed them)
                byte accelXLow = (byte)((data[2] >> 6) & 0x03);  // Bits 6-7 of byte 2
                byte accelYLow = (byte)((data[1] >> 6) & 0x03);  // Bits 6-7 of byte 1
                byte accelZLow = (byte)(((data[2] >> 5) & 0x01) | (((data[1] >> 5) & 0x01) << 1)); // Bit 5 of bytes 1-2
                
                // Combine to get 10-bit values
                int accelX10bit = (accelXHigh << 2) | accelXLow;
                int accelY10bit = (accelYHigh << 2) | accelYLow;
                int accelZ10bit = (accelZHigh << 2) | accelZLow;

                // Convert to normalized values (-1.0 to 1.0, with neutral ~512 for 10-bit)
                connection.Model.AccelX = (accelX10bit - 512) / 512.0f;
                connection.Model.AccelY = (accelY10bit - 512) / 512.0f;
                connection.Model.AccelZ = (accelZ10bit - 512) / 512.0f;

                // Battery level is in byte 6 (full byte, 0-255 scale)
                if (length >= 7)
                {
                    connection.Model.BatteryLevel = (data[6] * 100) / 255;
                }

                // Update connection indicators
                connection.Model.SignalStrength = 100;
                connection.Model.UpdateLastCommunication();
                
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ACCEL: X={connection.Model.AccelX:F2} Y={connection.Model.AccelY:F2} Z={connection.Model.AccelZ:F2} Bat={connection.Model.BatteryLevel}%");
                
                // Show in UI occasionally
                if (Random.Shared.Next(50) == 0)
                {
                    ProgressUpdate?.Invoke(this, $"[ACCEL] X:{connection.Model.AccelX:F2} Y:{connection.Model.AccelY:F2} Z:{connection.Model.AccelZ:F2}");
                }
            }
            catch (Exception ex)
            {
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ERROR: Accel parsing - {ex.Message}");
                ProgressUpdate?.Invoke(this, $"[ERROR] Accel parsing failed: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var connection in _connections.Values)
        {
            connection.CancellationToken.Cancel();
            connection.Stream?.Dispose();
        }

        _connections.Clear();
        
        _logWriter?.WriteLine($"=== LOG ENDED {DateTime.Now} ===");
        _logWriter?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
