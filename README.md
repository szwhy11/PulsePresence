<div align="center">

<img src="assets/icon.png" alt="PulsePresence Logo" width="104">

# PulsePresence

**Your music rhythm and track from any application in your Discord status.**

Yandex Music, Spotify, YouTube Music, VK, SoundCloud, Telegram, Chrome, Opera, AIMP, foobar2000 — whatever plays on Windows.

[![Release](https://img.shields.io/github/v/release/szwhy11/PulsePresence?style=for-the-badge&logo=tag&color=5865F2&labelColor=18191c)](https://github.com/szwhy11/PulsePresence/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/szwhy11/PulsePresence/total?style=for-the-badge&logo=windows&color=00F5FF&labelColor=18191c)](https://github.com/szwhy11/PulsePresence/releases)
[![Stars](https://img.shields.io/github/stars/szwhy11/PulsePresence?style=for-the-badge&logo=github&color=2D91FA&labelColor=18191c)](https://github.com/szwhy11/PulsePresence/stargazers)
[![Telegram Support](https://img.shields.io/badge/Telegram-Поддержка-26A5E4?style=for-the-badge&logo=telegram&logoColor=white&labelColor=18191c)](https://t.me/martdoma)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-7289da?style=for-the-badge&logo=windows11&logoColor=white&labelColor=18191c)](https://github.com/szwhy11/PulsePresence)
[![License](https://img.shields.io/github/license/szwhy11/PulsePresence?style=for-the-badge&color=8B5CF6&labelColor=18191c)](LICENSE)

<br/>

**[Русский](README.ru.md)** · English

<br/>

[**⬇ Download PulsePresence.exe (v2.0.0)**](https://github.com/szwhy11/PulsePresence/releases/latest/download/PulsePresence.exe)

<br/>

<img src="assets/preview.png" alt="PulsePresence Settings Preview" width="720">

</div>

---

## ⚡ Highlights

* **Liquid Glass Dark Theme:** Clean translucent frosted glass with vector SVG icons and zero emojis.
* **Smart Player Detection:** Displays *"Listening to Yandex Music"*, *"Listening to YouTube Music"*, *"Listening to SoundCloud"*, or custom player names dynamically per active audio session.
* **Customizable Member List Display:**
  - `Artist — Track` (e.g. `Kishlak — 11:11`)
  - `Track — Artist` (e.g. `11:11 — Kishlak`)
  - `Artist`
  - `Track title`
  - `Source name (Yandex Music, SoundCloud...)`
  - `Discord application name`
* **Clean Discord Card Formatting:** Player name is not repeated on line 3 when already shown in the header.
* **Multi-DPI Pixel-Perfect Tray Icon:** 100% transparent vector pulse equalizer designed to look razor-sharp on 100%, 125%, 150%, and 200% DPI on both dark and light taskbars.
* **Fast Cover Art Caching:** Reads thumbnail from Windows Media API and uploads asynchronously via high-speed image cache so track detection never stutters.
* **Ultra-Lightweight & Efficient:** Pure .NET Framework 4.8 & pure WPF. No external dependencies, no bloated runtime, ~35 MB RAM, 0% CPU.

---

## 💬 Bug Reports & Feedback

Found a bug or have a feature suggestion?
* Telegram: **t.me/martdoma** (`@martdoma`)
* GitHub Issues: [szwhy11/PulsePresence/issues](https://github.com/szwhy11/PulsePresence/issues)

---

## 🚀 Quick Start

1. Download **`PulsePresence.exe`** from [Releases](https://github.com/szwhy11/PulsePresence/releases/latest).
2. Launch it. The neon pulse equalizer icon will appear in your system tray.
3. Make sure Discord has **User Settings → Activity Privacy → «Share your detected activities with others»** enabled.
4. Play any track in your favorite music player or browser!

---

## 🛠 Building from Source

PulsePresence builds with the standard MSBuild / C# compiler included with .NET Framework 4.8 on Windows 10 & 11:

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

The resulting `PulsePresence.exe` will be ready in the project directory.

---

## 📄 License

Licensed under the [MIT License](LICENSE).
