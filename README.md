# NRoot

**A powerful cross-platform system utility / root management tool.**

![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)
![Platforms](https://img.shields.io/badge/Platforms-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green.svg)

---

## ✨ Features

- **Cross-platform support**: Windows (x64 & arm64), Linux (x64 & arm64), macOS (x64 & arm64)
- **Single-file executable** — easy to distribute with no dependencies
- **Built with .NET 10** — high performance and modern
- **Lightweight & portable**
- Pre-built releases for all major architectures

---

## 📥 Downloads

Download the latest release from the **[Releases page](https://github.com/NuggiDev/NRoot/releases)**.

### Available Builds:

| Platform     | Architecture | Installer format                  |
|--------------|--------------|-------------------------|
| Windows      | x64 / arm64  | `.msi`                  |
| Linux        | x64 / arm64  | `.tar.gz`               |
| macOS        | x64 / arm64  | `.tar.gz`               |

---

## 🚀 Usage

```bash
# Run the application
NRoot.exe                  # Windows
./NRoot                    # Linux / macOS
For available commands:
BashNRoot --help

⚠️ Disclaimer
Use at your own risk.
This software is provided "AS IS", without warranty of any kind.
The author (NuggiDev) is not responsible for any damage, data loss, or system issues that may occur while using this tool.
By using NRoot, you agree that you cannot hold the author liable for any damages caused to your system.

🛠️ Build from Source
Prerequisites

.NET 10 SDK

Commands
Bash# Build the project
dotnet build -c Release

# Publish single-file executables for all platforms
dotnet publish -c Release --self-contained true -r win-x64   --output publish/win-x64
dotnet publish -c Release --self-contained true -r win-arm64 --output publish/win-arm64
dotnet publish -c Release --self-contained true -r linux-x64 --output publish/linux-x64
dotnet publish -c Release --self-contained true -r linux-arm64 --output publish/linux-arm64
dotnet publish -c Release --self-contained true -r osx-x64   --output publish/osx-x64
dotnet publish -c Release --self-contained true -r osx-arm64 --output publish/osx-arm64

📁 Project Structure
textNRoot/
├── NRoot/
│   ├── Program.cs
│   └── NRoot.csproj
├── .gitignore
├── .gitattributes
├── LICENSE
└── README.md

📄 License
This project is licensed under the MIT License — see the LICENSE file for details.

🤝 Contributing
Contributions, issues, and feature requests are welcome!
Made with ❤️ by NuggiDev
