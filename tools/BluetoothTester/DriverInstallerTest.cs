using WiimoteManager.Services;

namespace BluetoothTester;

/// <summary>
/// TESTS THE AUTOMATED HID-WIIMOTE DRIVER INSTALLER
/// </summary>
class DriverInstallerTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║   🚀 HID-WIIMOTE DRIVER INSTALLER TEST 🚀            ║");
        Console.WriteLine("║   Automated Solution - Full Control!                 ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        var installer = new HidWiimoteDriverInstaller();
        
        // Subscribe to progress
        installer.ProgressUpdate += (sender, message) =>
        {
            Console.WriteLine(message);
        };
        
        // Run installation
        Console.WriteLine("Starting automated installation...\n");
        
        var success = await installer.AutoInstallAsync();
        
        if (success)
        {
            Console.WriteLine("\n✅ Installation completed successfully!");
            Console.WriteLine("🔄 REBOOT your computer to activate the driver");
            Console.WriteLine("🎮 After reboot, reconnect your Wiimote");
        }
        else
        {
            Console.WriteLine("\n❌ Installation failed");
            Console.WriteLine("💡 Make sure to run as Administrator");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
