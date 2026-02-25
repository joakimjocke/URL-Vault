# UrlVault

A Windows desktop URL manager built with WPF and .NET 8. Save, organise, search, and open your URLs with ease.

## Features

- **Add / Edit / Delete URLs** with title, category, tags, and a comment
- **Auto-fetch page titles** — click "Fetch Title" and UrlVault downloads the page and extracts the `<title>` tag
- **Search** across title, URL, comment, and tags in real time
- **Category filter** — drop-down to narrow the list to a single category
- **Tag filter** — check any combination of tags to filter entries
- **Copy URL** to clipboard in one click
- **Open in Browser** — double-click a row or press the toolbar button to launch the URL in your default browser
- **Duplicate detection** — warns you before saving a URL you already have
- **Persistent JSON storage** — no database required; all data lives in two plain JSON files

## File Locations

| File | Purpose |
|------|---------|
| `UrlVault/data/urls.json` | All saved URL entries |
| `UrlVault/data/config.json` | Categories and tags configuration |

Both files are stored relative to the application executable so they travel with the app.

## Project Structure

```
release1/
  UrlVault.sln
  UrlVault/
    Models/          – UrlEntry, AppConfig
    Services/        – StorageService, ConfigService, TitleFetcherService
    ViewModels/      – BaseViewModel, MainViewModel, AddEditViewModel, RelayCommand
    Views/           – MainWindow, AddEditWindow
    Converters/      – StringJoinConverter, StringToVisibilityConverter, InverseBoolToVisibilityConverter
    data/            – config.json, urls.json
  docs/
    README.md
    SETUP_VSCODE.md
    BUILD_AND_RELEASE.md
    DATA_FORMAT.md
    TROUBLESHOOTING.md
```

## Quick Start

```bash
cd release1/UrlVault
dotnet run
```

See [SETUP_VSCODE.md](SETUP_VSCODE.md) for full VS Code setup instructions and [BUILD_AND_RELEASE.md](BUILD_AND_RELEASE.md) for build and publish commands.
