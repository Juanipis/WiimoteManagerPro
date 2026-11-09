# 🔋 Investigation: Home Button & Battery Issues

## Date: 2026-01-25 17:47

---

## ❌ Problem 1: Home Button Not Detected

### Current Status
- **Expected**: Bit 7 of byte 1 (0x8000) should detect Home button
- **Reality**: Never fires, even with special detection code
- **Current Implementation**: Checking bit 7 BEFORE masking with 0x1F

### Findings from Wiibrew

From https://wiibrew.org/wiki/Wiimote#Buttons:

> **Core Buttons (BB BB)**: The first two bytes of ALL input reports, except 0x3d, contain the Core Buttons.

Report 0x31 format:
```
(a1) 31 BB BB AA AA AA
```
Where:
- BB BB = Core Buttons (2 bytes)
- AA AA AA = Accelerometer (3 bytes, with LSBs embedded in button bytes)

### Button Bitmask (from Wiibrew)
According to the specification:
```
Byte 1 (High):
  Bit 0 (0x0100) = Two
  Bit 1 (0x0200) = One
  Bit 2 (0x0400) = B
  Bit 3 (0x0800) = A
  Bit 4 (0x1000) = Minus
  Bit 7 (0x8000) = Home

Byte 2 (Low):
  Bit 0 (0x0001) = DPad Left
  Bit 1 (0x0002) = DPad Right
  Bit 2 (0x0004) = DPad Down
  Bit 3 (0x0008) = DPad Up
  Bit 4 (0x0010) = Plus
```

### Hypothesis: Accelerometer LSBs Overwrite Home Bit?

From Report 0x31 documentation:
> "Modes which include Accelerometer data also embed part of it in the unused Buttons bits."

In Report 0x31:
- **Bits 5-7 of both button bytes** are used for Accelerometer LSBs
- Bit 7 of byte 1 might contain **Y-axis LSB** instead of Home button!

### Accelerometer LSB Embedding (Report 0x31)
```
Button Byte 1 bits:
  0-4: Buttons (Two, One, B, A, Minus)
  5-6: Y-axis LSBs
  7: Unknown (possibly Y-axis or unused)

Button Byte 2 bits:
  0-4: Buttons (DPad Left/Right/Down/Up, Plus)
  5: Z-axis LSB (bit 0)
  6-7: X-axis LSBs
```

### Possible Solutions

#### Solution A: Request Report 0x30 for Home Button
Report 0x30 = Core Buttons ONLY (no accelerometer)
```
(a1) 30 BB BB
```
- No accelerometer data = no LSB embedding
- Home button bit 7 should be clean

**Trade-off**: Lose accelerometer data

#### Solution B: Request Report 0x20 (Status) Separately
Status report includes button data + battery:
```
(a1) 20 BB BB LF 00 00 VV
```
- BB BB = Core buttons (clean, no accel LSBs)
- VV = Battery level
- LF = LED + flags

**To request**: Send Output Report 0x15
```
(a2) 15 00
```

#### Solution C: Accept Home Button Limitation
- Many applications don't use Home button
- Can be optional feature
- Focus on other functionality

---

## ❌ Problem 2: Battery Always 0%

### Current Status
- **Current Code**: Reading byte 6 from Report 0x31
- **Reality**: Byte 6 is always 0x00
- **Root Cause**: **Report 0x31 DOES NOT include battery data**

### Correct Implementation (from Wiibrew)

#### Report 0x20: Status Information
```
(a1) 20 BB BB LF 00 00 VV
```
Where:
- BB BB = Core Buttons
- L = LED state
- F = Flags (battery low, extension connected, etc.)
- VV = **Battery level (0-255)**

#### How to Request Status Report
Send Output Report 0x15:
```
(a2) 15 00
```

#### Battery Level Calculation
```csharp
byte rawBattery = statusReport[6]; // VV from report 0x20
int batteryPercent = (rawBattery * 100) / 255;
```

### Implementation Plan

1. **Add method to request Status Report**
   ```csharp
   public bool RequestStatus(string deviceKey)
   {
       byte[] report = new byte[22];
       report[0] = 0x15; // Status Request
       report[1] = 0x00; // No rumble
       return SendOutputReport(deviceKey, report);
   }
   ```

2. **Handle Report 0x20 in ProcessInputReport**
   ```csharp
   if (reportId == 0x20 && length >= 7)
   {
       // Parse status report
       byte flags = data[3];
       byte battery = data[6];
       
       connection.Model.BatteryLevel = (battery * 100) / 255;
       connection.Model.IsExtensionConnected = (flags & 0x02) != 0;
       // etc.
   }
   ```

3. **Periodic Status Requests**
   - Request status every 30-60 seconds
   - Or on-demand via UI button
   - Update battery display when received

---

## 🎯 Recommended Solution

### For Home Button
**Option B**: Implement Report 0x20 (Status) handling
- Solves BOTH problems (Home + Battery)
- Request periodically (e.g., every 30 seconds)
- Parse button data from 0x20 for Home detection
- Continue using 0x31 for main input loop (accel + buttons except Home)

### For Battery
**Implement Report 0x20 handling** (as above)
- Remove battery reading from Report 0x31
- Only read battery from Report 0x20
- Request status on connect + periodically

---

## 📝 Implementation Checklist

- [ ] Add `RequestStatus()` method to WiimoteService
- [ ] Add Report 0x20 parser in `ProcessInputReport()`
- [ ] Remove battery reading from Report 0x31 parser
- [ ] Add periodic status request (Timer or background task)
- [ ] Test Home button detection from Report 0x20
- [ ] Test battery reading from Report 0x20
- [ ] Update UI to show battery updates
- [ ] Add manual "Refresh Status" button (optional)

---

## 🔗 References

- Wiibrew Core Buttons: https://wiibrew.org/wiki/Wiimote#Buttons
- Wiibrew Report 0x20: https://wiibrew.org/wiki/Wiimote#0x20:_Status
- Wiibrew Report 0x31: https://wiibrew.org/wiki/Wiimote#0x31:_Core_Buttons_and_Accelerometer
- Wiibrew Data Reporting: https://wiibrew.org/wiki/Wiimote#Data_Reporting

---

**Created**: 2026-01-25 17:47  
**Status**: Investigation complete, ready to implement  
**Priority**: HIGH (battery is essential functionality)
