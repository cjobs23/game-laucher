 BF2 Mod Launcher

BF2 Mod Launcher is a Windows Forms application designed to manage and launch mods for the game Battlefield 2. It provides functionalities to check for updates, download mod files, and launch the game with the specified mod configuration.

## Features

- **Mod Management**: Automatically checks for updates and downloads mod files from a remote server.
- **Game Launcher**: Launches Battlefield 2 with the specified mod configuration.
- **Logging**: Keeps a log of operations for troubleshooting and monitoring.
- **User Interface**: Simple and intuitive UI for managing mods and launching the game.

## Getting Started

### Prerequisites

- Windows operating system
- .NET Framework 4.7.2 or later
- Battlefield 2 installed on your system

### Installation

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/yourusername/bf2-mod-launcher.git
Open the Solution:

Open the BF2ModLauncher.sln file in Visual Studio.
Build the Project:

Build the solution using Visual Studio to ensure all dependencies are resolved.
Configuration
Mod Path: The default mod path is set to mods/bf2rw relative to the application's directory. You can change this in the AppConfig class.
Remote Base URL: The URL from which the mod files are downloaded. Update this in the AppConfig class if your mod files are hosted elsewhere.
Usage
Check for Updates: Click the "Check for Updates" button to check if there are any new mod files available.
Download Updates: If updates are available, the application will prompt you to download them.
Launch Game: Click the "Play!" button to launch Battlefield 2 with the specified mod.
Contributing
Contributions are welcome! Please open an issue or submit a pull request.

License
This project is licensed under the MIT License. See the LICENSE file for details.

Acknowledgments
Thanks to the Battlefield 2 community for their support and contributions.
