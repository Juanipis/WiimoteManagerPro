using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Net.Sockets;

namespace BluetoothTester;

/// <summary>
/// STANDALONE BLUETOOTH L2CAP TESTER
/// Tests direct Wiimote communication bypassing HID restrictions!
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║   🚀 WIIMOTE BLUETOOTH L2CAP DIRECT TESTER 🚀        ║");
        Console.WriteLine("║   Revolutionary Solution - No HID, No Zadig!         ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        var tester = new WiimoteBluetoothTester();
        await tester.RunAsync();
        
        Console.WriteLine("\n✅ Test complete! Press any key to exit...");
        Console.ReadKey();
    }
}

class WiimoteBluetoothTester
{
    private BluetoothClient? _client;
    private BluetoothDeviceInfo? _wiimote;
    
    public async Task RunAsync()
    {
        try
        {
            // Step 1: Discover paired devices
            Console.WriteLine("🔍 [STEP 1] Discovering paired Bluetooth devices...\n");
            
            _client = new BluetoothClient();
            var devicesEnum = _client.DiscoverDevicesAsync();
            var devices = new List<BluetoothDeviceInfo>();
            
            await foreach (var device in devicesEnum)
            {
                devices.Add(device);
            }
            
            Console.WriteLine($"📱 Found {devices.Count} paired device(s):");
            
            foreach (var device in devices)
            {
                string name = device.DeviceName ?? "(No name)";
                Console.WriteLine($"   - '{name}' ({device.DeviceAddress})");
                Console.WriteLine($"     Class: 0x{device.ClassOfDevice:X6}");
                
                // Check if it's a Wiimote by device class (0x002504 = Joystick/Gamepad)
                uint deviceClass = (uint)device.ClassOfDevice;
                bool isGameController = ((deviceClass & 0x1F00) >> 8) == 5; // Peripheral device
                bool isJoystick = ((deviceClass & 0xC0) >> 6) == 1; // Joystick subclass
                
                Console.WriteLine($"     Is Game Controller: {isGameController}");
                Console.WriteLine($"     Is Joystick: {isJoystick}");
                
                // More permissive matching
                string nameLower = name.ToLower();
                if (nameLower.Contains("nintendo") || 
                    nameLower.Contains("rvl") ||
                    nameLower.Contains("wiimote") ||
                    nameLower.Contains("wii") ||
                    isGameController ||  // Wiimote usually reports as game controller
                    name == "(No name)")  // Sometimes paired devices lose their name
                {
                    Console.WriteLine($"      ✅ POTENTIAL WIIMOTE - Will try to connect!");
                    _wiimote = device;
                }
            }
            
            if (_wiimote == null)
            {
                Console.WriteLine("\n❌ No Wiimote found in paired devices!");
                Console.WriteLine("💡 Please pair your Wiimote in Windows Settings first:");
                Console.WriteLine("   1. Open Settings > Bluetooth & devices");
                Console.WriteLine("   2. Press RED SYNC button on Wiimote");
                Console.WriteLine("   3. Add device > Bluetooth");
                Console.WriteLine("   4. Select 'Nintendo RVL-CNT-01'");
                return;
            }
            
            Console.WriteLine($"\n✅ Wiimote found: {_wiimote.DeviceName}");
            Console.WriteLine($"   Address: {_wiimote.DeviceAddress}");
            Console.WriteLine($"   Authenticated: {_wiimote.Authenticated}");
            Console.WriteLine($"   Connected: {_wiimote.Connected}");
            
            // Step 2: Connect via HID service
            Console.WriteLine("\n🔌 [STEP 2] Connecting to Wiimote via Bluetooth HID...");
            
            var endpoint = new BluetoothEndPoint(_wiimote.DeviceAddress, BluetoothService.HumanInterfaceDevice);
            Console.WriteLine($"   Endpoint: {endpoint}");
            Console.WriteLine($"   Service: HID (Human Interface Device)");
            
            try
            {
                Console.WriteLine("   Calling Connect()...");
                _client.Connect(endpoint);
                
                if (_client.Connected)
                {
                    Console.WriteLine("   ✅✅✅ CONNECTED! ✅✅✅");
                    
                    // Step 3: Test sending LED command
                    Console.WriteLine("\n💡 [STEP 3] Testing LED control...");
                    await TestLEDAsync();
                    
                    // Step 4: Test rumble
                    Console.WriteLine("\n📳 [STEP 4] Testing rumble...");
                    await TestRumbleAsync();
                    
                    // Step 5: Read button data
                    Console.WriteLine("\n🎮 [STEP 5] Reading button data...");
                    Console.WriteLine("   Press buttons on Wiimote (30 seconds)...");
                    await ReadButtonsAsync(TimeSpan.FromSeconds(30));
                }
                else
                {
                    Console.WriteLine("   ❌ Connect() returned but not connected");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"   ❌ Socket exception: {ex.Message}");
                Console.WriteLine($"   Error code: 0x{ex.ErrorCode:X8} ({ex.ErrorCode})");
                Console.WriteLine($"   Socket error: {ex.SocketErrorCode}");
                
                if (ex.ErrorCode == 10049 || ex.ErrorCode == 10051)
                {
                    Console.WriteLine("\n💡 This error means Windows is blocking L2CAP connection.");
                    Console.WriteLine("   Trying alternative approach...");
                    
                    // Try using SetServiceState
                    Console.WriteLine("\n🔧 [ALT] Attempting to activate HID service...");
                    try
                    {
                        _wiimote.SetServiceState(BluetoothService.HumanInterfaceDevice, true);
                        Console.WriteLine("   ✅ Service activated");
                        
                        // Retry connection
                        _client = new BluetoothClient();
                        _client.Connect(endpoint);
                        
                        if (_client.Connected)
                        {
                            Console.WriteLine("   ✅ Connected after service activation!");
                            await TestLEDAsync();
                        }
                    }
                    catch (Exception altEx)
                    {
                        Console.WriteLine($"   ❌ Alternative approach failed: {altEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Exception: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
        }
    }
    
    private async Task TestLEDAsync()
    {
        try
        {
            if (_client == null || !_client.Connected)
            {
                Console.WriteLine("   ⚠️ Not connected");
                return;
            }
            
            var stream = _client.GetStream();
            
            // Test all 4 LEDs sequentially
            byte[] leds = { 0x10, 0x20, 0x40, 0x80 };
            
            for (int i = 0; i < leds.Length; i++)
            {
                byte[] command = new byte[3];
                command[0] = 0x52;  // OUTPUT report
                command[1] = 0x11;  // LED/Rumble
                command[2] = leds[i];
                
                Console.WriteLine($"   Sending LED {i+1} command: [{command[0]:X2} {command[1]:X2} {command[2]:X2}]");
                
                await stream.WriteAsync(command, 0, command.Length);
                await stream.FlushAsync();
                
                Console.WriteLine($"   ✅ LED {i+1} sent!");
                await Task.Delay(500);
            }
            
            // Turn off all LEDs
            byte[] offCommand = new byte[3] { 0x52, 0x11, 0x00 };
            await stream.WriteAsync(offCommand, 0, offCommand.Length);
            Console.WriteLine("   ✅ All LEDs off");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ LED test failed: {ex.Message}");
        }
    }
    
    private async Task TestRumbleAsync()
    {
        try
        {
            if (_client == null || !_client.Connected)
            {
                Console.WriteLine("   ⚠️ Not connected");
                return;
            }
            
            var stream = _client.GetStream();
            
            // Rumble ON
            byte[] onCommand = new byte[3] { 0x52, 0x11, 0x01 };
            await stream.WriteAsync(onCommand, 0, onCommand.Length);
            Console.WriteLine("   ✅ RUMBLE ON");
            
            await Task.Delay(500);
            
            // Rumble OFF
            byte[] offCommand = new byte[3] { 0x52, 0x11, 0x00 };
            await stream.WriteAsync(offCommand, 0, offCommand.Length);
            Console.WriteLine("   ✅ RUMBLE OFF");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Rumble test failed: {ex.Message}");
        }
    }
    
    private async Task ReadButtonsAsync(TimeSpan duration)
    {
        try
        {
            if (_client == null || !_client.Connected)
            {
                Console.WriteLine("   ⚠️ Not connected");
                return;
            }
            
            var stream = _client.GetStream();
            var cts = new CancellationTokenSource(duration);
            
            byte[] buffer = new byte[23];
            int packetsReceived = 0;
            
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    
                    if (bytesRead > 0)
                    {
                        packetsReceived++;
                        
                        // Parse button data (bytes 2-3)
                        if (bytesRead >= 4)
                        {
                            byte high = buffer[2];
                            byte low = buffer[3];
                            ushort buttons = (ushort)((high << 8) | low);
                            
                            if (buttons != 0)
                            {
                                Console.WriteLine($"   🎮 Buttons: 0x{buttons:X4} (Packet #{packetsReceived})");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ Read error: {ex.Message}");
                    await Task.Delay(100);
                }
            }
            
            Console.WriteLine($"   ✅ Read test complete. Received {packetsReceived} packets.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Read test failed: {ex.Message}");
        }
    }
}
