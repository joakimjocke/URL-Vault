# Troubleshooting

## Build Issues

### `error MSB4018: The "GenerateResource" task failed`
**Cause:** Building a WPF project on Linux without the Windows desktop workload.  
**Fix:** This is expected when building on a non-Windows host. The project targets `net8.0-windows`. Use a Windows machine or a Windows GitHub Actions runner to produce a runnable binary. The `dotnet build` command on Linux will still compile all C# and validate XAML, but you cannot run the output.

### `CS0234: The type or namespace name 'Windows' does not exist`
**Cause:** The project is not targeting `net8.0-windows` or `<UseWPF>true</UseWPF>` is missing.  
**Fix:** Check `UrlVault.csproj`:
```xml
<TargetFramework>net8.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
```

### NuGet restore fails / no internet
**Fix:** Copy the NuGet cache from another machine or set `NUGET_PACKAGES` to a local folder containing the required packages.

---

## Runtime Issues

### The app opens but the list is empty
**Cause:** `data/urls.json` is missing, empty, or malformed.  
**Fix:** Check that `data/urls.json` exists next to the executable and contains valid JSON (at minimum `[]`). The app will recreate it as an empty array on the next save.

### "Fetch Title" returns an empty title
**Possible causes:**
1. No internet connection
2. The site blocks automated requests (missing/incorrect User-Agent)
3. The page uses JavaScript to inject the `<title>` tag after load (SPA)
4. The request timed out (> 8 seconds)

**Fix:** Enter the title manually, or try a direct link to a static HTML page.

### Categories or tags are missing from drop-downs
**Cause:** `data/config.json` is missing or has a JSON syntax error.  
**Fix:** Delete or fix `config.json`. UrlVault will regenerate it with defaults on the next launch.

### Window appears off-screen
**Fix:** Delete the window-position settings if any are cached, or use `Win + Arrow` to move the window back onto screen.

### High-DPI blurry text
**Fix:** Add an `app.manifest` with DPI-aware settings, or set the process DPI awareness in code:
```csharp
// In App.xaml.cs, before InitializeComponent()
[System.Runtime.InteropServices.DllImport("user32.dll")]
static extern bool SetProcessDPIAware();
```
WPF handles DPI automatically for most setups; if text is blurry, check your display scaling settings.

---

## Data Issues

### Accidental deletion – can I recover?
UrlVault does **not** keep backups automatically. To protect your data:
- Copy `data/urls.json` to a backup location periodically
- Commit the file to a private Git repository

### urls.json grew very large
JSON files do not compact themselves. Open the file in any text editor or JSON viewer and delete unwanted entries. The file is plain UTF-8 JSON.

### Special characters (emoji, non-ASCII) not saving correctly
UrlVault uses `System.Text.Json` with UTF-8 encoding by default. If you see garbled characters, check that the file is saved as UTF-8 (without BOM). Most modern text editors default to this.
