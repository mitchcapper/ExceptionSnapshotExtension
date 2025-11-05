
# Exception Snapshot Extension

## Overview

Exception Snapshot is a Visual Studio extension that helps you manage the "BreakOnFirstChance" setting for individual exceptions or groups of exceptions with ease. It provides a dedicated tool window for saving, and restoring exception break settings, making debugging more efficient and organized.  It stores the conditions along with their enabled states.

## Features

- Manage "BreakOnFirstChance" for specific exceptions or exception groups
- Visual Studio tool window for snapshot management
- Save and restore exception break settings
- Easy-to-use UI integrated into Visual Studio
- Supports Visual Studio 2017–2026

## Installation

### From Visual Studio Marketplace

1. Open Visual Studio
2. Go to `Extensions > Manage Extensions`
3. Search for **Exception Snapshot**
4. Click `Download` and restart Visual Studio to complete installation

### Manual Build & Install

1. Clone this repository:
	```pwsh
	git clone https://github.com/VladimirUAZ/ExceptionSnapshotExtension.git
	```
2. Open `ExceptionSnapshotExtension.sln` in Visual Studio
3. Build the solution (`Build > Build Solution`)
4. Locate the generated `.vsix` file in the `bin/Debug` folder
5. Double-click the `.vsix` file to install the extension

## Usage

1. After installation, open Visual Studio and load your solution/project.
2. Go to `View > Other Windows > SnapshotWindow` (or use the command added to the menu).
3. Use the tool window to manage exception break settings:
	- View current snapshots
	- Save snapshots of your settings
	- Restore previous snapshots as needed

## Development & Contribution

### Prerequisites
- Visual Studio 2017 or newer
- .NET Framework 4.5 or higher

### Build Instructions
1. Clone the repository
2. Open the solution in Visual Studio
3. Build the project

### Contributing
Pull requests and issues are welcome! Please open an issue to discuss your ideas or submit a PR for review.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
