# Building a Professional Wiimote Manager with GitHub Copilot CLI

**Submission for the GitHub Copilot CLI Challenge**

## 🚀 Overview

**Wiimote Manager Pro** is a modern, production-ready Windows application that lets you use Nintendo Wii Remotes as Xbox 360 controllers. It features a beautiful dark UI, motion controls (steering!), smart profile auto-switching, and support for up to 4 players.

What makes this project special is that **it was architected, coded, and documented almost entirely using the GitHub Copilot CLI**.

## 🎥 Demo

[Insert GIF/Video of: 
1. Connecting a Wiimote
2. Showing the UI
3. Playing a racing game with tilt controls]

## 🛠️ How I Built It

I used the **GitHub Copilot CLI** as my lead engineer. Instead of just "tab-completing" lines of code, I used the CLI to:

1.  **Reverse Engineer the Protocol**: I asked Copilot to explain the Wiimote Bluetooth HID protocol. It analyzed the challenges (like the Button/Accel conflict in Mode 0x31) and proposed a "Hybrid Polling" solution that I implemented.
2.  **Design the UI**: I described a "Modern Dark Theme with Glassmorphism", and Copilot generated the XAML resources, styles, and templates that give the app its professional look.
3.  **Debug Complex Issues**: When battery reporting was flaky, I used the CLI to analyze the bit-masking logic in my C# code, and it found the off-by-one error in my byte parsing.
4.  **DevOps & Docs**: Copilot wrote the entire CI/CD pipeline (`release.yml`) and authored the user documentation, transforming a prototype into a shippable product.

## 🌟 Key Features

*   **Xbox 360 Emulation**: Works with 99% of modern PC games via ViGEmBus.
*   **Motion Steering**: Use the Wiimote like a steering wheel for racing games.
*   **Auto-Profiles**: The app watches your running processes. Launch "Need for Speed"? It switches to the Racing profile automatically.
*   **Modern UX**: Responsive, accessible, and beautiful WPF interface.

## 💻 The Code

The architecture follows a clean **MVVM pattern**:

*   **Services**: `HidCommunicationService` handles the raw Bluetooth streams.
*   **ViewModels**: `MainViewModel` orchestrates the state without tightly coupling to the UI.
*   **Views**: Pure XAML with data binding.

## 🏆 Conclusion

This project proves that AI tools like GitHub Copilot CLI aren't just for snippets—they can help architect and ship complete, complex desktop applications.

**[Link to Repository](https://github.com/JuanI/UCHWiiRemoteMod)**
