using WhatTheDob.API;

using MenuParser = WhatTheDob.API.MenuParser;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var parser = new MenuParser(app.Configuration);
await parser.GetMenusPageAsync();
Console.WriteLine("");
app.MapGet("/getMenu", async () =>
{
    
})
.WithName("GetMenu");

app.Run();
