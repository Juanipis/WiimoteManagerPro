# Profile Management Guide

## 🎯 Overview

The Wiimote Manager Pro features a powerful profile system that allows you to create, manage, and automatically switch between different control schemes for your games.

## Quick Actions

1.  **Select Profile**: Use the dropdown menu on the Wiimote card to switch profiles.
2.  **Create Profile**: Click the **+** button to create a copy of the currently selected profile.
3.  **Delete Profile**: Click the **Trash** icon to delete the selected profile (Default profile cannot be deleted).
4.  **Save Changes**: Any changes made in the Mapping Window are automatically saved to the currently selected profile.

## Profile Manager Window

Click **"Profile Manager"** button in the main window to access advanced features:

- **Search & Filter**: Find profiles by name, tags, or associated games
- **Sort Options**: By name, last used, most used, favorites, or creation date
- **Import/Export**: Share profiles with other users (JSON format)
- **Profile Templates**: Quick-create from pre-built templates
- **Usage Stats**: See how many times each profile was used
- **Metadata**: Add descriptions, tags, and game associations

## 📦 Profile Templates

### 🏎️ Racing Game Template
Perfect for racing games with **accelerometer tilt steering**:
- **Tilt to Steer**: Motion-based steering using Wiimote tilt
- **Buttons**: 2=Accelerate, 1=Brake, A=Handbrake, B=Nitro
- **Suggested Games**: Need for Speed, Forza Horizon, Mario Kart, TrackMania, Rocket League

### 🍄 Platformer Template
Optimized for 2D platformers (Mario-style):
- **Horizontal Grip**: NES-style button layout
- **Buttons**: 2=Jump, 1=Run, A=Crouch, B=Special
- **Suggested Games**: Super Mario, Sonic, Celeste, Hollow Knight

### 🥊 Fighting Game Template
Quick access to all buttons for combos:
- **Buttons**: A=Light Punch, 2=Heavy Punch, B=Light Kick, 1=Heavy Kick
- **Suggested Games**: Street Fighter, Mortal Kombat, Tekken, Guilty Gear

### 🔫 FPS/Shooter Template
First-person shooter optimized:
- **Buttons**: B=Shoot, A=Jump, 1=Reload, 2=Switch Weapon
- **Suggested Games**: Call of Duty, Halo, Counter-Strike, Doom

### ⚽ Sports Game Template
General sports game controls:
- **Buttons**: A=Pass, B=Shoot, 1=Through Ball, 2=Lob
- **Suggested Games**: FIFA, NBA 2K, Madden NFL, Rocket League

## 🔄 Auto-Profile Switching

The app monitors running processes and automatically switches to the associated profile:

1. Open **Profile Manager**
2. Edit a profile and add game process names (e.g., "Need for Speed", "Mario")
3. When you launch the game, the profile switches automatically
4. Works in the background while you play

## 📊 Usage Analytics

Profiles track:
- **Usage Count**: How many times you've used the profile
- **Last Used**: When you last selected this profile
- **Created/Modified**: Timestamps for management
- **Favorite Status**: Mark your most-used profiles

Profiles are stored in: `%AppData%\WiimoteManager\Profiles\`
