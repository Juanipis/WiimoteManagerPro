using System.IO;
using System.Text;
using WiimoteManager.Models;

namespace WiimoteManager.Services;

/// <summary>
/// Diagnostic logger for debugging button mapping and sensor issues.
/// Logs are saved in the solution's Logs folder for persistent debugging.
/// </summary>
public class DiagnosticLogger : IDisposable
{
    private readonly string _logDirectory;
    private readonly string _sessionLogPath;
    private readonly StreamWriter _sessionWriter;
    private readonly List<ButtonTestResult> _buttonTestResults = new();
    private readonly object _lock = new();

    public IReadOnlyList<ButtonTestResult> ButtonTestResults => _buttonTestResults.AsReadOnly();

    public DiagnosticLogger()
    {
        // Create Logs folder in solution directory
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _logDirectory = Path.Combine(baseDirectory, "Logs");
        Directory.CreateDirectory(_logDirectory);

        // Create session log file
        var sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _sessionLogPath = Path.Combine(_logDirectory, $"diagnostic_session_{sessionId}.log");
        
        _sessionWriter = new StreamWriter(_sessionLogPath, false, Encoding.UTF8) { AutoFlush = true };
        
        LogSessionHeader();
    }

    private void LogSessionHeader()
    {
        _sessionWriter.WriteLine("========================================");
        _sessionWriter.WriteLine("WIIMOTE DIAGNOSTIC SESSION");
        _sessionWriter.WriteLine($"Session Start: {DateTime.Now}");
        _sessionWriter.WriteLine($"OS: {Environment.OSVersion}");
        _sessionWriter.WriteLine($".NET Version: {Environment.Version}");
        _sessionWriter.WriteLine("========================================");
        _sessionWriter.WriteLine();
    }

    /// <summary>
    /// Logs raw button data with expected and actual button names
    /// </summary>
    public void LogButtonPress(string expectedButton, ushort rawButtonValue, ButtonState actualState)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now;
            var actualButtons = GetButtonNames(actualState);

            var result = new ButtonTestResult
            {
                Timestamp = timestamp,
                ExpectedButton = expectedButton,
                RawHexValue = rawButtonValue,
                ActualButtonState = actualState,
                ActualButtonNames = actualButtons,
                IsCorrect = actualButtons.Contains(expectedButton, StringComparer.OrdinalIgnoreCase)
            };

            _buttonTestResults.Add(result);

