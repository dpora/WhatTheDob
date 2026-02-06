# WhatTheDob Logging Strategy

## Overview

The WhatTheDob project implements comprehensive structured logging using Serilog across all layers of the application. This document describes the logging implementation, log levels, and logging patterns used throughout the codebase.

## Logging Framework

**Framework:** Serilog  
**Version:** 3.1.x (via Serilog.AspNetCore 8.0.3)

## Configuration

### Serilog Configuration

Logging is configured in both `WhatTheDob.Web` and `WhatTheDob.API` projects via:
- **Code:** Program.cs bootstrap configuration
- **Settings:** appsettings.json for runtime configuration

### Log Outputs

1. **Console Sink**: Real-time logging to console
   - Format: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}`
   
2. **File Sink**: Rolling file logs
   - Web Application: `logs/whatthedobweb-.log`
   - API Application: `logs/whatthedobapi-.log`
   - Rolling Interval: Daily
   - Retention: 7 days
   - Format: Same as console

## Log Levels

The application uses the following log levels according to their semantic meaning:

| Level | Usage | Examples |
|-------|-------|----------|
| **Debug** | Detailed diagnostic information useful during development | HTTP request details, loop iterations, filter values |
| **Information** | General informational messages about application flow | Application startup/shutdown, menu fetch completion, successful operations |
| **Warning** | Potentially harmful situations that don't prevent operation | Invalid input, missing optional data, using fallback values |
| **Error** | Error events that might still allow the application to continue | Failed API calls, database operation failures, caught exceptions |
| **Fatal** | Very severe errors that lead to application termination | Unhandled exceptions in Program.cs, critical initialization failures |

### Default Log Levels by Namespace

```json
{
  "Default": "Information",
  "Microsoft": "Warning",
  "Microsoft.AspNetCore": "Warning",
  "Microsoft.EntityFrameworkCore": "Warning",
  "System": "Warning"
}
```

## Logging by Layer

### 1. Presentation Layer (Blazor - WhatTheDob.Web)

**Files:**
- `Components/Pages/Menu.razor`

**What is Logged:**
- Page initialization with session ID
- Filter loading (campuses and meals)
- Menu load requests with date, campus, meal parameters
- Rating submissions with session ID and item details
- All exceptions with full context

**Log Examples:**
```csharp
Logger.LogInformation("Menu page initialized with SessionId={SessionId}", sessionId);
Logger.LogInformation("Loading menu for Date={Date}, CampusId={CampusId}, MealId={MealId}", ...);
Logger.LogError(ex, "Failed to load menu for Date={Date}, CampusId={CampusId}, MealId={MealId}", ...);
```

### 2. Middleware Layer (WhatTheDob.Web)

**Files:**
- `Middleware/SessionCookieMiddleware.cs`

**What is Logged:**
- Middleware initialization configuration
- New session cookie creation
- Existing session cookie usage
- Session management errors

**Log Examples:**
```csharp
_logger.LogInformation("Creating new session cookie with SessionId={SessionId}", sessionId);
_logger.LogDebug("Using existing session cookie with SessionId={SessionId}", existingSessionId);
_logger.LogError(ex, "Error occurred in SessionCookieMiddleware");
```

### 3. Service Layer (WhatTheDob.Infrastructure/Services)

**Files:**
- `Services/MenuService.cs`
- `Services/External/MenuApiClient.cs`
- `Services/External/DailyMenuJob.cs`

**What is Logged:**
- Service initialization with configuration values
- Menu fetch operations (start, progress, completion, counts)
- API call details (method, URL, parameters, content length)
- Background job scheduling and execution
- All service-level errors with parameters

**Log Examples:**
```csharp
_logger.LogInformation("Starting menu fetch from API for {DaysToFetch} days", daysToFetch);
_logger.LogInformation("Successfully fetched {MenuCount} menus out of {TaskCount} attempts", ...);
_logger.LogError(ex, "Error fetching menu for Date={Date}, Meal={Meal}, Campus={CampusId}", ...);
```

### 4. Repository Layer (WhatTheDob.Infrastructure/Persistence)

**Files:**
- `Persistence/Repositories/MenuRepository.cs`

**What is Logged:**
- Database transaction lifecycle (begin, commit, rollback)
- Upsert operations with entity counts
- User rating operations (create, update)
- Database errors with entity context

**Log Examples:**
```csharp
_logger.LogInformation("Upserting {MenuCount} menus", menuCount);
_logger.LogDebug("Database transaction started for menu upsert");
_logger.LogInformation("Successfully committed transaction for {MenuCount} menus", menuCount);
_logger.LogError(ex, "Transaction rolled back during menu upsert for {MenuCount} menus", menuCount);
```

### 5. Application Configuration (Program.cs)

**Files:**
- `WhatTheDob.Web/Program.cs`
- `WhatTheDob.API/Program.cs`

**What is Logged:**
- Application startup
- Database configuration and initialization
- Initial menu fetch operations
- Background job scheduling
- Application shutdown (graceful or unexpected)

**Log Examples:**
```csharp
Log.Information("Starting WhatTheDob Web application");
Log.Information("Database created/verified successfully");
Log.Information("Initial menu fetch completed successfully");
Log.Fatal(ex, "WhatTheDob Web application terminated unexpectedly");
```

## Exception Handling Strategy

### User-Facing vs. Technical Errors

**Principle:** Users see friendly messages; logs contain technical details.

**Implementation:**
1. All exceptions are caught at appropriate boundaries
2. User-facing error messages are generic and helpful
3. Full exception details (stack trace, inner exceptions) are logged
4. Context parameters are always included in error logs

**Example:**
```csharp
catch (Exception ex)
{
    Logger.LogError(ex, "Failed to load menu for Date={Date}, CampusId={CampusId}, MealId={MealId}", 
        date, campusId, mealId);
    _errorMessage = "Failed to load menu. Please try again."; // User-friendly
}
```

## Structured Logging Patterns

### Template Parameters

Always use structured logging templates with parameters:

✅ **Correct:**
```csharp
_logger.LogInformation("Processing menu for {Date} at {Campus}", date, campusName);
```

❌ **Incorrect:**
```csharp
_logger.LogInformation($"Processing menu for {date} at {campusName}");
```

### Consistent Property Names

Use consistent property names across the application:
- `SessionId`: User session identifier
- `Date`: Menu date
- `CampusId`: Campus identifier
- `MealId`: Meal identifier
- `ItemValue`: Menu item name
- `Rating`: User rating value

## Security Considerations

### What NOT to Log

The following information is **never** logged:
- Passwords or authentication tokens
- Full credit card numbers or sensitive payment information
- Personal Identifiable Information (PII) beyond anonymous session IDs
- Database connection strings with credentials

### What IS Safe to Log

- Anonymous session identifiers (GUIDs)
- Menu item names and categories
- Aggregate rating values
- Campus and meal selections
- Dates and non-sensitive filter values

## Testing and Validation

### Verifying Logs

1. **Console Output**: Run the application and observe console logs
2. **File Output**: Check `logs/` directory for daily log files
3. **Log Levels**: Adjust log levels in appsettings.json to control verbosity

### Common Scenarios to Test

- Application startup and initialization
- Menu loading with various filter combinations
- Rating submissions (new and updates)
- Error conditions (network failures, invalid input)
- Background job execution

## Troubleshooting

### Logs Not Appearing

1. Check `appsettings.json` configuration
2. Verify log file permissions for `logs/` directory
3. Ensure Serilog is properly initialized in Program.cs

### Too Many Logs

1. Adjust log levels in appsettings.json
2. Set Microsoft namespaces to Warning or Error
3. Use filters to exclude noisy components

### Performance Impact

- Information level has minimal performance impact
- Debug level should be used sparingly in production
- File rolling occurs daily to manage disk space

## Maintenance

### Log File Management

- Files are automatically rolled daily
- Only 7 days of logs are retained
- Manual cleanup is not required

### Log Monitoring

Consider implementing:
- Log aggregation tools (ELK stack, Seq, Application Insights)
- Alerting on Error and Fatal level messages
- Dashboard for key metrics from logs

## References

- [Serilog Documentation](https://serilog.net/)
- [ASP.NET Core Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/)
- [Structured Logging Best Practices](https://github.com/serilog/serilog/wiki/Structured-Data)
