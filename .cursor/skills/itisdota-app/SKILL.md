---
name: itisdota-app
description: >-
  Develops and maintains the ItisDota ASP.NET Razor Pages app (players JSON
  import, PostgreSQL upsert by Telegram tag, editable team prompt, Docker
  Compose). Use when editing ItisDota code, Docker, import/UI, PromptService,
  PlayerImportService, or appsettings/EF layers.
---

# ItisDota App Development

## Layout

| Layer | Path | Responsibility |
|-------|------|----------------|
| Presentation | `ItisDota/Pages/` | Razor Pages + page JS |
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

- File upload: `multipart/form-data`, `IFormFile JsonFile` + textarea fallback
- Build-prompt button: enabled only when selected count `> 0 && count % 10 === 0`
- Clipboard copy stays on the same page (no navigation)
- Players shown as `PlayerDto` via `PlayerMappingExtensions` (no `SelectionCount` in UI)
- `SelectionCount` increments on RecordSelections AJAX; list order unchanged until page reload
- Delete per row; Telegram search via `TgSearch` query

## Docker

- Compose at solution root; app URL `http://localhost:8080`
- Persist DB via volume `itisdota_pgdata`
- Rebuild web after code changes: `docker compose up --build`
- Schema: EF Core migrations (`dotnet ef database update` / `MigrateAsync` on startup)
