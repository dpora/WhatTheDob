using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Application.Interfaces.Services.BackgroundTasks;
using WhatTheDob.Application.Interfaces.Services.External;
using WhatTheDob.Infrastructure.Interfaces.Mapping;
using WhatTheDob.Infrastructure.Interfaces.Persistence;
using WhatTheDob.Infrastructure.Mapping;
using WhatTheDob.Infrastructure.Persistence;
using WhatTheDob.Infrastructure.Persistence.Repositories;
using WhatTheDob.Infrastructure.Services;
using WhatTheDob.Infrastructure.Services.BackgroundTasks;
using WhatTheDob.Infrastructure.Services.External;
using WhatTheDob.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// Get data storage path from configuration
var dataStoragePath = builder.Configuration.GetValue<string>("DataStorage:Path") ?? "datastorage";
var repoRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var fullDataStoragePath = Path.Combine(repoRoot, dataStoragePath);

// Update connection string to use the data storage path
var connectionString = builder.Configuration.GetConnectionString("WhatTheDob");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    var connBuilder = new SqliteConnectionStringBuilder(connectionString);
    var dataSource = connBuilder.DataSource;
    
    // If the data source is relative, combine it with the data storage path
    if (!Path.IsPathRooted(dataSource))
    {
        var fullDbPath = Path.Combine(fullDataStoragePath, dataSource);
        connBuilder.DataSource = fullDbPath;
        connectionString = connBuilder.ToString();
    }
}

builder.Services.AddDbContext<WhatTheDobDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IMenuItemMapper, MenuItemMapper>();
builder.Services.AddScoped<IMenuFilterMapper, MenuFilterMapper>();
builder.Services.AddHttpClient<IMenuApiClient, MenuApiClient>();
builder.Services.AddSingleton<IDailyMenuJob, DailyMenuJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Create DB if it doesn't exist
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<WhatTheDobDbContext>();

// Ensure the database directory exists
var dbConnection = db.Database.GetConnectionString();
if (!string.IsNullOrWhiteSpace(dbConnection))
{
    var connBuilder = new SqliteConnectionStringBuilder(dbConnection);
    var dbPath = connBuilder.DataSource;
    
    var directory = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

// Ensure the database is created
await db.Database.EnsureCreatedAsync();

// Check if initial fetch is required
var initialFetch = builder.Configuration.GetValue<bool?>("MenuFetch:InitialFetch") ?? false;
if (initialFetch)
{
    // Grab all menus from API on startup and insert into DB
    var menuService = scope.ServiceProvider.GetRequiredService<IMenuService>();
    await menuService.FetchMenusFromApiAsync().ConfigureAwait(false);
}

// Schedule daily menu fetch task.
// NOTE: In this context, "MenuFetch:DaysToFetch" is interpreted as a *days offset* from today
// (i.e. fetch the menu for the date 'today + offset'), not as "number of days to fetch".
// The configuration key name is kept for backwards compatibility with existing deployments.
var dailyFetchDaysOffset = builder.Configuration.GetValue<int?>("MenuFetch:DaysToFetch") ?? 7;
var job = scope.ServiceProvider.GetRequiredService<IDailyMenuJob>();
// Each day at midnight, fetch the menu for the date 'dailyFetchDaysOffset + today'
job.ScheduleDailyTask(dailyFetchDaysOffset);

// Use custom middleware to manage session cookies, i.e. session identifiers for users
app.UseMiddleware<WhatTheDob.Web.Middleware.SessionCookieMiddleware>();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();