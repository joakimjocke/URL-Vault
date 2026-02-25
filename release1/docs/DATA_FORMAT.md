# Data Format

UrlVault stores all persistent data in two JSON files inside the `data/` directory next to the executable.

---

## urls.json

An array of URL entry objects.

### Schema

| Field | Type | Description |
|-------|------|-------------|
| `id` | `string` (GUID) | Unique identifier, auto-generated |
| `url` | `string` | The full URL including scheme |
| `title` | `string` | Page title (fetched or manually entered) |
| `dateSaved` | `string` (ISO 8601) | UTC timestamp when the entry was created |
| `category` | `string` | One of the categories defined in `config.json` |
| `tags` | `string[]` | Zero or more tags from `config.json` |
| `comment` | `string` | Free-text note |
| `lastModified` | `string` (ISO 8601) | UTC timestamp of the last edit |

### Example

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "url": "https://learn.microsoft.com/dotnet/csharp/",
    "title": "C# documentation – Microsoft Learn",
    "dateSaved": "2024-06-01T10:30:00.0000000Z",
    "category": "Dev",
    "tags": ["C#"],
    "comment": "Official C# docs",
    "lastModified": "2024-06-01T10:30:00.0000000Z"
  },
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "url": "https://hub.docker.com/",
    "title": "Docker Hub",
    "dateSaved": "2024-06-02T08:00:00.0000000Z",
    "category": "Infra",
    "tags": ["Docker"],
    "comment": "",
    "lastModified": "2024-06-02T08:00:00.0000000Z"
  }
]
```

---

## config.json

Defines the available categories and tags.

### Schema

| Field | Type | Description |
|-------|------|-------------|
| `categories` | `string[]` | List of category names shown in drop-downs |
| `tags` | `string[]` | List of tag names shown in checkboxes |

### Example

```json
{
  "categories": ["Work", "Personal", "Hacking", "Dev", "Infra"],
  "tags": ["C#", "React", "Security", "Docker", "Neo4j"]
}
```

### Customising

Edit `config.json` directly and restart UrlVault. The new categories and tags appear in the UI immediately. Existing URL entries are not affected by adding or removing values here.

---

## Atomic Writes

Both files are written atomically: the app writes to a `.tmp` sibling file first, then calls `File.Move` with `overwrite: true`. This prevents data loss if the process is killed mid-write.
