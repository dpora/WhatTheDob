using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Domain.Entities;
using WhatTheDob.Infrastructure.Interfaces.Persistence;
using Xunit;

namespace WhatTheDob.Web.IntegrationTests;

public class MenuIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MenuIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Home_page_sets_session_cookie()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Any(c => c.Contains("UserSessionId")).Should().BeTrue();
    }

    [Fact]
    public async Task Menu_service_returns_seeded_menu()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMenuRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IMenuService>();

        await repo.UpsertCampusesAsync(new Dictionary<int, string> { { 46, "Main" } });
        await repo.UpsertMealsAsync(new[] { "Lunch" });

        var menu = new Menu("01/01/25", "Lunch", [ new MenuItem("Pasta", ["Vegan"], "Entree", 0, 0) ], 46);
        await repo.UpsertMenusAsync(new[] { menu });

        var mealId = (await repo.GetMealsAsync()).Single(m => m.Value == "Lunch").Id;
        var result = await service.GetMenuAsync("01/01/25", 46, mealId);

        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle(i => i.Value == "Pasta" && i.Category == "Entree");
    }
}
