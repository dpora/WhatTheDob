using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WhatTheDob.Infrastructure.Interfaces.Mapping;
using WhatTheDob.Infrastructure.Interfaces.Persistence;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Application.Interfaces.Services.BackgroundTasks;
using WhatTheDob.Application.Interfaces.Services.External;
using WhatTheDob.Infrastructure.Persistence;
using WhatTheDob.Infrastructure.Services;
using WhatTheDob.Infrastructure.Services.External;
using WhatTheDob.Infrastructure.Mapping;
using WhatTheDob.Infrastructure.Services.BackgroundTasks;
using WhatTheDob.Infrastructure.Persistence.Repositories;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/whatthedobapi-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

Log.Information("Starting WhatTheDob API application");

//Add services to the container.
//Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Get data storage path from configuration
var dataStoragePath = builder.Configuration.GetValue<string>("DataStorage:Path") ?? "datastorage";
var repoRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var fullDataStoragePath = Path.Combine(repoRoot, dataStoragePath);

Log.Information("Data storage path configured: {FullDataStoragePath}", fullDataStoragePath);

// Update connection string to use the data storage path
var connectionString = builder.Configuration.GetConnectionString("WhatTheDob");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    var connBuilder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    var dataSource = connBuilder.DataSource;
    
    // If the data source is relative, combine it with the data storage path
    if (!Path.IsPathRooted(dataSource))
    {
        var fullDbPath = Path.Combine(fullDataStoragePath, dataSource);
        connBuilder.DataSource = fullDbPath;
        connectionString = connBuilder.ToString();
    }
}

// Register infrastructure implementations for Core interfaces
builder.Services.AddDbContext<WhatTheDobDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddHttpClient<IMenuApiClient, MenuApiClient>();
builder.Services.AddScoped<IMenuItemMapper, MenuItemMapper>();
builder.Services.AddScoped<IMenuFilterMapper, MenuFilterMapper>();
builder.Services.AddSingleton<IDailyMenuJob, DailyMenuJob>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (app.Environment.IsDevelopment())
    {
        var db = scope.ServiceProvider.GetRequiredService<WhatTheDobDbContext>();
        
        // Ensure the database directory exists
        var dbConnection = db.Database.GetConnectionString();
        if (!string.IsNullOrWhiteSpace(dbConnection))
        {
            var connBuilder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(dbConnection);
            var dbPath = connBuilder.DataSource;
            
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        db.Database.EnsureCreated();
        Log.Information("Database created/verified successfully");
    }

    var dailyFetchDaysOffset = builder.Configuration.GetValue<int?>("MenuFetch:DaysToFetch") ?? 7;
    var job = scope.ServiceProvider.GetRequiredService<IDailyMenuJob>();
    job.ScheduleDailyTask(dailyFetchDaysOffset);
    Log.Information("Daily menu fetch job scheduled with {DaysOffset} days offset", dailyFetchDaysOffset);
}

//Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/getMenu", async (IMenuService menuService) =>
{
    var menus = await menuService.GetMenuAsync("12/03/25", 46, 3);
    return Results.Ok(menus);
})
.WithName("GetMenu");

Log.Information("WhatTheDob API application configured and ready to run");

try
{
    app.Run();
    Log.Information("WhatTheDob API application shut down gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "WhatTheDob API application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
