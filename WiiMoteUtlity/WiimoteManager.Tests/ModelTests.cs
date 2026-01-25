using WiimoteManager.Models;

namespace WiimoteManager.Tests;

/// <summary>
/// Unit tests for ButtonState enum and bitmask operations.
/// </summary>
public class ButtonStateTests
{
    [Fact]
    public void ButtonState_None_HasNoButtons()
    {
        // Arrange & Act
        var state = ButtonState.None;

        // Assert
        Assert.Equal(0, (int)state);
        Assert.False(state.HasFlag(ButtonState.A));
        Assert.False(state.HasFlag(ButtonState.B));
    }

    [Fact]
    public void ButtonState_SingleButton_IsSet()
    {
        // Arrange
        var state = ButtonState.A;

        // Act & Assert
        Assert.True(state.HasFlag(ButtonState.A));
        Assert.False(state.HasFlag(ButtonState.B));
    }

    [Fact]
    public void ButtonState_MultipleButtons_AreCombined()
    {
        // Arrange
        var state = ButtonState.A | ButtonState.B | ButtonState.One;

        // Act & Assert
        Assert.True(state.HasFlag(ButtonState.A));
        Assert.True(state.HasFlag(ButtonState.B));
        Assert.True(state.HasFlag(ButtonState.One));
        Assert.False(state.HasFlag(ButtonState.Home));
    }

    [Fact]
    public void ButtonState_DPadDirections_AreDistinct()
    {
        // Arrange & Act
        var up = ButtonState.DPadUp;
        var down = ButtonState.DPadDown;
        var left = ButtonState.DPadLeft;
        var right = ButtonState.DPadRight;

        // Assert
        Assert.NotEqual(up, down);
        Assert.NotEqual(left, right);
        Assert.NotEqual(up, left);
    }

    [Fact]
    public void ButtonState_HomePlusMinusButtons_AreDefined()
    {
        // Arrange & Act & Assert
        Assert.NotEqual(0, (int)ButtonState.Home);
        Assert.NotEqual(0, (int)ButtonState.Plus);
        Assert.NotEqual(0, (int)ButtonState.Minus);
    }

    [Theory]
    [InlineData(ButtonState.A)]
    [InlineData(ButtonState.B)]
    [InlineData(ButtonState.One)]
    [InlineData(ButtonState.Two)]
    public void ButtonState_ValidButtons_CanBeChecked(ButtonState button)
    {
        // Arrange
        var state = button;

        // Act & Assert
        Assert.True(state.HasFlag(button));
        Assert.NotEqual(0, (int)button);
    }
}

/// <summary>
/// Unit tests for WiimoteDevice model.
/// </summary>
public class WiimoteDeviceTests
{
    [Fact]
    public void WiimoteDevice_CreatedWithAddress_HasCorrectProperties()
    {
        // Arrange
        var address = "00:1A:7D:DA:71:13";
        var name = "Nintendo RVL-CNT-01";

        // Act
        var device = new WiimoteDevice(address, name);

        // Assert
        Assert.Equal(address, device.BluetoothAddress);
        Assert.Equal(name, device.DeviceName);
        Assert.False(device.IsConnected);
        Assert.False(device.IsPaired);
        Assert.Equal(0, device.BatteryLevel);
    }

    [Fact]
    public void WiimoteDevice_DisplayName_ShowsAddressWhenNoAlias()
    {
        // Arrange
        var address = "00:1A:7D:DA:71:13";
        var device = new WiimoteDevice(address);

        // Act
        var displayName = device.DisplayName;

        // Assert
        Assert.Contains(address, displayName);
    }

    [Fact]
    public void WiimoteDevice_DisplayName_ShowsAliasWhenSet()
    {
        // Arrange
        var address = "00:1A:7D:DA:71:13";
        var alias = "My Wiimote";
        var device = new WiimoteDevice(address)
        {
            UserAlias = alias
        };

        // Act
        var displayName = device.DisplayName;

        // Assert
        Assert.Contains(alias, displayName);
        Assert.DoesNotContain(device.DeviceName, displayName);
    }

    [Theory]
    [InlineData(100, "Good")]
    [InlineData(75, "Good")]
    [InlineData(50, "Fair")]
    [InlineData(25, "Low")]
    [InlineData(10, "Critical")]
    [InlineData(0, "Critical")]
    public void WiimoteDevice_BatteryStatus_IsCorrect(int batteryLevel, string expectedStatus)
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13")
        {
            BatteryLevel = batteryLevel
        };

        // Act
        var status = device.BatteryStatus;

        // Assert
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public void WiimoteDevice_StatusText_ReflectsConnectionState()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");

        // Act & Assert
        device.IsConnected = false;
        device.IsPaired = false;
        Assert.Equal("Not Paired", device.StatusText);

        device.IsPaired = true;
        Assert.Equal("Disconnected", device.StatusText);

        device.IsConnected = true;
        Assert.Equal("Connected", device.StatusText);
    }

    [Fact]
    public void WiimoteDevice_ResetSensorData_ClearsAllSensorValues()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13")
        {
            CurrentButtonState = ButtonState.A | ButtonState.B,
            AccelX = 0.5f,
            AccelY = 0.3f,
            AccelZ = 0.9f
        };

        // Act
        device.ResetSensorData();

        // Assert
        Assert.Equal(ButtonState.None, device.CurrentButtonState);
        Assert.Equal(0f, device.AccelX);
        Assert.Equal(0f, device.AccelY);
        Assert.Equal(1f, device.AccelZ);
        Assert.Null(device.NunchukState);
        Assert.Null(device.ClassicControllerState);
    }

    [Fact]
    public void WiimoteDevice_UpdateLastCommunication_UpdatesTimestamp()
    {
        // Arrange
        var device = new WiimoteDevice("00:1A:7D:DA:71:13");
        var originalTime = device.LastCommunication;
        System.Threading.Thread.Sleep(100);

        // Act
        device.UpdateLastCommunication();

        // Assert
        Assert.True(device.LastCommunication > originalTime);
    }
}

