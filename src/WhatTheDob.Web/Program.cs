using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Application.Interfaces.Services.External;
using WhatTheDob.Infrastructure.Interfaces.Mapping;
using WhatTheDob.Infrastructure.Interfaces.Persistence;
using WhatTheDob.Infrastructure.Mapping;
using WhatTheDob.Infrastructure.Persistence;
using WhatTheDob.Infrastructure.Persistence.Repositories;
using WhatTheDob.Infrastructure.Services;
using WhatTheDob.Infrastructure.Services.External;
using WhatTheDob.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<WhatTheDobDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("WhatTheDob")));

builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IMenuItemMapper, MenuItemMapper>();
builder.Services.AddScoped<IMenuFilterMapper, MenuFilterMapper>();
builder.Services.AddHttpClient<IMenuApiClient, MenuApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WhatTheDobDbContext>();
    var connectionString = builder.Configuration.GetConnectionString("WhatTheDob");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var dbPath = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(app.Environment.ContentRootPath, dataSource);

        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
