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
    private DiagnosticLogger? _diagnosticLogger;

    public event EventHandler<string>? ProgressUpdate;
    public event EventHandler<WiimoteDevice>? WiimoteConnected;
    public event EventHandler<WiimoteDevice>? WiimoteDisconnected;

    public DiagnosticLogger? DiagnosticLogger => _diagnosticLogger;

    public WiimoteService()
    {
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "wiimote_debug.log");
            _logWriter = new StreamWriter(logPath, false) { AutoFlush = true };
            _logWriter.WriteLine($"=== WIIMOTE DEBUG LOG STARTED {DateTime.Now} ===");
            
            // Initialize diagnostic logger
            _diagnosticLogger = new DiagnosticLogger();
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
        public System.Threading.Timer? StatusTimer { get; set; }
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
                    string deviceKey = hidDevice.DevicePath ?? $"wiimote_{i}";

                    // Skip if already connected
                    if (_connections.ContainsKey(deviceKey)) continue;

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

                            // USE REPORT 0x30 (Buttons Only) - More stable for -TR Wiimotes than 0x31
                            RequestDataReport(deviceKey);

                            // Request initial status (battery + Home button)
                            RequestStatus(deviceKey);

                            // Setup periodic status requests (every 200ms for Home button + battery)
                            // This hybrid approach allows us to use Report 0x31 for fast Accel/Buttons
                            // while getting the Home button (which is broken in 0x31) via polling.
                            connection.StatusTimer = new System.Threading.Timer(
                                _ => RequestStatus(deviceKey),
                                null,
                                TimeSpan.FromMilliseconds(200),
                                TimeSpan.FromMilliseconds(200)
                            );

                            // Turn on appropriate LED based on index (1=LED1, 2=LED2, etc)
                            int ledMask = 0x10 << (devices.Count); // 0x10, 0x20, 0x40, 0x80
                            SetLED(deviceKey, ledMask);

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
    /// Requests data reporting (Report 0x31 - Buttons + Accel)
    /// </summary>
    public bool RequestDataReport(string deviceKey)
    {
        byte[] report = new byte[22];
        report[0] = 0x12; // Data reporting mode
        report[1] = 0x00; // Continuous
        report[2] = 0x31; // Report 0x31 - Buttons + Accelerometer
        
        bool success = SendOutputReport(deviceKey, report);
        if (!success)
        {
            ProgressUpdate?.Invoke(this, $"[WARN] Request data failed");
        }

        return success;
    }

    /// <summary>
    /// Requests Status Report (Report 0x20) which includes battery level and Home button
    /// </summary>
    public bool RequestStatus(string deviceKey)
    {
        byte[] report = new byte[22];
        report[0] = 0x15; // Status Request
        report[1] = 0x00; // No rumble

        bool success = SendOutputReport(deviceKey, report);
        if (success)
        {
            // Reduce log spam for high-frequency polling
            // _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] STATUS REQUEST sent");
        }
        else
        {
            ProgressUpdate?.Invoke(this, $"[WARN] Request status failed");
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
        
        // Log to file and diagnostic logger
        try
        {
            _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] RAW: Len={length} ID=0x{reportId:X2} Bytes={BitConverter.ToString(data, 0, Math.Min(length, 10))}");
            _diagnosticLogger?.LogRawPacket(reportId, data, length);
        }
        catch { }
        
        // Parse Status Report (0x20) - includes battery, Home button, and extension status
        if (reportId == 0x20 && length >= 7)
        {
            // Parse button data (bytes 1-2) - NO accelerometer LSBs here!
            ushort byte1 = data[1];
            ushort byte2 = data[2];
            ushort buttons = (ushort)((byte1 << 8) | byte2);
            
            // Extract Home button (bit 7 of byte 2 -> 0x0080)
            bool homePressed = (buttons & 0x0080) != 0;
            
            // Parse flags byte (byte 3)
            byte flags = data[3];
            bool batteryLow = (flags & 0x01) != 0;
            bool extensionConnected = (flags & 0x02) != 0;
            bool speakerEnabled = (flags & 0x04) != 0;
            bool irEnabled = (flags & 0x08) != 0;
            byte ledState = (byte)((flags >> 4) & 0x0F);
            
            // Parse battery level (byte 6)
            byte rawBattery = data[6];
            int batteryPercent = (rawBattery * 100) / 255;
            
            // Update model
            connection.Model.BatteryLevel = batteryPercent;
            connection.Model.LedState = ledState;
            
            // Log status information
            _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] STATUS: Battery={batteryPercent}% (raw=0x{rawBattery:X2}), LEDs=0x{ledState:X}, Ext={extensionConnected}, Home={homePressed}");
            _diagnosticLogger?.LogBatteryReading(rawBattery, batteryPercent);
            
            // MERGE Home button state with current buttons
            var currentButtons = connection.Model.CurrentButtonState;
            
            if (homePressed)
            {
                // Set Home bit
                connection.Model.CurrentButtonState = currentButtons | ButtonState.Home;
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] HOME BUTTON DETECTED IN STATUS REPORT!");
            }
            else
            {
                // Clear Home bit (so it releases when button is released)
                connection.Model.CurrentButtonState = currentButtons & ~ButtonState.Home;
            }
            
            return; // Status report processed
        }
        
        // Parse button data - present in reports 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37
        if (length >= 3 && reportId >= 0x30 && reportId <= 0x3F)
        {
            // Button data is in bytes 1-2, BIG-ENDIAN (byte 1 = high, byte 2 = low)
            // NOTE: In report 0x31, accelerometer LSBs are embedded:
            //   - Byte 1 bits 5-7: Must be cleared (only bits 0-4 are buttons)
            //   - Byte 2 bits 5-7: Must be cleared (only bits 0-4 are buttons)
            // Total 10 bits for buttons: bits 0-4 from each byte
            //
            // SPECIAL CASE: Home button is at bit 7 of byte 1 (0x80 in byte, 0x8000 in word)
            // This bit is OUTSIDE the 0x1F mask, so we must check it separately BEFORE masking
            
            ushort byte1Raw = data[1];
            ushort byte2Raw = data[2];
            
            ushort byte1 = byte1Raw;
            ushort byte2 = byte2Raw;
            
            // For report 0x31, mask out accelerometer bits from BOTH button bytes
            if (reportId == 0x31)
            {
                byte1 = (ushort)(byte1 & 0x1F); // Clear bits 5-7 (keep only 0-4)
                byte2 = (ushort)(byte2 & 0x1F); // Clear bits 5-7 (keep only 0-4)
            }
            // For Report 0x30, NO MASKING is needed as it is purely buttons!
            
            ushort buttons = (ushort)((byte1 << 8) | byte2);
            
            
                // Only log when buttons change
            if (buttons != (ushort)(connection.Model.CurrentButtonState & ~ButtonState.Home))
            {
                // Preserve Home button state (as it comes from Report 0x20)
                var currentHome = connection.Model.CurrentButtonState & ButtonState.Home;
                connection.Model.CurrentButtonState = (ButtonState)buttons | currentHome;
                
            // Ensure progress update is marshalled to UI thread if someone is listening
            // Note: ProgressUpdate is usually bound to an ObservableCollection/String in ViewModel
            // which throws if updated from background thread.
            // However, WiimoteService is a "Service" and shouldn't know about UI Dispatcher.
            // The ViewModel should marshal it. BUT, the standard EventHandler invocation happens on THIS thread.
            
            // Temporary fix: Do NOT invoke ProgressUpdate from high-frequency loops.
            // ProgressUpdate?.Invoke(this, $"[INPUT] Buttons: 0x{buttons:X4}");
            _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BUTTONS: 0x{buttons:X4}");
                
                // If in diagnostic test mode, log the press
                if (_diagnosticLogger != null && buttons != 0)
                {
                    _diagnosticLogger.LogButtonPress("Unknown", buttons, (ButtonState)buttons);
                }
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
                float accelXNorm = (accelX10bit - 512) / 512.0f;
                float accelYNorm = (accelY10bit - 512) / 512.0f;
                float accelZNorm = (accelZ10bit - 512) / 512.0f;
                
                connection.Model.AccelX = accelXNorm;
                connection.Model.AccelY = accelYNorm;
                connection.Model.AccelZ = accelZNorm;
                
                _diagnosticLogger?.LogAccelerometer(accelX10bit, accelY10bit, accelZ10bit, accelXNorm, accelYNorm, accelZNorm);

                // NOTE: Report 0x31 does NOT include battery data!
                // Battery is only available in Report 0x20 (Status)
                // We request status periodically via timer

                // Update connection indicators
                connection.Model.SignalStrength = 100;
                connection.Model.UpdateLastCommunication();
                
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ACCEL: X={connection.Model.AccelX:F2} Y={connection.Model.AccelY:F2} Z={connection.Model.AccelZ:F2}");
                
                // Show in UI occasionally
                if (Random.Shared.Next(50) == 0)
                {
                    // DO NOT invoke ProgressUpdate from here - it's a background thread and causes cross-thread exceptions in the ViewModel's ObservableCollection
                    // ProgressUpdate?.Invoke(this, $"[ACCEL] X:{connection.Model.AccelX:F2} Y:{connection.Model.AccelY:F2} Z:{connection.Model.AccelZ:F2}");
                }
            }
            catch (Exception ex)
            {
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ERROR: Accel parsing - {ex.Message}");
                // DO NOT invoke ProgressUpdate from background thread
                // ProgressUpdate?.Invoke(this, $"[ERROR] Accel parsing failed: {ex.Message}");
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
            connection.StatusTimer?.Dispose();
            connection.Stream?.Dispose();
        }

        _connections.Clear();
        
        _logWriter?.WriteLine($"=== LOG ENDED {DateTime.Now} ===");
        _logWriter?.Dispose();
        
        _diagnosticLogger?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
