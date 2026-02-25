# Setting Up UrlVault in VS Code

## Prerequisites

### 1. Install the .NET 8 SDK

Download from <https://dotnet.microsoft.com/download/dotnet/8.0> and install the **SDK** (not just the Runtime).

Verify the installation:

```bash
dotnet --version
# should print 8.x.x
```

### 2. Install VS Code

Download from <https://code.visualstudio.com/>.

### 3. Required VS Code Extensions

Install these from the Extensions panel (`Ctrl+Shift+X`):

| Extension | ID | Purpose |
|-----------|----|---------|
| **C# Dev Kit** | `ms-dotnettools.csdevkit` | IntelliSense, refactoring, test runner |
| **C#** | `ms-dotnettools.csharp` | Core language support (installed by Dev Kit) |
| **.NET Install Tool** | `ms-dotnettools.vscode-dotnet-runtime` | SDK management |

Optional but recommended:

| Extension | ID |
|-----------|----|
| XAML Styler | `Togusa09.XAML` |
| EditorConfig | `editorconfig.editorconfig` |

## Opening the Project

```bash
code /path/to/release1
```

VS Code will detect the `.sln` file. Accept any prompts to restore NuGet packages.

## Building

Open the integrated terminal (`Ctrl+\``) and run:

```bash
cd UrlVault
dotnet restore
dotnet build
```

## Running

```bash
dotnet run --project UrlVault/UrlVault.csproj
```

> **Note:** WPF requires Windows. On Linux/macOS the project will *build* (cross-compile) but cannot *run*.

## Debugging in VS Code

1. Open the Command Palette (`Ctrl+Shift+P`) → **".NET: Generate Assets for Build and Debug"**
2. VS Code creates `.vscode/launch.json` and `tasks.json` automatically
3. Press `F5` to start debugging

The generated `launch.json` looks like:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (console)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/UrlVault/bin/Debug/net8.0-windows/UrlVault.exe",
      "args": [],
      "cwd": "${workspaceFolder}/UrlVault",
      "stopAtEntry": false
    }
  ]
}
```

## IntelliSense Tips

- C# Dev Kit provides full IntelliSense for XAML-bound properties
- Right-click any `.xaml.cs` file → **Go to Definition** works across the MVVM boundary
- Use `Ctrl+.` for quick-fix suggestions
