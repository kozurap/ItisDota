---
name: itisdota-app
description: >-
  Develops and maintains the ItisDota ASP.NET Blazor app (players JSON
  import, PostgreSQL upsert by Telegram tag, editable team prompt, Docker
  Compose). Use when editing ItisDota code, Docker, import/UI, PromptService,
  PlayerImportService, or appsettings/EF layers.
---

# ItisDota App Development

## Layout

| Layer | Path | Responsibility |
|-------|------|----------------|
| Presentation | `ItisDota/Components/` | Blazor Interactive Server UI |
| Business | `ItisDota/Business/Services/` | Parse JSON, upsert orchestration, prompt get/set |
| Data | `ItisDota/Data/` | Entities, `AppDbContext`, repositories |

RootNamespace / namespace: `ItisDota`.

## Import JSON shape

```json
[
  [["ИРЛ Имя","Тимур"],["Тег в tg ","@kozurap"],["Сколько ПТС","3500-4500"],
   ["Роль (1-й приоритет)","1"], ... ]
]
```

Keys may have trailing spaces — normalize with `Trim()` before match (`PlayerImportService`).

## Upsert

Match on normalized `TgTag`. Existing → update name/MMR/roles; missing → insert. Unique index on `TgTag`.

## Prompt

- Key: `TeamPrompt` in `AppSettings`
- Constant: `PromptService.DefaultPrompt`
- On startup: seed if missing

## UI constraints

- File upload: textarea or dropped/selected JSON file, read as text, then `ImportPlayersAsync`
- Build-prompt button: enabled only when selected count `> 0 && count % 10 === 0`
- Clipboard copy stays on the same page (no navigation)
- Players shown as `PlayerDto` via `PlayerMappingExtensions` (no `SelectionCount` in UI)
- `SelectionCount` increments when the prompt is built; list order stays until the next full reload
- Delete from the edit dialog; Telegram search filters the loaded list
- UI calls `PromptScope` so each action gets a fresh `DbContext`

## Docker

- Compose at solution root; app URL `http://localhost:8080`
- Persist DB via volume `itisdota_pgdata`
- Rebuild web after code changes: `docker compose up --build`
- Schema: EF Core migrations (`dotnet ef database update` / `MigrateAsync` on startup)
