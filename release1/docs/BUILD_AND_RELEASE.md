# Build and Release

## Development Build

```bash
cd release1/UrlVault
dotnet restore
dotnet build
```

The output lands in `UrlVault/bin/Debug/net9.0-windows/`.

## Run Locally

```bash
dotnet run --project UrlVault/UrlVault.csproj
```

## Release Build (optimised)

```bash
dotnet build -c Release
```

Output: `UrlVault/bin/Release/net9.0-windows/`

## Publish – Framework-Dependent (small, requires .NET 9 runtime on target machine)

```bash
dotnet publish UrlVault/UrlVault.csproj \
  -c Release \
  -r win-x64 \
  --no-self-contained \
  -o ./publish/framework-dependent
```

The output folder contains `UrlVault.exe` plus a few DLLs. The user must have .NET 9 Desktop Runtime installed.

## Publish – Self-Contained (larger, no runtime required)

```bash
dotnet publish UrlVault/UrlVault.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish/self-contained
```

This produces a single `UrlVault.exe` (~150 MB) that runs without any prerequisites.

## Important: Ship the data folder

After publishing, copy the `data/` folder next to the executable so the app finds its JSON files on first launch:

```
publish/
  UrlVault.exe
  data/
    config.json
    urls.json
```

If the `data/` folder is missing, UrlVault creates it with defaults on first run, so this step is optional.

## CI with GitHub Actions (example)

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore release1/UrlVault/UrlVault.csproj
      - run: dotnet build release1/UrlVault/UrlVault.csproj -c Release --no-restore
      - run: dotnet publish release1/UrlVault/UrlVault.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
      - uses: actions/upload-artifact@v4
        with:
          name: UrlVault
          path: publish/
```


