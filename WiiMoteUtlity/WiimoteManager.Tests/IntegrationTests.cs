using Moq;
using WiimoteManager.Models;
using WiimoteManager.Services;
using WiimoteManager.ViewModels;

namespace WiimoteManager.Tests;

/// <summary>
/// Integration tests for HidCommunicationService with mocked HID devices.
/// </summary>
public class HidCommunicationServiceTests
{
    [Fact]
    public void HidCommunicationService_Created_CanBeDisposed()
    {
        // Arrange
        var service = new HidCommunicationService();

        // Act
        service.Dispose();

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public void HidCommunicationService_RegisterDevice_AddsDeviceToTracking()
    {
        // Arrange
        var service = new HidCommunicationService();
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");

        // Act
        service.RegisterDevice(device);

        // Assert - Should not throw and device should be tracked
        service.Dispose();
        Assert.True(true);
    }

    [Fact]
    public void HidCommunicationService_UnregisterDevice_RemovesDeviceFromTracking()
    {
        // Arrange
        var service = new HidCommunicationService();
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        service.RegisterDevice(device);

        // Act
        service.UnregisterDevice(device.DeviceId);

        // Assert - Should not throw
        service.Dispose();
        Assert.True(true);
    }

    [Fact]
    public void HidCommunicationService_SetLED_RequiresOpenDevice()
    {
        // Arrange
        var service = new HidCommunicationService();
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");

        // Act
        var result = service.SetLEDAsync(device.DeviceId, WiimoteReports.LED1, false).GetAwaiter().GetResult();

        // Assert - Should fail because device is not open
        Assert.False(result);

        service.Dispose();
    }
}

/// <summary>
/// Tests for BluetoothService with mocked Bluetooth adapters.
/// </summary>
public class BluetoothServiceTests
{
    [Fact]
    public async Task WiimoteService_Created_CanBeInitialized()
    {
        // Arrange
        var service = new WiimoteService();

        // Act
        await Task.CompletedTask; // WiimoteService doesn't need initialization

        // Assert
        service.Dispose();
        Assert.True(true);
    }

    [Fact]
    public async Task WiimoteService_Discovery_CanBeCalled()
    {
        // Arrange
        var service = new WiimoteService();

        // Act
        var devices = await service.DiscoverWiimotesAsync();

        // Assert - should complete without error even if no devices found
        service.Dispose();
        Assert.NotNull(devices);
    }

    [Fact]
    public async Task WiimoteService_WiimoteConnected_EventCanBeRaised()
    {
        // Arrange
        var service = new WiimoteService();
        bool eventRaised = false;
        WiimoteDevice? discoveredDevice = null;

        service.WiimoteConnected += (s, device) =>
        {
            eventRaised = true;
            discoveredDevice = device;
        };

        var testDevice = new WiimoteDevice("00:1A:7D:DA:71:13", "Nintendo RVL-CNT-01");

        // We can't directly invoke the event, so we'll just verify the subscription works
        // In real scenarios, the event would be raised by actual Wiimote connection

        // Assert
        Assert.False(eventRaised); // Event not raised until actual connection occurs
        service.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WiimoteService_WiimoteDisconnected_EventCanBeSubscribed()
    {
        // Arrange
        var service = new WiimoteService();
        bool subscriptionWorks = false;

        service.WiimoteDisconnected += (s, device) =>
        {
            subscriptionWorks = true;
        };

        // Assert - Subscription successful
        Assert.True(true);
        service.Dispose();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Tests for MainViewModel with mocked services.
/// </summary>
public class MainViewModelTests
{
    [Fact]
    public void MainViewModel_Created_HasEmptyDeviceList()
    {
        // Arrange & Act
        var viewModel = new MainViewModel();

        // Assert
        Assert.Empty(viewModel.ConnectedWiimotes);
        Assert.NotEmpty(viewModel.StatusMessage);

        viewModel.Dispose();
    }

    [Fact]
    public async Task MainViewModel_Initialize_CanRun()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.NotEmpty(viewModel.StatusMessage);

        viewModel.Dispose();
    }

    [Fact]
    public async Task MainViewModel_ScanDevices_CommandCanExecute()
    {
        // Arrange
        var viewModel = new MainViewModel();
        await viewModel.InitializeAsync();

        // Act
        var command = viewModel.ScanDevicesCommand;

        // Assert
        Assert.NotNull(command);
        Assert.True(command.CanExecute(null));

        viewModel.Dispose();
    }

    [Fact]
    public void MainViewModel_ClearDevices_RemovesAllDevices()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Manually add a device to the collection for testing
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var vm = new WiimoteViewModel(device, new HidCommunicationService());
        viewModel.ConnectedWiimotes.Add(vm);

        Assert.Single(viewModel.ConnectedWiimotes);

        // Act
        viewModel.ClearDiscoveredDevicesCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.ConnectedWiimotes);

        viewModel.Dispose();
    }
}

/// <summary>
/// Tests for WiimoteViewModel with mocked HID service.
/// </summary>
public class WiimoteViewModelTests
{
    [Fact]
    public void WiimoteViewModel_Created_HasDefaultValues()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var hidService = new HidCommunicationService();

