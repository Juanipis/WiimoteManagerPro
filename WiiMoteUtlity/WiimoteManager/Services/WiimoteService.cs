using HidSharp;
using WiimoteManager.Models;

namespace WiimoteManager.Services;

public class WiimoteService : IDisposable
{
    private const int NINTENDO_VENDOR_ID = 0x057E;
    private const int WIIMOTE_PRODUCT_ID = 0x0306;
    private const int WIIMOTE_PLUS_PRODUCT_ID = 0x0330;
    
    private readonly List<HidDevice> _connectedDevices = new();
    private readonly List<HidStream> _streams = new();
    private bool _disposed = false;

    public event EventHandler<string>? ProgressUpdate;
    public event EventHandler<WiimoteDevice>? WiimoteConnected;
    public event EventHandler<WiimoteDevice>? WiimoteDisconnected;

    public async Task<int> DiscoverWiimotesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                ProgressUpdate?.Invoke(this, "Searching for Wiimotes via HID...");
                ProgressUpdate?.Invoke(this, $"[DEBUG] Looking for VID=0x{NINTENDO_VENDOR_ID:X4}, PID=0x{WIIMOTE_PRODUCT_ID:X4} or 0x{WIIMOTE_PLUS_PRODUCT_ID:X4}");
                
                var deviceList = DeviceList.Local;
                var allDevices = deviceList.GetHidDevices().ToArray();
                
                ProgressUpdate?.Invoke(this, $"[DEBUG] Found {allDevices.Length} total HID devices");
                
                var wiimotes = allDevices.Where(d => 
                    d.VendorID == NINTENDO_VENDOR_ID && 
                    (d.ProductID == WIIMOTE_PRODUCT_ID || d.ProductID == WIIMOTE_PLUS_PRODUCT_ID)
                ).ToList();
                
                ProgressUpdate?.Invoke(this, $"Found {wiimotes.Count} Wiimote HID device(s)");
                
                if (wiimotes.Count == 0)
                {
                    ProgressUpdate?.Invoke(this, "[INFO] No Wiimote HID devices found.");
                    ProgressUpdate?.Invoke(this, "[INFO] Wiimote must be paired AND powered on");
                    
                    var nintendoDevices = allDevices.Where(d => d.VendorID == NINTENDO_VENDOR_ID).ToArray();
                    if (nintendoDevices.Length > 0)
                    {
                        ProgressUpdate?.Invoke(this, $"[DEBUG] Found {nintendoDevices.Length} Nintendo device(s):");
                        foreach (var dev in nintendoDevices)
                        {
                            ProgressUpdate?.Invoke(this, $"[DEBUG]   VID=0x{dev.VendorID:X4}, PID=0x{dev.ProductID:X4}");
                        }
                    }
                    
                    return 0;
                }

                int connectedCount = 0;
                
                for (int i = 0; i < wiimotes.Count; i++)
                {
                    var hidDevice = wiimotes[i];
                    
                    try
                    {
                        ProgressUpdate?.Invoke(this, $"[INFO] Connecting to Wiimote {i + 1}...");
                        
                        if (!hidDevice.TryOpen(out var stream))
                        {
                            ProgressUpdate?.Invoke(this, $"[ERROR] Failed to open Wiimote {i + 1}");
                            continue;
                        }
                        
                        ProgressUpdate?.Invoke(this, $"[SUCCESS] Opened HID stream");
                        
                        _connectedDevices.Add(hidDevice);
                        _streams.Add(stream);
                        
                        var device = new WiimoteDevice
                        {
                            DeviceId = $"wiimote_{i}",
                            DeviceName = $"Nintendo RVL-CNT-01 #{i + 1}",
                            HidPath = hidDevice.DevicePath,
                            IsConnected = true,
                            IsPaired = true
                        };
                        
                        SetLED(stream, i % 4);
                        _ = Task.Run(() => ReadInputLoop(stream, device));
                        
                        ProgressUpdate?.Invoke(this, $"✓ Wiimote {i + 1} connected!");
                        WiimoteConnected?.Invoke(this, device);
                        
                        connectedCount++;
                    }
                    catch (Exception ex)
                    {
                        ProgressUpdate?.Invoke(this, $"[ERROR] {ex.Message}");
                    }
                }
                
