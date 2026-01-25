using HidSharp;

namespace WiimoteHardwareTest;

class Program
{
    const int NINTENDO_VID = 0x057E;
    const int WIIMOTE_PID = 0x0306;
    const int WIIMOTE_PLUS_PID = 0x0330;
    
    static HidStream? _stream;
    static HidDevice? _device;
    
    static void Main(string[] args)
    {
        Console.WriteLine("=== WIIMOTE HARDWARE TEST ===\n");
        
        try
        {
            Console.WriteLine("1. Searching for Wiimote...");
            var deviceList = DeviceList.Local;
            var devices = deviceList.GetHidDevices();
            
            _device = devices.FirstOrDefault(d => 
                d.VendorID == NINTENDO_VID && 
                (d.ProductID == WIIMOTE_PID || d.ProductID == WIIMOTE_PLUS_PID));
            
            if (_device == null)
            {
                Console.WriteLine("No Wiimote found.");
                return;
            }
            
            Console.WriteLine($"Found: VID=0x{_device.VendorID:X4}, PID=0x{_device.ProductID:X4}");
            Console.WriteLine($"Max Input: {_device.GetMaxInputReportLength()} bytes");
            Console.WriteLine($"Max Output: {_device.GetMaxOutputReportLength()} bytes\n");
            
            Console.WriteLine("2. Opening HID stream...");
            if (!_device.TryOpen(out _stream))
            {
                Console.WriteLine("Failed to open stream");
                return;
            }
            Console.WriteLine("Stream opened\n");
            
            while (true)
            {
                Console.WriteLine("\n=== TEST MENU ===");
                Console.WriteLine("1. Test LED 1");
                Console.WriteLine("2. Test LED 2");
                Console.WriteLine("3. Test LED 3");
                Console.WriteLine("4. Test LED 4");
                Console.WriteLine("5. Test ALL LEDs");
                Console.WriteLine("6. Test Rumble");
                Console.WriteLine("7. Request Buttons (0x30)");
                Console.WriteLine("8. Request Buttons+Accel (0x31)");
                Console.WriteLine("9. Read 10 packets");
                Console.WriteLine("D. Dump device info");
                Console.WriteLine("0. Exit");
                Console.Write("\nChoice: ");
                
                var choice = Console.ReadLine()?.Trim().ToUpper();
                
                switch (choice)
                {
                    case "1": TestLED(0x10, "LED 1"); break;
                    case "2": TestLED(0x20, "LED 2"); break;
                    case "3": TestLED(0x40, "LED 3"); break;
                    case "4": TestLED(0x80, "LED 4"); break;
                    case "5": TestLED(0xF0, "ALL LEDs"); break;
                    case "6": TestRumble(); break;
                    case "7": RequestData(0x30); break;
                    case "8": RequestData(0x31); break;
                    case "9": ReadData(); break;
                    case "D": DumpInfo(); break;
                    case "0": return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
        finally
        {
            _stream?.Dispose();
        }
    }
    
    static void TestLED(byte ledMask, string name)
    {
        Console.WriteLine($"\nTesting {name}...");
        
        // Test Write() method
        Console.WriteLine("  [Write() method]");
        var testsWrite = new[] { 
            (22, (byte)0xA2), (22, (byte)0x52) 
        };
        
        foreach (var (size, prefix) in testsWrite)
        {
            try
            {
                Console.Write($"    {size}b, 0x{prefix:X2}... ");
                byte[] report = new byte[size];
                report[0] = prefix;
                report[1] = 0x11;
                report[2] = ledMask;
                _stream!.Write(report);
                Console.WriteLine("OK");
                Thread.Sleep(300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
            }
        }
        
        // Test SetFeature() method
        Console.WriteLine("  [SetFeature() method - Report ID based]");
        var testsFeature = new[] { 
            (22, (byte)0x11), // Report ID = 0x11 (LED command)
        };
        
        foreach (var (size, reportId) in testsFeature)
        {
            try
            {
                Console.Write($"    {size}b, ReportID=0x{reportId:X2}... ");
                byte[] report = new byte[size];
                report[0] = reportId;  // Report ID
                report[1] = ledMask;   // LED mask (no 0x11 prefix needed)
                _stream!.SetFeature(report);
                Console.WriteLine("OK - CHECK WIIMOTE NOW!");
                Thread.Sleep(1500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
            }
        }
    }
    
    static void TestRumble()
    {
        Console.WriteLine("\nTesting Rumble...");
        
        var tests = new[] { (22, (byte)0xA2), (22, (byte)0x52) };
        
        foreach (var (size, prefix) in tests)
        {
            try
            {
                Console.Write($"  {size}b, 0x{prefix:X2}... ");
                byte[] reportOn = new byte[size];
                reportOn[0] = prefix;
                reportOn[1] = 0x11;
                reportOn[2] = 0x01;
                _stream!.Write(reportOn);
                Console.Write("ON ");
                Thread.Sleep(500);
                
                byte[] reportOff = new byte[size];
                reportOff[0] = prefix;
                reportOff[1] = 0x11;
                _stream!.Write(reportOff);
                Console.WriteLine("OFF");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
            }
        }
    }
    
    static void RequestData(byte type)
    {
        Console.WriteLine($"\nRequesting 0x{type:X2}...");
        
        var tests = new[] {
            (22, (byte)0xA2, (byte)0x04),
            (22, (byte)0x52, (byte)0x04)
        };
        
        foreach (var (size, prefix, cont) in tests)
        {
            try
            {
                Console.Write($"  {size}b, 0x{prefix:X2}, cont=0x{cont:X2}... ");
                byte[] report = new byte[size];
                report[0] = prefix;
                report[1] = 0x12;
                report[2] = cont;
                report[3] = type;
                _stream!.Write(report);
                Console.WriteLine("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
            }
        }
    }
    
    static void ReadData()
    {
        Console.WriteLine("\nReading 10 packets...");
        _stream!.ReadTimeout = 2000;
        byte[] buffer = new byte[_device!.GetMaxInputReportLength()];
        
        for (int i = 0; i < 10; i++)
        {
            try
            {
                int n = _stream.Read(buffer, 0, buffer.Length);
                if (n > 0)
                {
                    Console.Write($"  [{i+1}] {n}b: ");
                    Console.Write(string.Join(" ", buffer.Take(10).Select(b => $"{b:X2}")));
                    
                    byte rid = buffer[0];
                    if (rid >= 0x30 && n >= 3)
                    {
                        ushort btns = (ushort)((buffer[1] << 8) | buffer[2]);
                        Console.Write($" | Btns=0x{btns:X4}");
                        if (rid >= 0x31 && n >= 6)
                            Console.Write($" | Accel={buffer[3]},{buffer[4]},{buffer[5]}");
                    }
                    Console.WriteLine();
                }
            }
            catch (TimeoutException) { Console.WriteLine($"  [{i+1}] Timeout"); }
            catch (Exception ex) { Console.WriteLine($"  [{i+1}] Error: {ex.Message}"); }
        }
    }
    
    static void DumpInfo()
    {
        Console.WriteLine("\nDevice Info:");
        Console.WriteLine($"  VID: 0x{_device!.VendorID:X4}");
        Console.WriteLine($"  PID: 0x{_device.ProductID:X4}");
        Console.WriteLine($"  Max In: {_device.GetMaxInputReportLength()}");
        Console.WriteLine($"  Max Out: {_device.GetMaxOutputReportLength()}");
    }
}
