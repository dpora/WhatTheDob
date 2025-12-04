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

var builder = WebApplication.CreateBuilder(args);

//Add services to the container.
//Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register infrastructure implementations for Core interfaces
builder.Services.AddDbContext<WhatTheDobDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("WhatTheDob")));
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
        Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "database"));
        db.Database.EnsureCreated();
    }

    var job = scope.ServiceProvider.GetRequiredService<IDailyMenuJob>();
    job.ScheduleMidnightTask();
    await job.RunTaskAsync();
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

app.Run();
