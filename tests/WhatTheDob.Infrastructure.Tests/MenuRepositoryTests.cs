using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WhatTheDob.Domain.Entities;
using WhatTheDob.Infrastructure.Persistence.Models;
using WhatTheDob.Infrastructure.Persistence.Repositories;
using WhatTheDob.Infrastructure.Tests.Support;
using Xunit;

namespace WhatTheDob.Infrastructure.Tests;

public class MenuRepositoryTests
{
    [Fact]
    public async Task UpsertCampusesAsync_inserts_and_updates_records()
    {
        using var harness = new SqliteInMemoryContext();
        var repo = new MenuRepository(harness.Context, NullLogger<MenuRepository>.Instance);

        await repo.UpsertCampusesAsync(new Dictionary<int, string> { { 46, "Main" } });
        var campuses = await repo.GetCampusesAsync();
        campuses.Should().ContainSingle(c => c.Id == 46 && c.Value == "Main");

        await repo.UpsertCampusesAsync(new Dictionary<int, string> { { 46, "Renamed" } });
        var updated = await repo.GetCampusesAsync();
        updated.Should().ContainSingle(c => c.Id == 46 && c.Value == "Renamed");
    }

    [Fact]
    public async Task UpsertMealsAsync_adds_distinct_meals_and_skips_blanks()
    {
        using var harness = new SqliteInMemoryContext();
        var repo = new MenuRepository(harness.Context, NullLogger<MenuRepository>.Instance);

        await repo.UpsertMealsAsync(new[] { "Breakfast", "Lunch", "lunch", "   " });
        var meals = await repo.GetMealsAsync();

        meals.Should().HaveCount(2);
        meals.Select(m => m.Value).Should().Contain(new[] { "Breakfast", "Lunch" });
    }

    [Fact]
    public async Task UpsertUserRatingAsync_creates_and_updates_user_rating_and_totals()
    {
        using var harness = new SqliteInMemoryContext();
        var context = harness.Context;
        context.ItemRatings.Add(new ItemRating { Value = "Pasta", TotalRating = 2, RatingCount = 1 });
        await context.SaveChangesAsync();

        var repo = new MenuRepository(context, NullLogger<MenuRepository>.Instance);

        await repo.UpsertUserRatingAsync("session-1", "Pasta", 5);
        var rating = context.ItemRatings.Single(r => r.Value == "Pasta");
        rating.TotalRating.Should().Be(7);
        rating.RatingCount.Should().Be(2);

        await repo.UpsertUserRatingAsync("session-1", "Pasta", 3);
        rating.TotalRating.Should().Be(5);
        rating.RatingCount.Should().Be(2);
        context.UserRatings.Single().RatingValue.Should().Be(3);
    }

    [Fact]
    public async Task UpsertMenusAsync_persists_menu_and_item_mappings()
    {
        using var harness = new SqliteInMemoryContext();
        var repo = new MenuRepository(harness.Context, NullLogger<MenuRepository>.Instance);

        await repo.UpsertMealsAsync(new[] { "Lunch" });
        await repo.UpsertCampusesAsync(new Dictionary<int, string> { { 46, "Main" } });

        var menu = new Domain.Entities.Menu(
            "01/01/25",
            "Lunch",
            [ new Domain.Entities.MenuItem("Pasta", ["Vegan"], "Entree", 0, 0) ],
            46);

        await repo.UpsertMenusAsync(new[] { menu });

        var persistedMenu = await repo.GetMenuAsync("01/01/25", 46, harness.Context.Meals.Single().Id);
        persistedMenu.Should().NotBeNull();

        var mappings = await repo.GetMenuMappingsAsync(persistedMenu!.Id);
        mappings.Should().ContainSingle();
        mappings.Single().MenuItem.Value.Should().Be("Pasta");
    }
}
