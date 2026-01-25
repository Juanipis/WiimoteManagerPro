using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiimoteManager.Models;
using WiimoteManager.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace WiimoteManager.ViewModels;

/// <summary>
/// ViewModel for the Button Test Diagnostic Window.
/// Allows systematic testing of button mapping to identify issues.
/// </summary>
public partial class ButtonTestViewModel : ObservableObject
{
    private readonly WiimoteService _wiimoteService;
    private readonly WiimoteDevice _device;
    private string _expectedButton = string.Empty;
    private CancellationTokenSource? _testCancellation;

    [ObservableProperty]
    private string instructions = "Click 'Start Test' and follow on-screen prompts to press each button.";

    [ObservableProperty]
    private string currentTestButton = "";

    [ObservableProperty]
    private string lastDetectedButtons = "None";

    [ObservableProperty]
    private string lastRawHex = "0x0000";

    [ObservableProperty]
    private bool isTestRunning = false;

    [ObservableProperty]
    private int testsCompleted = 0;

    [ObservableProperty]
    private int testsCorrect = 0;

    [ObservableProperty]
    private int testsIncorrect = 0;

    [ObservableProperty]
    private ObservableCollection<ButtonTestResultViewModel> testResults = new();

    [ObservableProperty]
    private string logsPath = "";

    public ButtonTestViewModel(WiimoteDevice device, WiimoteService wiimoteService)
    {
        _device = device;
        _wiimoteService = wiimoteService;

        // Subscribe to button changes
        _device.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(WiimoteDevice.CurrentButtonState))
            {
                OnButtonStateChanged();
            }
        };

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        LogsPath = Path.Combine(baseDir, "Logs");
    }

    [RelayCommand]
    private async Task StartTest()
    {
        IsTestRunning = true;
        TestResults.Clear();
        TestsCompleted = 0;
        TestsCorrect = 0;
        TestsIncorrect = 0;

        _testCancellation = new CancellationTokenSource();

        var buttonsToTest = new[]
        {
            "A", "B", "1", "2", "+", "-", "Home",
            "↑", "↓", "←", "→"
        };

        try
        {
            foreach (var button in buttonsToTest)
            {
                if (_testCancellation.Token.IsCancellationRequested)
                    break;

                await TestButton(button, _testCancellation.Token);
            }

            Instructions = "Test Complete! Check results below.";
            CurrentTestButton = "";

            // Generate summary
            if (_wiimoteService.DiagnosticLogger != null)
            {
                var summary = _wiimoteService.DiagnosticLogger.GenerateButtonTestSummary();
                var csvPath = _wiimoteService.DiagnosticLogger.ExportToCSV();
                Instructions = $"Test complete. Logs saved to:\n{LogsPath}\nCSV: {csvPath}";
            }
        }
        catch (TaskCanceledException)
        {
            Instructions = "Test stopped by user.";
            CurrentTestButton = "";
        }
        finally
        {
            IsTestRunning = false;
        }
    }

    [RelayCommand]
    private void StopTest()
    {
        _testCancellation?.Cancel();
        IsTestRunning = false;
        Instructions = "Test stopped.";
        CurrentTestButton = "";
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            if (Directory.Exists(LogsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = LogsPath,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    private async Task TestButton(string buttonName, CancellationToken ct)
    {
        _expectedButton = buttonName;
        CurrentTestButton = $"Press: {buttonName}";
        
        // Special message for Home button
        if (buttonName == "Home")
        {
            Instructions = $"Press the HOME button (round button below D-Pad). Note: This button may not be detected in all Wiimote models. Will auto-skip after 10 seconds...";
        }
        else
        {
            Instructions = $"Please press the {buttonName} button now and hold for 1 second...";
        }

        var previousState = _device.CurrentButtonState;
        var timeout = DateTime.Now.AddSeconds(10);

        try
        {
            // Wait for button press
            while (!ct.IsCancellationRequested && DateTime.Now < timeout)
            {
                await Task.Delay(50, ct);

                if (_device.CurrentButtonState != ButtonState.None && _device.CurrentButtonState != previousState)
                {
                    // Button pressed!
                    await Task.Delay(500, ct); // Hold detection
                    RecordButtonTest(buttonName, (ushort)_device.CurrentButtonState, _device.CurrentButtonState);

                    // Wait for release
                    while (_device.CurrentButtonState != ButtonState.None && !ct.IsCancellationRequested)
                    {
                        await Task.Delay(50, ct);
                    }

                    await Task.Delay(500, ct); // Pause before next
                    return;
                }
            }

            // Timeout
            if (buttonName == "Home")
            {
                Instructions = $"Home button not detected (this is common). Skipping to next button...";
            }
            else
            {
                Instructions = $"Timeout waiting for {buttonName}. Moving to next...";
            }
            await Task.Delay(1000, ct);
        }
        catch (TaskCanceledException)
        {
            // User stopped test, just rethrow to be caught by StartTest
            throw;
        }
    }

    private void OnButtonStateChanged()
    {
        if (_device.CurrentButtonState == ButtonState.None)
        {
            LastDetectedButtons = "None";
            LastRawHex = "0x0000";
            return;
        }

        var buttons = GetButtonNames(_device.CurrentButtonState);
        LastDetectedButtons = string.Join(", ", buttons);
        LastRawHex = $"0x{(ushort)_device.CurrentButtonState:X4}";
    }

    private void RecordButtonTest(string expectedButton, ushort rawValue, ButtonState actualState)
    {
        var actualButtons = GetButtonNames(actualState);
        var isCorrect = actualButtons.Contains(expectedButton, StringComparer.OrdinalIgnoreCase);

        // Log to diagnostic logger
        _wiimoteService.DiagnosticLogger?.LogButtonPress(expectedButton, rawValue, actualState);

        // Update UI
        TestsCompleted++;
        if (isCorrect)
            TestsCorrect++;
        else
            TestsIncorrect++;

        var result = new ButtonTestResultViewModel
        {
            ExpectedButton = expectedButton,
            RawHex = $"0x{rawValue:X4}",
            DetectedButtons = string.Join(", ", actualButtons),
            IsCorrect = isCorrect,
            Status = isCorrect ? "✓ PASS" : "✗ FAIL"
        };

        TestResults.Add(result);
    }

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
}

public partial class ButtonTestResultViewModel : ObservableObject
{
    [ObservableProperty]
    private string expectedButton = "";

    [ObservableProperty]
    private string rawHex = "";

    [ObservableProperty]
    private string detectedButtons = "";

    [ObservableProperty]
    private bool isCorrect = false;

    [ObservableProperty]
    private string status = "";
}
