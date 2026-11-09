# 🔧 Wiimote Button Test & Diagnostic System

## Overview
Sistema integrado de testing y diagnóstico para identificar y corregir problemas de mapeo de botones y sensores del Wiimote.

---

## 🎯 Features

### 1. **Diagnostic Logging**
- ✅ **Logging automático** de todas las presiones de botones
- ✅ **Raw HEX values** + nombres de botones detectados
- ✅ **Timestamps precisos** (milisegundos)
- ✅ **Datos de acelerómetro** (10-bit raw + normalized)
- ✅ **Lecturas de batería** (raw byte + percentage)

### 2. **Button Test Window**
- ✅ **Test sistemático** de todos los botones
- ✅ **Feedback visual** en tiempo real
- ✅ **Resultados PASS/FAIL** automáticos
- ✅ **Exportación a CSV** para análisis
- ✅ **Logs persistentes** en carpeta interna

### 3. **Integration**
- ✅ **Botón "Test"** en cada WiimoteCard
- ✅ **Múltiples instancias** (test varios Wiimotes simultáneamente)
- ✅ **No bloquea UI principal**

---

## 📁 Log File Locations

```
Project Structure:
UCHWiiRemoteMod/
└── WiiMoteUtlity/
    └── WiimoteManager/
        ├── bin/Debug/.../Logs/           ← Session logs aquí
        │   ├── diagnostic_session_*.log
        │   ├── button_test_export_*.csv
        │   └── button_test_summary_*.txt
        └── ...

Desktop/
└── wiimote_debug.log                     ← Runtime log (legacy)
```

---

## 🚀 How to Use

### Step 1: Connect Wiimote
1. Abre la aplicación **WiimoteManager**
2. Click en **"Connect Wiimotes"**
3. Espera a que aparezca la tarjeta del Wiimote

### Step 2: Open Button Test
1. En la **WiimoteCard**, click en botón **"🔧 Test"**
2. Se abrirá la ventana **"Wiimote Button Test Diagnostic"**

### Step 3: Run Automated Test
1. Click en **"▶ Start Test"**
2. Sigue las instrucciones en pantalla
3. Presiona cada botón cuando se solicite:
   - A, B, 1, 2, +, -, Home
   - ↑, ↓, ←, →

### Step 4: Review Results
- **Verde (✓ PASS)**: Botón correctamente mapeado
- **Rojo (✗ FAIL)**: Mapeo incorrecto detectado

### Step 5: Export Data
1. Click en **"📁 Open Logs"** para abrir carpeta de logs
2. Archivos disponibles:
   - `diagnostic_session_*.log` - Log completo de sesión
   - `button_test_export_*.csv` - Datos en formato CSV
   - `button_test_summary_*.txt` - Resumen de pruebas

---

## 📊 Log File Formats

### Diagnostic Session Log
```
========================================
WIIMOTE DIAGNOSTIC SESSION
Session Start: 2026-01-25 17:30:00
OS: Microsoft Windows NT 10.0.22631.0
.NET Version: 8.0.11
========================================

[17:30:15.123] BUTTON TEST
  Expected: A
  Raw Hex:  0x0800
  Detected: DPadUp
  Match:    ✗ INCORRECT

[17:30:16.456] BATTERY
  Raw Byte: 0x00 (0)
  Percent:  0%

[17:30:17.789] ACCELEROMETER
  10-bit:    X=512 Y=520 Z=600
  Normalized: X=0.000 Y=0.016 Z=0.172
```

### CSV Export
```csv
Timestamp,Expected,RawHex,ActualButtons,IsCorrect
2026-01-25 17:30:15.123,A,0x0800,"DPadUp",False
2026-01-25 17:30:16.456,B,0x0400,"DPadDown",False
2026-01-25 17:30:17.789,1,0x0200,"DPadRight",False
```

---

## 🔍 Troubleshooting Current Issues

