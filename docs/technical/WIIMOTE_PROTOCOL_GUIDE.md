# Wiimote Bluetooth Communication Protocol Guide

## Overview

Communicating with a Nintendo Wiimote via Bluetooth involves HID (Human Interface Device) reports. The Wiimote does not use standard HID descriptors effectively, requiring specific byte-level manipulation of Input (from Wiimote) and Output (to Wiimote) reports.

## Connection Basics

- **Vendor ID:** `0x057E` (Nintendo)
- **Product ID:** `0x0306` (Original) or `0x0330` (MotionPlus inside)
- **Protocol:** Bluetooth HID
- **Output Report Size:** MUST be exactly **22 bytes** for Bluetooth communication.

## Critical Challenges & Solutions

### 1. The Home Button & Accelerometer Conflict
The Wiimote has multiple Data Reporting Modes. The most common are:
- **Mode 0x30 (Core Buttons):** Returns all buttons cleanly. No accelerometer.
- **Mode 0x31 (Core Buttons + Accel):** Returns buttons and accelerometer.
  - **PROBLEM:** In Mode 0x31, the accelerometer LSBs (Least Significant Bits) overwrite the bit used for the **Home Button**.
  - **RESULT:** You cannot read the Home button reliably in Mode 0x31.

### 2. The Solution: Hybrid Polling
To get **BOTH** fast accelerometer data AND the Home button without using complex Interleaved modes:
1. Set Data Reporting Mode to **0x31** (Buttons + Accel).
2. Use a background timer to request **Status Reports (0x20)** every ~200ms.
3. **Merge the data:**
   - Use 0x31 for A, B, 1, 2, +/-, D-Pad, and Accelerometer.
   - Use 0x20 (Status) to read the Home button and Battery level.

## Report Structures

### Output Report 0x12 (Set Data Reporting Mode)
Send this to tell the Wiimote what data to send back.
```
Byte 0: 0x12 (Report ID)
Byte 1: 0x00 (Rumble off) or 0x01 (Rumble on)
Byte 2: Mode ID (e.g., 0x30, 0x31, 0x37)
```

### Output Report 0x15 (Request Status)
Send this to ask for battery and clean button data.
```
Byte 0: 0x15 (Report ID)
Byte 1: 0x00 (Rumble off) or 0x01 (Rumble on)
```

### Input Report 0x20 (Status Report)
Received in response to 0x15.
```
Byte 0: 0x20 (Report ID)
Byte 1: Buttons High Byte (Bit 7 = HOME BUTTON 0x0080)
Byte 2: Buttons Low Byte
Byte 3: Flags (LEDs, Extension connected, etc.)
...
Byte 6: Battery Level (0-255)
```

### Input Report 0x31 (Buttons + Accelerometer)
Received continuously if Mode 0x31 is set.
```
Byte 0: 0x31 (Report ID)
Byte 1: Buttons High (Mask 0x1F to remove Accel bits)
Byte 2: Buttons Low (Mask 0x1F to remove Accel bits)
Byte 3: Accel X (High 8 bits)
Byte 4: Accel Y (High 8 bits)
Byte 5: Accel Z (High 8 bits)
```
**NOTE:** In this mode, `Byte 1` & `0x80` is **NOT** the Home button; it's part of the Z-axis accelerometer.

## Button Mapping (Corrected)

Hardware testing confirmed the following bitmask mapping (Big Endian):

**Byte 1 (High)** | **Byte 2 (Low)**
--- | ---
`0x0800` D-Pad Up | `0x0001` Two
`0x0400` D-Pad Down | `0x0002` One
`0x0100` D-Pad Left | `0x0004` B (Trigger)
`0x0200` D-Pad Right | `0x0008` A
`0x1000` Plus | `0x0010` Minus
(Home overwriten) | `0x0080` Home (Only in Report 0x20/0x30)

## Implementation Strategy (C#)

```csharp
// 1. Initialize
SendOutputReport(0x12, 0x00, 0x31); // Set Mode 0x31

// 2. Start Polling Timer (200ms)
Timer_Tick() {
    SendOutputReport(0x15, 0x00); // Request Status
}

// 3. Process Input
void OnInputReceived(byte[] data) {
    if (data[0] == 0x31) {
        // Parse Accel & Standard Buttons
        // IGNORE Home bit here
    }
    else if (data[0] == 0x20) {
        // Parse Battery & Home Button
        bool homePressed = (data[2] & 0x80) != 0;
        // Update model with Home state
    }
}
```

## Battery Calculation
Battery is returned as a raw byte (0-200 approx) in Report 0x20, Byte 6.
`Percentage = (RawByte * 100) / 255` (Rough approximation)