                ProgressUpdate?.Invoke(this, $"Complete. {connectedCount} connected.");
                return connectedCount;
            }
            catch (Exception ex)
            {
                ProgressUpdate?.Invoke(this, $"[FATAL] {ex.Message ?? "Unknown"}");
                return 0;
            }
        });
    }

    private void SetLED(HidStream stream, int ledIndex)
    {
        try
        {
            byte ledFlags = ledIndex switch
            {
                0 => 0x10, // LED 1
                1 => 0x20, // LED 2
                2 => 0x40, // LED 3  
                3 => 0x80, // LED 4
                _ => 0x10
            };
            
            // Wiimote LED report format for HID:
            // Byte 0: 0xA2 (HID output report)
            // Byte 1: 0x11 (LED command)
            // Byte 2: LED flags
            byte[] report = new byte[3];
            report[0] = 0xA2; // HID output report
            report[1] = 0x11; // LED command
            report[2] = ledFlags;
            
            stream.Write(report);
            ProgressUpdate?.Invoke(this, $"[DEBUG] Set LED {ledIndex + 1}");
            
            // Wait a bit then request button data
            Thread.Sleep(100);
            RequestButtonData(stream);
        }
        catch (Exception ex)
        {
            ProgressUpdate?.Invoke(this, $"[WARN] LED failed: {ex.Message}");
            // Even if LED fails, try to get button data
            try { RequestButtonData(stream); } catch { }
        }
    }
    
    private void RequestButtonData(HidStream stream)
    {
        try
        {
            // Set report type to buttons only (0x30) - simpler and more reliable
            // Format: 0xA2 (HID output), 0x12 (set report), continuous flag, report type
            byte[] report = new byte[4];
            report[0] = 0xA2; // HID output report
            report[1] = 0x12; // Set report type command
            report[2] = 0x00; // Continuous reporting
            report[3] = 0x30; // Report type: buttons only (simpler)
            
            stream.Write(report);
            ProgressUpdate?.Invoke(this, $"[DEBUG] Requested button data (report 0x30)");
        }
        catch (Exception ex)
        {
            ProgressUpdate?.Invoke(this, $"[WARN] Request data failed: {ex.Message}");
        }
    }

    private async Task ReadInputLoop(HidStream stream, WiimoteDevice device)
    {
        byte[] buffer = new byte[stream.Device.GetMaxInputReportLength()];
        
        try
        {
            ProgressUpdate?.Invoke(this, $"[DEBUG] Starting read loop (buffer size: {buffer.Length})");
            
            // Set a longer timeout - Wiimote might be slow to respond initially
            stream.ReadTimeout = 10000; // 10 seconds
            
            while (!_disposed && stream.CanRead)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        // Parse the report
                        byte reportId = buffer[0];
                        
                        // Report 0x30-0x3F are input reports with button data
                        if (reportId >= 0x30 && reportId <= 0x3F)
                        {
                            // Parse button state (bytes 1-2)
                            ushort buttons = (ushort)((buffer[1] << 8) | buffer[2]);
                            ParseButtons(device, buttons);
                            
                            // Parse accelerometer if available (report 0x31+)
                            if (reportId >= 0x31 && bytesRead >= 6)
                            {
                                device.AccelX = buffer[3];
                                device.AccelY = buffer[4];
                                device.AccelZ = buffer[5];
                            }
                            
                            // Update connection indicators
                            device.IsConnected = true;
                            device.SignalStrength = 100; // Connected via HID = full strength
                            device.BatteryLevel = 85; // Placeholder - will add proper battery reading
                            device.UpdateLastCommunication();
                            
                            // Only log when buttons are pressed
                            if (buttons != 0)
                            {
                                ProgressUpdate?.Invoke(this, $"[INPUT] Buttons: 0x{buttons:X4}");
                            }
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Timeout is normal if no buttons pressed - just continue
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            ProgressUpdate?.Invoke(this, $"[ERROR] Read loop ended: {ex.Message}");
            device.IsConnected = false;
            WiimoteDisconnected?.Invoke(this, device);
        }
    }
    
    private void ParseButtons(WiimoteDevice device, ushort buttons)
    {
        // Wiimote button mapping (from Wiimote protocol)
        ButtonState newState = ButtonState.None;
        
        // First byte (buffer[1])
        if ((buttons & 0x0001) != 0) newState |= ButtonState.DPadLeft;
        if ((buttons & 0x0002) != 0) newState |= ButtonState.DPadRight;
        if ((buttons & 0x0004) != 0) newState |= ButtonState.DPadDown;
        if ((buttons & 0x0008) != 0) newState |= ButtonState.DPadUp;
        if ((buttons & 0x0010) != 0) newState |= ButtonState.Plus;
        
        // Second byte (buffer[2])
        if ((buttons & 0x0100) != 0) newState |= ButtonState.Two;
        if ((buttons & 0x0200) != 0) newState |= ButtonState.One;
        if ((buttons & 0x0400) != 0) newState |= ButtonState.B;
        if ((buttons & 0x0800) != 0) newState |= ButtonState.A;
        if ((buttons & 0x1000) != 0) newState |= ButtonState.Minus;
        if ((buttons & 0x8000) != 0) newState |= ButtonState.Home;
        
        device.CurrentButtonState = newState;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var stream in _streams)
        {
            try { stream?.Dispose(); } catch { }
        }
        
        _streams.Clear();
        _connectedDevices.Clear();
    }
}
