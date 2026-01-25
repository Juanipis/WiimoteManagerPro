# Current Button Mapping Behavior

**Date**: 2026-01-25  
**Status**: ⚠️ PARTIALLY WORKING - Button detection works but mapping is incorrect

---

## 🎮 ACTUAL BEHAVIOR (What User Sees)

When pressing Wiimote buttons, the app shows **incorrect** button names:

| Physical Button Pressed | What UI Shows | Expected |
|------------------------|---------------|----------|
| **A** | ↑ (Up arrow / DPadUp) | A |
| **B** | ↓ (Down arrow / DPadDown) | B |
| **1** | → (Right arrow / DPadRight) | One (1) |
| **2** | ← (Left arrow / DPadLeft) | Two (2) |
| **-** | Plus (+) | Minus (-) |
| **+** | Minus (-) | Plus (+) |
| **Home** | Not recognized | Home |

---

## 🔍 TECHNICAL ANALYSIS

### What We Know:
1. ✅ **HID Communication Works** - All buttons detected correctly at hardware level
2. ✅ **Bit Values Correct** - 0x0800 detected when pressing A, etc.
3. ❌ **Mapping Incorrect** - ButtonState enum values don't match what UI shows

### Current ButtonState Enum:
```csharp
// From Models/ButtonState.cs (latest version)
DPadLeft = 0x0001,   // Byte 2 bit 0
DPadRight = 0x0002,  // Byte 2 bit 1
DPadDown = 0x0004,   // Byte 2 bit 2
DPadUp = 0x0008,     // Byte 2 bit 3
Plus = 0x0010,       // Byte 2 bit 4

Two = 0x0100,   // Byte 1 bit 0
One = 0x0200,   // Byte 1 bit 1
B = 0x0400,     // Byte 1 bit 2
A = 0x0800,     // Byte 1 bit 3
Minus = 0x1000, // Byte 1 bit 4
Home = 0x8000,  // Byte 1 bit 7 (detected before masking)
```

### The Mystery:
- User presses **A** → bit `0x0800` is set ✅
- ButtonState enum says `A = 0x0800` ✅
- But UI shows **"DPadUp"** ❌

**Hypothesis**: Either:
1. The UI is using a different mapping/translation layer
2. The enum names and actual Wiimote protocol don't match (Wiibrew documentation discrepancy?)
3. There's a display layer converting button states incorrectly

---

## 📊 WIIBREW PROTOCOL VS OUR IMPLEMENTATION

### According to Wiibrew (what we implemented):
```
Byte 2 (after 0x1F mask):
  Bit 0 (0x0001) = DPad Left
  Bit 1 (0x0002) = DPad Right
  Bit 2 (0x0004) = DPad Down
  Bit 3 (0x0008) = DPad Up
  Bit 4 (0x0010) = Plus

Byte 1 (after 0x1F mask):
  Bit 0 (0x0100) = Two
  Bit 1 (0x0200) = One
  Bit 2 (0x0400) = B
  Bit 3 (0x0800) = A
  Bit 4 (0x1000) = Minus
  Bit 7 (0x8000) = Home (before masking)
```

### What User Actually Experiences:
```
0x0001 → Shows "Two (2)" when pressed
0x0002 → Shows "One (1)" when pressed
0x0004 → Shows "B" when pressed
0x0008 → Shows "A" when pressed
0x0010 → Shows "Minus (-)" when pressed

0x0100 → Unknown (not tested)
0x0200 → Unknown (not tested)
0x0400 → Shows "Down arrow" when pressed
0x0800 → Shows "Up arrow" when pressed
0x1000 → Shows "Plus (+)" when pressed
0x8000 → Home not detected
```

**PATTERN DETECTED**: The bits are shifted! Byte 1 and Byte 2 might be swapped or there's an endianness issue in the UI layer.

---

## 🚧 WHAT WORKS

1. ✅ **HID Connection** - Stable, no disconnects
2. ✅ **Button Detection** - All button presses detected at bit level
3. ✅ **LED Control** - All 4 LEDs working perfectly
4. ✅ **Accelerometer** - 10-bit precision, smooth updates
5. ✅ **No Phantom Buttons** - Fixed with 0x1F masks
6. ✅ **No Crashes** - Stable read loop
7. ✅ **File Logging** - Debug log working (Desktop/wiimote_debug.log)

---

## 🐛 WHAT NEEDS FIXING

### HIGH PRIORITY:
1. **Button Name Mapping** 
   - Physical buttons don't match displayed names
   - Possibly byte order issue or UI display bug
   - Need to investigate WiimoteViewModel or UI bindings
   
2. **Home Button Detection**
   - Currently not recognized despite special handling
   - May need different report mode or raw byte check

### LOW PRIORITY:
3. **Battery Level**
   - Always shows 0%
   - Byte 6 consistently reads 0x00
   - May need Status Report (0x20) instead of Data Report (0x31)

---

## 📝 NEXT STEPS (Tomorrow)

1. **Debug Button Display**
   - Check WiimoteViewModel.cs - how ButtonState is converted to display strings
   - Check if there's any translation/mapping layer in UI
   - Add debug logging to see button state → UI string conversion
   - Consider testing with raw hex values displayed directly

2. **Investigate Wiibrew Documentation**
   - Double-check if Wiibrew protocol matches real Wiimote behavior
   - Test with other Wiimote libraries (reference implementation)
   - Check if byte order is device-specific (RVL-CNT-01 vs RVL-CNT-01-TR)

3. **Fix Home Button**
   - Verify bit 7 of byte 1 actually has Home data
   - Test with different report modes
   - Check if Home requires special command to enable

4. **Battery Level**
   - Request Status Report (0x15) separately
   - Parse response (0x20) for battery data
   - Update UI when status report received

---

## 💾 SESSION STATE

**Last Working Commit**: a371708
- ✅ Button parsing with correct bitmasks (0x1F)
- ✅ Accelerometer working
- ✅ No phantom buttons
- ✅ Stable connection

**Current Changes** (in this commit):
- Updated ButtonState.cs with Wiibrew protocol values
- Added Home button detection logic (before masking)
- Documented current behavior in this file

**User Status**: Tired, continuing tomorrow

---

## 🔬 DEBUG DATA NEEDED (Tomorrow)

1. Raw log file showing button presses with hex values
2. Screenshot of UI showing incorrect button names
3. WiimoteViewModel button state handling code review
4. Check if UI uses ButtonState enum directly or has translation layer

---

## 📚 REFERENCES

- **Wiibrew Core Buttons**: https://wiibrew.org/wiki/Wiimote#Buttons_2
- **Checkpoint 008**: Session checkpoints/008-button-bitmask-breakthrough.md
- **Debug Log**: Desktop/wiimote_debug.log (runtime generated)
- **FINAL_SOLUTION.md**: Root directory (complete technical docs)

---

**Status**: Ready to debug button mapping tomorrow. Core functionality proven working.