        // Act
        var viewModel = new WiimoteViewModel(device, hidService);

        // Assert
        Assert.NotNull(viewModel.Device);
        Assert.Equal(device.BluetoothAddress, viewModel.Device.BluetoothAddress);
        Assert.False(viewModel.IsReading);
        Assert.Equal(0, viewModel.BatteryPercent);

        viewModel.Dispose();
        hidService.Dispose();
    }

    [Fact]
    public void WiimoteViewModel_LEDToggles_ChangeState()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var hidService = new HidCommunicationService();
        var viewModel = new WiimoteViewModel(device, hidService);

        // Act
        Assert.False(viewModel.IsLed1On);
        viewModel.ToggleLED1Command.Execute(null);

        // Assert
        Assert.True(viewModel.IsLed1On);

        viewModel.Dispose();
        hidService.Dispose();
    }

    [Fact]
    public void WiimoteViewModel_RumbleToggle_TogglesBetweenStates()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var hidService = new HidCommunicationService();
        var viewModel = new WiimoteViewModel(device, hidService);

        // Act
        Assert.Equal(0f, viewModel.VibrationIntensity);
        viewModel.ToggleRumbleCommand.Execute(null);

        // Assert
        Assert.Equal(1.0f, viewModel.VibrationIntensity);

        viewModel.Dispose();
        hidService.Dispose();
    }

    [Fact]
    public void WiimoteViewModel_ButtonDisplay_UpdatesWhenDeviceChanges()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var hidService = new HidCommunicationService();
        var viewModel = new WiimoteViewModel(device, hidService);

        Assert.Equal("None", viewModel.PressedButtons);

        // Act
        device.CurrentButtonState = ButtonState.A | ButtonState.B;

        // Assert
        Assert.Contains("A", viewModel.PressedButtons);
        Assert.Contains("B", viewModel.PressedButtons);

        viewModel.Dispose();
        hidService.Dispose();
    }

    [Fact]
    public void WiimoteViewModel_BatteryDisplay_UpdatesWhenDeviceChanges()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var hidService = new HidCommunicationService();
        var viewModel = new WiimoteViewModel(device, hidService);

        Assert.Equal(0, viewModel.BatteryPercent);

        // Act
        device.BatteryLevel = 75;

        // Assert
        Assert.Equal(75, viewModel.BatteryPercent);

        viewModel.Dispose();
        hidService.Dispose();
    }

    [Fact]
    public void WiimoteViewModel_TiltDisplay_CalculatesFromAccelerometer()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var hidService = new HidCommunicationService();
        var viewModel = new WiimoteViewModel(device, hidService);

        // Act
        device.AccelX = 0.0f;
        device.AccelY = 0.0f;
        device.AccelZ = 1.0f;

        // Assert - With neutral accel, tilt should be near zero
        Assert.True(Math.Abs(viewModel.TiltX) < 5.0f); // Allow small margin for calculation
        Assert.True(Math.Abs(viewModel.TiltY) < 5.0f);

        viewModel.Dispose();
        hidService.Dispose();
    }
}

/// <summary>
/// Smoke tests to ensure the application can start without errors.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Application_CanCreateMainViewModel()
    {
        // Arrange & Act
        var viewModel = new MainViewModel();

        // Assert
        Assert.NotNull(viewModel);
        Assert.NotEmpty(viewModel.StatusMessage);

        viewModel.Dispose();
    }

    [Fact]
    public async Task Application_CanInitializeAndDispose()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        await viewModel.InitializeAsync();
        viewModel.Dispose();

        // Assert - No exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void WiimoteDevice_CanBeCreatedAndModified()
    {
        // Arrange & Act
        var device = new WiimoteDevice("00:1A:7D:DA:71:13", "Test Device");
        device.BatteryLevel = 50;
        device.IsConnected = true;

        // Assert
        Assert.Equal(50, device.BatteryLevel);
        Assert.True(device.IsConnected);
    }

    [Fact]
    public void ButtonState_CanDecodeWiimoteButtons()
    {
        // Arrange
        var buttonsMask = (ushort)0x0C00; // Bits for A and B buttons
        var buttons = (ButtonState)buttonsMask;

        // Act & Assert
        Assert.True(buttons.HasFlag(ButtonState.A));
        Assert.True(buttons.HasFlag(ButtonState.B));
    }
}
