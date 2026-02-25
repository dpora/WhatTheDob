# WhatTheDob

Blazor Server web app for browsing the Penn State Behrend Dobbins dining hall menu and rating dishes. Data is scraped from the public PSU menu page, stored in SQLite via EF Core, and exposed through a single-page experience with anonymous session-based ratings.

## Why this exists
The dining hall at my college is notorious for hit-or-miss (mostly miss) food. I built this so students can rate each item 1–5 and help each other decide what to grab or avoid. It started as a Dobbins-only rater and grew to support multiple campuses and dining halls.

## Architecture at a Glance
- Web UI: Blazor Server app in [src/WhatTheDob.Web](src/WhatTheDob.Web). Handles routing, UI, dependency injection, logging, and the background fetch timer.
- Application: Contracts in [src/WhatTheDob.Application](src/WhatTheDob.Application) defining services and background jobs used by the UI.
- Domain: Menu entities in [src/WhatTheDob.Domain](src/WhatTheDob.Domain) (campus, meal, menu, menu item).
- Infrastructure: EF Core + HtmlAgilityPack implementations in [src/WhatTheDob.Infrastructure](src/WhatTheDob.Infrastructure) for scraping, mapping, persistence, and ratings.

### Documentation assets
- ERD, package-style overview, and source diagram: see [documentation](documentation) (includes exported images plus the editable drawio file).

### Data flow
1. Menu fetch: `MenuService` calls the PSU menu page through `MenuApiClient`, parses HTML with `MenuFilterMapper`/`MenuItemMapper`, then upserts campuses, meals, menus, and items via `MenuRepository` into SQLite.
2. User browsing: the Menu page loads campuses/meals from SQLite and fetches a menu for the selected date/campus/meal.
3. Rating: a session cookie (created by `SessionCookieMiddleware`) ties anonymous users to their ratings. Ratings update aggregates in SQLite and persist a lightweight cookie so the UI can show the user's stars locally.
4. Background refresh: `DailyMenuJob` schedules a midnight fetch for a configurable future date offset.

## Quick start
Prerequisites: .NET 9 SDK.

```bash
# From repo root
dotnet restore WhatTheDob.sln
dotnet run --project src/WhatTheDob.Web/WhatTheDob.Web.csproj
# App will start on the Kestrel HTTP/HTTPS ports shown in the console
```

On first run, the app will create:
- `datastorage/WhatTheDob.db` (SQLite DB with menus, categories, ratings)
- `logstorage/whatthedobweb-*.log` (Serilog rolling logs)

## Configuration (appsettings.json)
- ConnectionStrings.WhatTheDob: SQLite connection string (relative paths resolve under `datastorage`).
- DataStorage.DataDirectory: folder for the SQLite file when a relative path is used.
- SessionCookie: `CookieKey`, `DaysToExpire` for anonymous session IDs.
- MenuFetch: `InitialFetch` (run fetch on startup), `SelectedCampus` default campus ID, `Meals` list to include, `DaysToFetch` (days offset used by the scheduled job), `MenuApiUrl` source endpoint.
- TagMappings: friendly abbreviations for dietary tags displayed in the UI.
- Serilog: log directory, sinks, and levels. Files go under `logstorage` by default.

### Common tweaks
- Skip startup fetch: set `MenuFetch:InitialFetch` to `false` if repeatedly re-running.
- Change data/log locations: update `DataStorage:DataDirectory` or `Serilog:LogDirectory`.

## Key components (for reviewers)
- Startup and DI: [src/WhatTheDob.Web/Program.cs](src/WhatTheDob.Web/Program.cs)
- UI page: [src/WhatTheDob.Web/Components/Pages/Menu.razor](src/WhatTheDob.Web/Components/Pages/Menu.razor) and code-behind.
- Persistence: [src/WhatTheDob.Infrastructure/Persistence/WhatTheDobDbContext.cs](src/WhatTheDob.Infrastructure/Persistence/WhatTheDobDbContext.cs) and [src/WhatTheDob.Infrastructure/Persistence/Repositories/MenuRepository.cs](src/WhatTheDob.Infrastructure/Persistence/Repositories/MenuRepository.cs).
- Scraping/parsing: [src/WhatTheDob.Infrastructure/Services/External/MenuApiClient.cs](src/WhatTheDob.Infrastructure/Services/External/MenuApiClient.cs), [src/WhatTheDob.Infrastructure/Mapping/MenuFilterMapper.cs](src/WhatTheDob.Infrastructure/Mapping/MenuFilterMapper.cs), [src/WhatTheDob.Infrastructure/Mapping/MenuItemMapper.cs](src/WhatTheDob.Infrastructure/Mapping/MenuItemMapper.cs).
- Background job and sessions: [src/WhatTheDob.Infrastructure/Services/BackgroundTasks/DailyMenuJob.cs](src/WhatTheDob.Infrastructure/Services/BackgroundTasks/DailyMenuJob.cs), [src/WhatTheDob.Web/Middleware/SessionCookieMiddleware.cs](src/WhatTheDob.Web/Middleware/SessionCookieMiddleware.cs).

## Operational notes
- Remote dependency: menu data comes from `MenuFetch:MenuApiUrl`; if that endpoint is down, initial load and scheduled fetches will log errors but the site stays up.
- Ratings: 1–5 star ratings are per-session. If cookies are cleared, prior session ratings will not show as “yours” but aggregates remain.
- Scheduling: `DailyMenuJob` uses an in-process timer; for production consider a durable scheduler if the host can recycle.

## Possible review focuses
- Data validation and error handling in `MenuService` and `MenuRepository`.
- HTML parsing resilience in `MenuItemMapper`/`MenuFilterMapper` when the source page structure changes.
- Concurrency on rating upserts in SQLite.
- UX/ARIA/accessibility of the Menu page and star rating control.