### Issue 1: Incorrect Button Mapping ❌
**Síntoma**: Presionar A muestra "DPadUp", B muestra "DPadDown", etc.

**Diagnóstico**:
| Pressed | Expected | Raw Hex | Detected | Status |
|---------|----------|---------|----------|--------|
| A       | A        | 0x0800  | DPadUp   | ❌ FAIL |
| B       | B        | 0x0400  | DPadDown | ❌ FAIL |
| 1       | 1        | 0x0200  | DPadRight| ❌ FAIL |
| 2       | 2        | 0x0100  | DPadLeft | ❌ FAIL |

**Solución pendiente**: Corregir `ButtonState` enum en `Models/ButtonState.cs`

### Issue 2: Battery Always 0% 🔋
**Síntoma**: `BatteryLevel` siempre muestra 0%, byte 6 siempre 0x00

**Posible causa**: Report 0x31 no incluye batería, necesita Status Report (0x20)

**Solución pendiente**: Implementar solicitud de Status Report

---

## 🛠️ Technical Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   WiimoteService                         │
│  (HID Read Loop - Real-time data processing)            │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ Raw button data (ushort)
                     ↓
┌─────────────────────────────────────────────────────────┐
│                 DiagnosticLogger                         │
│  - LogButtonPress(expected, rawHex, actualState)        │
│  - LogAccelerometer(x, y, z)                            │
│  - LogBatteryReading(rawByte, percent)                  │
│  - GenerateButtonTestSummary()                          │
│  - ExportToCSV()                                        │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ Button events
                     ↓
┌─────────────────────────────────────────────────────────┐
│              ButtonTestViewModel                         │
│  - Orchestrates test sequence                           │
│  - Updates UI in real-time                              │
│  - Calculates PASS/FAIL results                         │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ UI bindings
                     ↓
┌─────────────────────────────────────────────────────────┐
│               ButtonTestWindow                           │
│  - User interface for testing                           │
│  - DataGrid with results                                │
│  - Control buttons (Start/Stop/Open Logs)               │
└─────────────────────────────────────────────────────────┘
```

---

## 📝 Code References

### Key Files
- **DiagnosticLogger**: `Services/DiagnosticLogger.cs`
- **Button Test VM**: `ViewModels/ButtonTestViewModel.cs`
- **Button Test UI**: `Views/ButtonTestWindow.xaml`
- **Integration**: `ViewModels/WiimoteViewModel.cs` (OpenButtonTestCommand)
- **Button State**: `Models/ButtonState.cs` (enum definitions)

### Key Methods
```csharp
// WiimoteService.cs
private void ProcessInputReport(string deviceKey, byte[] data, int length)
{
    // Parses button data and calls logger
    _diagnosticLogger?.LogButtonPress(expectedButton, rawValue, actualState);
}

// DiagnosticLogger.cs
public void LogButtonPress(string expectedButton, ushort rawButtonValue, ButtonState actualState)
{
    // Logs to file with timestamp and analysis
}

// ButtonTestViewModel.cs
private async Task TestButton(string buttonName, CancellationToken ct)
{
    // Orchestrates single button test
}
```

---

## 🎯 Next Steps

1. ✅ **Run test with real Wiimote** - Capturar datos de todos los botones
2. 📊 **Analyze CSV export** - Identificar patrones en mapeo incorrecto
3. 🔧 **Fix ButtonState enum** - Ajustar valores hex basado en datos reales
4. 🔋 **Implement battery reading** - Solicitar Status Report (0x20)
5. ✅ **Verify Home button** - Confirmar detección de bit 7

---

## 📚 References

- **Wiibrew Protocol**: https://wiibrew.org/wiki/Wiimote#Core_Buttons
- **CURRENT_BUTTON_MAPPING.md**: Documentación del problema actual
- **FINAL_SOLUTION.md**: Arquitectura general del sistema

---

**Created**: 2026-01-25  
**Status**: Ready for testing with real hardware  
**Version**: 1.0