/// <summary>
/// Unit tests for WiimoteReports constants.
/// </summary>
public class WiimoteReportsTests
{
    [Fact]
    public void WiimoteReports_LEDBits_AreCorrect()
    {
        // Arrange & Act & Assert
        Assert.Equal(0x10, WiimoteReports.LED1);
        Assert.Equal(0x20, WiimoteReports.LED2);
        Assert.Equal(0x40, WiimoteReports.LED3);
        Assert.Equal(0x80, WiimoteReports.LED4);
    }

    [Fact]
    public void WiimoteReports_LEDCombinations_Work()
    {
        // Arrange
        byte allLEDs = (byte)(WiimoteReports.LED1 | WiimoteReports.LED2 | WiimoteReports.LED3 | WiimoteReports.LED4);

        // Act & Assert
        Assert.Equal(0xF0, allLEDs);
    }

    [Fact]
    public void WiimoteReports_RumbleBit_IsCorrect()
    {
        // Arrange & Act & Assert
        Assert.Equal(0x01, WiimoteReports.RumbleBit);
    }

    [Fact]
    public void WiimoteReports_ReportIDs_AreValid()
    {
        // Arrange & Act & Assert
        Assert.Equal(0x11, WiimoteReports.ReportSetLEDRumble);
        Assert.Equal(0x12, WiimoteReports.ReportSetDataMode);
        Assert.Equal(0x15, WiimoteReports.ReportStatusRequest);
        Assert.Equal(0x20, WiimoteReports.ReportStatus);
        Assert.Equal(0x30, WiimoteReports.ReportCoreButtons);
        Assert.Equal(0x31, WiimoteReports.ReportCoreAccel);
    }

    [Fact]
    public void WiimoteReports_ExtensionIdentifiers_AreDefined()
    {
        // Arrange & Act & Assert
        Assert.NotNull(WiimoteReports.NoExtension);
        Assert.NotNull(WiimoteReports.NunchukIdentifier);
        Assert.NotNull(WiimoteReports.ClassicIdentifier);
        Assert.Equal(6, WiimoteReports.NunchukIdentifier.Length);
        Assert.Equal(6, WiimoteReports.ClassicIdentifier.Length);
    }

    [Fact]
    public void WiimoteReports_VIDandPID_AreValid()
    {
        // Arrange & Act & Assert
        Assert.Equal(0x057E, WiimoteReports.WiimoteVendorID);
        Assert.Equal(0x0306, WiimoteReports.WiimoteProductID);
    }
}

/// <summary>
/// Unit tests for DataReportingMode enum.
/// </summary>
public class DataReportingModeTests
{
    [Theory]
    [InlineData(DataReportingMode.ButtonsOnly, 0x30)]
    [InlineData(DataReportingMode.ButtonsAccelerometer, 0x31)]
    [InlineData(DataReportingMode.ButtonsExtension, 0x32)]
    [InlineData(DataReportingMode.ButtonsAccelerometerExtension, 0x35)]
    public void DataReportingMode_ValuesAreCorrect(DataReportingMode mode, byte expectedValue)
    {
        // Arrange & Act & Assert
        Assert.Equal(expectedValue, (byte)mode);
    }
}

/// <summary>
/// Unit tests for ExtensionType enum.
/// </summary>
public class ExtensionTypeTests
{
    [Fact]
    public void ExtensionType_None_HasValue0()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)ExtensionType.None);
    }

    [Fact]
    public void ExtensionType_Nunchuk_HasValue1()
    {
        // Arrange & Act & Assert
        Assert.Equal(1, (int)ExtensionType.Nunchuk);
    }

    [Fact]
    public void ExtensionType_AllValuesAreDefined()
    {
        // Arrange & Act & Assert
        Assert.NotEqual(0, (int)ExtensionType.Nunchuk);
        Assert.NotEqual(0, (int)ExtensionType.ClassicController);
        Assert.NotEqual(0, (int)ExtensionType.Guitar);
        Assert.NotEqual(0, (int)ExtensionType.MotionPlus);
    }
}

/// <summary>
/// Unit tests for NunchukState model.
/// </summary>
public class NunchukStateTests
{
    [Fact]
    public void NunchukState_Created_HasDefaultValues()
    {
        // Arrange & Act
        var state = new NunchukState();

        // Assert
        Assert.Equal(0f, state.StickX);
        Assert.Equal(0f, state.StickY);
        Assert.Equal(0f, state.AccelX);
        Assert.Equal(0f, state.AccelY);
        Assert.Equal(1f, state.AccelZ);
        Assert.Equal(NunchukButtons.None, state.ButtonState);
    }

    [Fact]
    public void NunchukState_ButtonChecks_Work()
    {
        // Arrange
        var state = new NunchukState
        {
            ButtonState = NunchukButtons.C | NunchukButtons.Z
        };

        // Act & Assert
        Assert.True(state.IsButtonC);
        Assert.True(state.IsButtonZ);
    }

    [Fact]
    public void NunchukState_StickPositions_CanBeSet()
    {
        // Arrange
        var state = new NunchukState
        {
            StickX = 0.5f,
            StickY = -0.3f
        };

        // Act & Assert
        Assert.Equal(0.5f, state.StickX);
        Assert.Equal(-0.3f, state.StickY);
    }
}
