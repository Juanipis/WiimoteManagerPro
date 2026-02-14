## [1.0.2](https://github.com/Juanipis/WiimoteManagerPro/compare/v1.0.1...v1.0.2) (2026-02-14)


### Features

* Update versioning and display application version in UI ([b568576](https://github.com/Juanipis/WiimoteManagerPro/commit/b5685767d2164c68c52c35ed1810bb581ccdc00e))

## [1.0.1](https://github.com/Juanipis/WiimoteManagerPro/compare/v1.0.0...v1.0.1) (2026-02-14)


### Bug Fixes

* Update repository references to use the correct GitHub username and API URL ([439cfed](https://github.com/Juanipis/WiimoteManagerPro/commit/439cfedb16c9e56ae7ba5bbdfd75260c910f8d97))

# 1.0.0 (2026-02-14)


### Bug Fixes

* HID protocol corrections for LED/vibration/accelerometer ([b32eb6e](https://github.com/Juanipis/WiiMoteUtility/commit/b32eb6ed751732e6c2c3b71f8fa010c90bcce800))
* Initialize CancellationTokenSource for discovery and read loop in ViewModels ([da81a78](https://github.com/Juanipis/WiiMoteUtility/commit/da81a787d37bae172dbd34892759669df73370e4))
* Support multiple Wiimotes by using DevicePath as unique key and setting appropriate LEDs ([b08216b](https://github.com/Juanipis/WiiMoteUtility/commit/b08216b5dd0f08a2383c783b412715c9afb4dbe0))


### Features

* Add UCH profile template and integrate with profile management ([787c195](https://github.com/Juanipis/WiiMoteUtility/commit/787c1955a1f6cab38db3ec196d7bbb07fe7815d5))
* Add Welcome Window and Update UI Styles ([871217f](https://github.com/Juanipis/WiiMoteUtility/commit/871217f4015caf335768d88fba332aaf3bf7a52b))
* Enhance profile management and button mapping for Rocket League ([f5ba4ba](https://github.com/Juanipis/WiiMoteUtility/commit/f5ba4baf6e3a961c92511d70617720ab3a719264))
* Fix button mapping and add diagnostic testing system ([ddcab7f](https://github.com/Juanipis/WiiMoteUtility/commit/ddcab7fcdad011e2deade5d9efb8944eeded7147))
* Fix Home button and Accelerometer conflict using Hybrid Polling (0x31 + 0x20) ([ab3a433](https://github.com/Juanipis/WiiMoteUtility/commit/ab3a43362d396356b6aa70606a5c04e8caaf85c3))
* Update UI for Profile Manager with button mapping and improved layout ([1463e44](https://github.com/Juanipis/WiiMoteUtility/commit/1463e449a31b89901b66c459bed390e69b58b175))
* Wiimote HID communication working - direct HidSharp implementation, button input reading, LED control ([3b6ecbf](https://github.com/Juanipis/WiiMoteUtility/commit/3b6ecbf1fe03909a73552bed0a6f580f83602d8f))

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-14
### Added
- Initial release of Wiimote Manager Pro
- Modern WPF UI with Dark Theme
- Xbox 360 Controller Emulation via ViGEmBus
- Smart Profile Management system
- **New Party Game Profile**: Added default template for Ultimate Chicken Horse and other party games
- Auto-profile switching based on active process
- Motion control support (Accelerometer to Xbox stick/triggers)
- Support for multiple Wiimotes (up to 4)
- Comprehensive documentation
- GitHub Actions CI/CD pipeline

### Fixed
- Fixed crash in Profile Manager related to XAML Color/Brush resource mismatch
- Improved button styles for better visibility