            // Log to file
            _sessionWriter.WriteLine($"[{timestamp:HH:mm:ss.fff}] BUTTON TEST");
            _sessionWriter.WriteLine($"  Expected: {expectedButton}");
            _sessionWriter.WriteLine($"  Raw Hex:  0x{rawButtonValue:X4}");
            _sessionWriter.WriteLine($"  Detected: {string.Join(", ", actualButtons)}");
            _sessionWriter.WriteLine($"  Match:    {(result.IsCorrect ? "✓ CORRECT" : "✗ INCORRECT")}");
            _sessionWriter.WriteLine();
        }
    }

    /// <summary>
    /// Logs raw HID packet data
    /// </summary>
    public void LogRawPacket(byte reportId, byte[] data, int length)
    {
        lock (_lock)
        {
            var hex = BitConverter.ToString(data, 0, Math.Min(length, 22));
            _sessionWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] RAW PACKET");
            _sessionWriter.WriteLine($"  Report ID: 0x{reportId:X2}");
            _sessionWriter.WriteLine($"  Length:    {length}");
            _sessionWriter.WriteLine($"  Data:      {hex}");
            _sessionWriter.WriteLine();
        }
    }

    /// <summary>
    /// Logs battery reading
    /// </summary>
    public void LogBatteryReading(byte rawValue, int calculatedPercent)
    {
        lock (_lock)
        {
            _sessionWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BATTERY");
            _sessionWriter.WriteLine($"  Raw Byte: 0x{rawValue:X2} ({rawValue})");
            _sessionWriter.WriteLine($"  Percent:  {calculatedPercent}%");
            _sessionWriter.WriteLine();
        }
    }

    /// <summary>
    /// Logs accelerometer data
    /// </summary>
    public void LogAccelerometer(int x10bit, int y10bit, int z10bit, float xNorm, float yNorm, float zNorm)
    {
        lock (_lock)
        {
            _sessionWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ACCELEROMETER");
            _sessionWriter.WriteLine($"  10-bit:    X={x10bit} Y={y10bit} Z={z10bit}");
            _sessionWriter.WriteLine($"  Normalized: X={xNorm:F3} Y={yNorm:F3} Z={zNorm:F3}");
            _sessionWriter.WriteLine();
        }
    }

    /// <summary>
    /// Generates a summary report of button test results
    /// </summary>
    public string GenerateButtonTestSummary()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("BUTTON TEST SUMMARY");
            sb.AppendLine($"Total Tests: {_buttonTestResults.Count}");
            sb.AppendLine($"Correct: {_buttonTestResults.Count(r => r.IsCorrect)}");
            sb.AppendLine($"Incorrect: {_buttonTestResults.Count(r => !r.IsCorrect)}");
            sb.AppendLine("========================================");
            sb.AppendLine();

            var grouped = _buttonTestResults.GroupBy(r => r.ExpectedButton);
            foreach (var group in grouped)
            {
                sb.AppendLine($"Button: {group.Key}");
                foreach (var result in group)
                {
                    sb.AppendLine($"  0x{result.RawHexValue:X4} -> {string.Join(", ", result.ActualButtonNames)} [{(result.IsCorrect ? "OK" : "FAIL")}]");
                }
                sb.AppendLine();
            }

            var summary = sb.ToString();
            _sessionWriter.WriteLine(summary);

            // Also save to separate summary file
            var summaryPath = Path.Combine(_logDirectory, $"button_test_summary_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(summaryPath, summary);

            return summary;
        }
    }

    /// <summary>
    /// Exports test results to CSV for analysis
    /// </summary>
    public string ExportToCSV()
    {
        lock (_lock)
        {
            var csvPath = Path.Combine(_logDirectory, $"button_test_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            
            using var writer = new StreamWriter(csvPath);
            writer.WriteLine("Timestamp,Expected,RawHex,ActualButtons,IsCorrect");
            
            foreach (var result in _buttonTestResults)
            {
                writer.WriteLine($"{result.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{result.ExpectedButton},0x{result.RawHexValue:X4},\"{string.Join("; ", result.ActualButtonNames)}\",{result.IsCorrect}");
            }

            return csvPath;
        }
    }

    /// <summary>
    /// Gets button names from ButtonState enum
    /// </summary>
    private static List<string> GetButtonNames(ButtonState state)
    {
        var buttons = new List<string>();
        
        if (state.HasFlag(ButtonState.A)) buttons.Add("A");
        if (state.HasFlag(ButtonState.B)) buttons.Add("B");
        if (state.HasFlag(ButtonState.One)) buttons.Add("1");
        if (state.HasFlag(ButtonState.Two)) buttons.Add("2");
        if (state.HasFlag(ButtonState.Plus)) buttons.Add("+");
        if (state.HasFlag(ButtonState.Minus)) buttons.Add("-");
        if (state.HasFlag(ButtonState.Home)) buttons.Add("Home");
        if (state.HasFlag(ButtonState.DPadUp)) buttons.Add("↑");
        if (state.HasFlag(ButtonState.DPadDown)) buttons.Add("↓");
        if (state.HasFlag(ButtonState.DPadLeft)) buttons.Add("←");
        if (state.HasFlag(ButtonState.DPadRight)) buttons.Add("→");

        return buttons;
    }

    public void Dispose()
    {
        _sessionWriter?.WriteLine($"Session End: {DateTime.Now}");
        _sessionWriter?.Dispose();
    }
}

/// <summary>
/// Represents a single button test result
/// </summary>
public class ButtonTestResult
{
    public DateTime Timestamp { get; set; }
    public string ExpectedButton { get; set; } = string.Empty;
    public ushort RawHexValue { get; set; }
    public ButtonState ActualButtonState { get; set; }
    public List<string> ActualButtonNames { get; set; } = new();
    public bool IsCorrect { get; set; }
}
