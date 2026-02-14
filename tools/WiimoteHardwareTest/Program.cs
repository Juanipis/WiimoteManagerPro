using HidSharp;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace WiimoteHardwareTest;

class Program
{
    const int NINTENDO_VID = 0x057E;
    const int WIIMOTE_PID = 0x0306;
    const int WIIMOTE_PLUS_PID = 0x0330;
    
    static HidStream? _stream;
    static HidDevice? _device;
    static ViGEmClient? _vigem;
    static IXbox360Controller? _controller;
    
    static void Main(string[] args)
    {
        Console.WriteLine("=== WIIMOTE & VIGEM DIAGNOSTIC ===\n");
        
        // 1. Test ViGEmBus
        Console.WriteLine("STEP 1: Testing Virtual Controller Driver...");
        try 
        {
            _vigem = new ViGEmClient();
            Console.WriteLine("[SUCCESS] ViGEmBus Client initialized!");
            
            _controller = _vigem.CreateXbox360Controller();
            Console.WriteLine("[SUCCESS] Created Virtual Xbox 360 Controller object");
            
            _controller.Connect();
            Console.WriteLine("[SUCCESS] Connected Virtual Controller to system");
            Console.WriteLine("   -> Check 'joy.cpl' or Device Manager for 'Xbox 360 Controller for Windows'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL ERROR] ViGEmBus Failed: {ex.Message}");
        }

        Console.WriteLine("\n------------------------------------------------\n");

        // 2. Test Wiimote
        Console.WriteLine("STEP 2: Testing Wiimote Connection...");
        try
        {
            var deviceList = DeviceList.Local;
            var devices = deviceList.GetHidDevices().ToList();
            
            _device = devices.FirstOrDefault(d => 
                d.VendorID == NINTENDO_VID && 
                (d.ProductID == WIIMOTE_PID || d.ProductID == WIIMOTE_PLUS_PID));
            
            if (_device == null)
            {
                Console.WriteLine("[ERROR] No Wiimote found. Is it paired?");
                return;
            }
            
            Console.WriteLine($"[SUCCESS] Found Wiimote: {_device.GetProductName()}");
            
            if (!_device.TryOpen(out _stream))
            {
                Console.WriteLine("[ERROR] Failed to open HID stream.");
                return;
            }
            Console.WriteLine("[SUCCESS] Opened HID stream");
            
            // INIT: Set Data Reporting Mode (0x30: Buttons Only, 0x31: Buttons + Accel)
            // This is CRITICAL for receiving data!
            Console.WriteLine("   -> Sending Data Reporting Request (0x12 0x00 0x30)...");
            byte[] report = new byte[22];
            report[0] = 0x12; // Report Mode
            report[1] = 0x00; // Continuous
            report[2] = 0x30; // 0x30 = Core Buttons (No Accel to keep it simple)
            _stream.Write(report);
            
            // Set LED 1
            report[0] = 0x11;
            report[1] = 0x10; 
            _stream.Write(report);
            Console.WriteLine("[SUCCESS] Sent Configuration Commands");

            Console.WriteLine("\nSTEP 3: READING INPUTS (Press CTRL+C to stop)");
            Console.WriteLine("   -> Press buttons on the Wiimote.");
            Console.WriteLine("   -> Watch the RAW HEX output below.");
            
            byte[] buffer = new byte[22];
            while (true)
            {
                try
                {
                    int n = _stream.Read(buffer, 0, buffer.Length);
                    if (n > 0)
                    {
                       ParseAndDebug(buffer);
                    }
                }
                catch (TimeoutException)
                {
                    // Ignore timeouts, just keep waiting
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Wiimote Test Failed: {ex.Message}");
        }
    }
    
    static void ParseAndDebug(byte[] data)
    {
        byte rid = data[0];
        
        // Filter for Core Button Reports (0x30)
        if (rid == 0x30)
        {
            // Bytes 1 and 2 contain buttons (Big Endian)
            ushort b1 = data[1];
            ushort b2 = data[2];
            ushort buttons = (ushort)((b1 << 8) | b2);
            
            if (buttons != 0) // Only log when pressed
            {
                Console.Write($"\r[INPUT] RAW: {b1:X2} {b2:X2} -> BUTTONS: {buttons:X4} | ");
                
                // Decode known bits
                List<string> pressed = new();
                if ((buttons & 0x0001) != 0) pressed.Add("2");
                if ((buttons & 0x0002) != 0) pressed.Add("1");
                if ((buttons & 0x0004) != 0) pressed.Add("B");
                if ((buttons & 0x0008) != 0) pressed.Add("A");
                if ((buttons & 0x0010) != 0) pressed.Add("-");
                
                if ((buttons & 0x0080) != 0) pressed.Add("HOME");
                
                if ((buttons & 0x0100) != 0) pressed.Add("LEFT");
                if ((buttons & 0x0200) != 0) pressed.Add("RIGHT");
                if ((buttons & 0x0400) != 0) pressed.Add("DOWN");
                if ((buttons & 0x0800) != 0) pressed.Add("UP");
                if ((buttons & 0x1000) != 0) pressed.Add("+");
                
                Console.Write(string.Join(", ", pressed));
                Console.Write("                           "); // Clear line remainder
                
                // Forward to Xbox for testing
                if (_controller != null)
                {
                    _controller.ResetReport();
                    if (pressed.Contains("A")) _controller.SetButtonState(Xbox360Button.A, true); // Wiimote A -> Xbox A
                    if (pressed.Contains("B")) _controller.SetButtonState(Xbox360Button.B, true); // Wiimote B -> Xbox B
                    if (pressed.Contains("1")) _controller.SetButtonState(Xbox360Button.X, true); // Wiimote 1 -> Xbox X
                    if (pressed.Contains("2")) _controller.SetButtonState(Xbox360Button.Y, true); // Wiimote 2 -> Xbox Y
                    if (pressed.Contains("HOME")) _controller.SetButtonState(Xbox360Button.Guide, true);
                    
                    if (pressed.Contains("LEFT")) _controller.SetButtonState(Xbox360Button.Up, true);    // Dpad Rotation (Horizontal)
                    if (pressed.Contains("RIGHT")) _controller.SetButtonState(Xbox360Button.Down, true);
                    if (pressed.Contains("DOWN")) _controller.SetButtonState(Xbox360Button.Left, true);
                    if (pressed.Contains("UP")) _controller.SetButtonState(Xbox360Button.Right, true);
                    
                    _controller.SubmitReport();
                }
            }
        }
    }
}
