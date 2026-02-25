# Master Prompt: "URL Vault" (C# .NET Desktop App, VS Code, JSON)

## Role

You are a senior C#/.NET desktop engineer and technical writer. Build a
small Windows desktop application that helps me store and manage URLs,
using local JSON files. Prioritize a clean UX, maintainable
architecture, and excellent documentation.

## Target platform

-   Windows 10/11
-   .NET (latest LTS preferred)
-   Development in Visual Studio Code (not full Visual Studio)

## UI framework choice

Pick one and stick to it: - Preferred: WPF (MVVM) - Acceptable
alternative if WPF tooling becomes too heavy in VS Code: WinForms
Explain briefly why you chose it, but then implement fully with that
choice.

## Core features (must-have)

### 1. Add URL

-   Input field: paste a web URL into the app.
-   On paste or on "Fetch title" action: automatically retrieve the page
    title from the URL.
    -   Implementation detail: perform an HTTP GET, parse HTML
        ```{=html}
        <title>
        ```
        safely.
    -   Handle timeouts, redirects, invalid certs gracefully (do not
        crash).
    -   If title cannot be fetched, allow manual title entry.

### 2. Metadata

Each saved entry must include: - id (GUID) - url - title - dateSaved
(ISO-8601 string, consistent format) - category (single selection) -
tags (multi-select) - comment (free text) - lastModified (ISO-8601)

### 3. Persistence

-   Save all URL entries into a local JSON file, e.g. data/urls.json
-   Categories and tags are configured in a separate JSON file read at
    startup, e.g. data/config.json
-   Create files/directories on first run if missing.
-   Use atomic save (write temp then replace) to avoid corruption.
-   Keep JSON human-readable (indented).

### 4. List + search + filter

Main list view of saved URLs with columns: - Title - URL - Category -
Tags - DateSaved

Provide: - Search box (search across title, url, comment, tags) - Filter
by category - Filter by one or multiple tags - Sorting (default:
dateSaved descending)

### 5. Clipboard + open

-   A "Copy URL" action copies the URL to clipboard.
-   Double-clicking a list item opens the URL in the default browser.

### 6. Edit existing entry

-   Select an entry and edit title/category/tags/comment.
-   Save updates back to JSON.
-   Provide "Cancel" to discard changes.

## Nice-to-have (include if easy, but don't bloat)

-   Duplicate detection (warn if URL already exists, allow save anyway).
-   Import/export (optional).
-   Basic validation for URLs.

------------------------------------------------------------------------

# Data file specifications

## data/config.json

Contains: - categories: array of strings - tags: array of strings

Example: - categories: \["Work","Personal","Hacking","Dev","Infra"\] -
tags: \["C#","React","Security","Docker","Neo4j"\]

Allow categories/tags to be edited manually in this file and reloaded on
next start.

## data/urls.json

Contains an array of objects matching the entry model: - id - url -
title - dateSaved - category - tags - comment - lastModified

------------------------------------------------------------------------

# Architecture requirements

-   Clear structure:
    -   Models/
    -   Services/ (TitleFetcher, StorageService, ConfigService)
    -   ViewModels/ (if WPF/MVVM)
    -   Views/
-   No database. JSON only.
-   Async IO for network/title fetching and file operations.
-   Centralized error handling and user-friendly messages.
-   Unit-testable core services where reasonable.

------------------------------------------------------------------------

# Implementation details

## Title fetching

-   Use HttpClient with a 5--10 second timeout.
-   Handle compressed responses.
-   Parse
    ```{=html}
    <title>
    ```
    robustly.
-   Avoid downloading huge content (limit read size if possible).

## JSON

-   Use System.Text.Json
-   Use indented formatting.

## Open URL

-   Use Process.Start with UseShellExecute = true

## Clipboard

-   Use framework-appropriate clipboard API.

------------------------------------------------------------------------

# UX requirements

-   Single-window layout is acceptable.
-   Fast workflow: Paste URL → Auto-title → Select category/tags →
    Comment → Save.
-   Keyboard-friendly navigation where easy.

------------------------------------------------------------------------

# Documentation deliverables

Create a /docs folder containing:

1.  README.md
    -   What the app does
    -   Features
    -   File locations
2.  SETUP_VSCODE.md
    -   Install .NET SDK
    -   Install VS Code
    -   Required VS Code extensions
    -   How to build/run/debug
3.  BUILD_AND_RELEASE.md
    -   dotnet build
    -   dotnet run
    -   dotnet publish instructions
4.  DATA_FORMAT.md
    -   Exact schema for config.json and urls.json
    -   Examples
5.  TROUBLESHOOTING.md
    -   Common issues and solutions

Also include: - .gitignore - Clear folder structure - Copy-paste command
blocks

------------------------------------------------------------------------

# Output format requirements

1.  Start with a short Plan (bullet points).
2.  Provide full project folder structure.
3.  Provide code files with filenames and contents.
4.  Provide sample data/config.json and data/urls.json.
5.  Provide all documentation markdown files.
6.  Ensure project runs with:
    -   dotnet restore
    -   dotnet build
    -   dotnet run

If using WPF, include all necessary XAML and bindings.

Do not leave TODOs for core features. Implement full working
application.

------------------------------------------------------------------------

# Assumptions

-   App name: UrlVault
-   .NET version: latest LTS
-   Storage paths: relative ./data/ during development.

If any assumption materially impacts the solution, state it once and
proceed.

------------------------------------------------------------------------

# Guardrails

-   Keep dependencies minimal.
-   Focus on stable MVP first.
-   Code should be clean, readable, and structured.
