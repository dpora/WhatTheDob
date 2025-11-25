using WhatTheDob.Core.Services;
using WhatTheDob.Core.Services.External;
using WhatTheDob.Core.Mapping;
using WhatTheDob.Core.Services.BackgroundTasks;
using WhatTheDob.Infrastructure.Services;
using WhatTheDob.Infrastructure.Services.External;
using WhatTheDob.Infrastructure.Mapping;
using WhatTheDob.Infrastructure.Services.BackgroundTasks;

var builder = WebApplication.CreateBuilder(args);

//Add services to the container.
//Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register infrastructure implementations for Core interfaces
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IMenuApiClient, MenuApiClient>();
builder.Services.AddScoped<IMenuItemMapper, MenuItemMapper>();
builder.Services.AddScoped<IMenuFilterMapper, MenuFilterMapper>();
builder.Services.AddScoped<IDailyMenuJob, DailyMenuJob>();

var app = builder.Build();

//Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/getMenu", async (IMenuService menuService) =>
{
    var menus = await menuService.GetMenuPagesAsync();
    return Results.Ok(menus);
})
.WithName("GetMenu");

app.Run();
