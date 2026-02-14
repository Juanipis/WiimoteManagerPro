# 🎮 Wiimote Manager Pro

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
![Build Status](https://github.com/Juanipis/WiimoteManagerPro/actions/workflows/ci.yml/badge.svg)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/Juanipis/WiimoteManagerPro)
![Platform](https://img.shields.io/badge/platform-Windows-blue)

**A professional, production-ready tool to use your Nintendo Wii Remotes as Xbox 360 controllers on Windows.**

> 🏆 **Built for the [GitHub Copilot CLI Challenge](https://dev.to/challenges/github-2026-01-21)**
> This entire project was architected, implemented, refined, and documented using the GitHub Copilot CLI agent.

---

## ✨ Features

- **🎮 Xbox 360 Emulation**: Plays almost any modern PC game (Steam, Epic, Game Pass) using standard XInput.
- **🎨 Modern UI**: Beautiful Dark Theme with glassmorphism effects, responsive layout, and smooth animations.
- **🧠 Smart Profile System**:
  - **Auto-Switching**: Detects running games and switches profiles automatically.
  - **Templates**: Built-in presets for Racing, FPS, Platformer, Fighting, and Sports games.
  - **Analytics**: Tracks your most used profiles and favorite games.
- **🏎️ Motion Controls**: Use the Wiimote's accelerometer for steering in racing games (tilt-to-steer).
- **🔌 Multi-Controller Support**: Connect and manage up to 4 Wiimotes simultaneously.
- **🛠️ Battery Efficient**: Hybrid polling system optimizes for low latency and high battery life.

## 🚀 Getting Started

### Prerequisites
1. **[ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases)** (Required for controller emulation)
2. **Bluetooth Adapter** (Bluetooth 4.0+ recommended)
3. **.NET 8.0 Runtime** (if running from source)

### ⚠️ Compatibility Warning

**Only original Nintendo Wiimotes (Model RVL-001 or RVL-036) are officially supported.**
Third-party "clones" often use non-standard Bluetooth protocols that may fail to connect or report data correctly.

### Installation
1. Download the latest release from the [Releases Page](https://github.com/Juanipis/WiimoteManagerPro/releases).
2. Extract the ZIP file.
3. Run `WiimoteManager.exe`.

### Quick Start
1. Open **Bluetooth Settings** in Windows.
2. Press the red **SYNC** button on your Wiimote (battery compartment).
3. Pair with **Nintendo RVL-CNT-01** (Skip PIN if asked).
4. Launch **Wiimote Manager Pro**.
5. Click **Connect**. The LEDs on your Wiimote will light up to indicate player number.

#### 💡 Pairing on Windows 11 (PIN Fix)
Windows 11 sometimes insists on a PIN code. To bypass this:
1. Press `Win + R` on your keyboard.
2. Type `devicepairingwizard` and press Enter.
3. Use this classic wizard to find and pair your **Nintendo RVL-CNT-01** device.
4. If asked for a PIN, simply leave it blank and click "Next".

---

## 🤖 Built with GitHub Copilot CLI

This project serves as a showcase of what's possible with AI-assisted development. The GitHub Copilot CLI was used to:

1.  **Architect the Solution**: Designed the MVVM structure and service layers.
2.  **Implement Complex Protocols**: Reverse-engineered the Wiimote Bluetooth HID protocol (including the tricky 0x31 reporting mode).
3.  **Create the UI**: Generated the XAML for the modern dark theme and responsive grid layouts.
4.  **Debug Hardware Issues**: Analyzed battery reporting bugs and implemented a hybrid polling fix.
5.  **Write Documentation**: Authored the technical guides and user documentation.
6.  **Setup DevOps**: Created the GitHub Actions CI/CD workflows and release automation.

---

## 📚 Documentation

- [**User Guide**](docs/user-guide/PROFILE_GUIDE.md): How to manage profiles and settings.
- [**Technical Architecture**](docs/technical/ARCHITECTURE.md): Deep dive into the code structure.
- [**Wiimote Protocol**](docs/technical/WIIMOTE_PROTOCOL_GUIDE.md): Technical details on HID communication.
- [**Troubleshooting**](docs/user-guide/QUICK_START.md#troubleshooting): Common solutions.

## 🛠️ Development

To build from source:

```powershell
# Clone repository
git clone https://github.com/Juanipis/WiimoteManagerPro.git

# Build solution
dotnet build WiiMoteUtlity/WiimoteManager.sln
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
