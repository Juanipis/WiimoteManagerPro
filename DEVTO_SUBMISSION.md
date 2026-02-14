_This is a submission for the [GitHub Copilot CLI Challenge](https://dev.to/challenges/github-2026-01-21)_

## What I Built

<!-- Provide an overview of your application and what it means to you. -->

I built **Wiimote Manager Pro**, a modern Windows application that breathes new life into old Nintendo Wii Remotes by converting them into fully functional Xbox 360 controllers.

I love playing local multiplayer party games on my PC, but I often run into the classic problem: not enough controllers for everyone. I had a box of old Wiimotes gathering dust, so I decided to put them to use. While emulators like Dolphin have this functionality built-in, I wanted a standalone, system-wide solution that works with _any_ modern PC game—from Steam to Game Pass.

Unlike older, abandoned drivers, this project is built on the modern .NET 8.0 stack and uses the **ViGEmBus** driver for seamless Xbox emulation.

**Key Features:**

- **🎮 Xbox 360 Emulation**: Works with almost any modern PC game (no custom driver config needed per game).
- **🧠 Smart Profile System**: Automatically detects which game is running and switches button mappings instantly.
- **🏎️ Motion Controls**: Utilizes the Wiimote's accelerometer for tilt-to-steer in racing games.
- **🔌 Multi-Controller Support**: Connects up to 4 players simultaneously.
- **🎨 Modern WPF UI**: A sleek, dark-themed dashboard with glassmorphism effects.

## Demo

<!-- Share a link to your project and include a video walkthrough or screenshots showing your application in action. -->

## My Experience with GitHub Copilot CLI

This was my first time building a C# application with the .NET 8 framework. I started with a high-level vision and a blank repository, using the GitHub Copilot CLI not just as an autocomplete tool, but as a Lead Architect and Senior Engineer.

The process was incredible. Since I was new to the ecosystem, Copilot guided me through the entire setup, explaining how to organize a scalable WPF architecture (MVVM) and which dependencies (like `HidSharp` and `ViGEm.Client`) I needed. I heavily utilized **MCP Servers** (like Context7 and Tavily) to let Copilot research the specific HID protocols for the Wiimote directly from technical documentation and wikis.

What surprised me most was how I could leverage different LLMs for different phases:

- **Research & Architecture**: I used models like Gemini to brainstorm the project structure and understand the Bluetooth stack.
- **Coding & Refactoring**: I switched to models specialized in code generation for the heavy lifting of the HID communication logic.

It felt like working with a dedicated team of experts. We went from a simple concept to a polished app with over 5,000 lines of code, 100% test coverage on core features, and a comprehensive documentation suite. Copilot didn't just write code; it taught me _how_ to write better .NET code along the way.

<!-- Don't forget to add a cover image (if you want). -->

<!-- Team Submissions: Please pick one member to publish the submission and credit teammates by listing their DEV usernames directly in the body of the post. -->

<!-- Thanks for participating! -->
