using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Application.Interfaces.Services.External;
using WhatTheDob.Domain.Entities;
using WhatTheDob.Infrastructure.Interfaces.Mapping;
using WhatTheDob.Infrastructure.Interfaces.Persistence;
using WhatTheDob.Infrastructure.Persistence.Models;
using WhatTheDob.Infrastructure.Services;
using Xunit;

namespace WhatTheDob.Infrastructure.Tests;

public class MenuServiceTests
{
    private static MenuService CreateService(
        IMenuRepository repository,
        ILogger<MenuService>? logger = null,
        IRatingThrottleService? throttleService = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MenuFetch:DaysToFetch"] = "1",
            ["MenuFetch:MenuApiUrl"] = "http://test",
            ["MenuFetch:SelectedCampus"] = "1"
        }).Build();

        throttleService ??= Mock.Of<IRatingThrottleService>(s => s.IsAllowedAndRecord(It.IsAny<string>()) == true);

        return new MenuService(
            configuration,
            Mock.Of<IMenuApiClient>(),
            repository,
            Mock.Of<IMenuItemMapper>(),
            Mock.Of<IMenuFilterMapper>(),
            logger ?? Mock.Of<ILogger<MenuService>>(),
            throttleService);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SubmitUserRatingAsync_rejects_missing_session(string? session)
    {
        var repoMock = new Mock<IMenuRepository>(MockBehavior.Strict);
        var service = CreateService(repoMock.Object);

        var result = await service.SubmitUserRatingAsync(session!, "Item", 3);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Session id is required.");
        repoMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SubmitUserRatingAsync_rejects_missing_item_value(string? item)
    {
        var repoMock = new Mock<IMenuRepository>(MockBehavior.Strict);
        var service = CreateService(repoMock.Object);

        var result = await service.SubmitUserRatingAsync("session", item!, 3);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Item value is required.");
        repoMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task SubmitUserRatingAsync_rejects_out_of_range_rating(int rating)
    {
        var repoMock = new Mock<IMenuRepository>(MockBehavior.Strict);
        var service = CreateService(repoMock.Object);

        var result = await service.SubmitUserRatingAsync("session", "Item", rating);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Rating must be between 1 and 5.");
        repoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SubmitUserRatingAsync_rejects_when_throttled()
    {
        var repoMock = new Mock<IMenuRepository>(MockBehavior.Strict);
        var throttleMock = new Mock<IRatingThrottleService>();
        throttleMock.Setup(t => t.IsAllowedAndRecord("session")).Returns(false);
        var service = CreateService(repoMock.Object, throttleService: throttleMock.Object);

        var result = await service.SubmitUserRatingAsync("session", "Item", 3);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Rate limit exceeded. Please wait before submitting more ratings.");
        repoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SubmitUserRatingAsync_returns_success_for_valid_request()
    {
        var repoMock = new Mock<IMenuRepository>();
        repoMock.Setup(r => r.UpsertUserRatingAsync("session", "Item", 5, default)).Returns(Task.CompletedTask);
        var service = CreateService(repoMock.Object);

        var result = await service.SubmitUserRatingAsync("session", "Item", 5);

        result.IsSuccess.Should().BeTrue();
        result.FailureReason.Should().BeNull();
        repoMock.Verify(r => r.UpsertUserRatingAsync("session", "Item", 5, default), Times.Once);
    }

    [Fact]
    public async Task SubmitUserRatingAsync_returns_failure_when_repository_throws()
    {
        var repoMock = new Mock<IMenuRepository>();
        repoMock.Setup(r => r.UpsertUserRatingAsync("session", "Item", 4, default))
            .ThrowsAsync(new System.Exception("db unavailable"));

        var service = CreateService(repoMock.Object);

        var result = await service.SubmitUserRatingAsync("session", "Item", 4);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Failed to submit rating. Please try again.");
        repoMock.Verify(r => r.UpsertUserRatingAsync("session", "Item", 4, default), Times.Once);
    }

    [Fact]
    public async Task GetMenuAsync_returns_null_when_no_mappings_found()
    {
        var repoMock = new Mock<IMenuRepository>();
        repoMock.Setup(r => r.GetMenuMappingsAsync("01/01/25", 1, 2, default)).ReturnsAsync(Enumerable.Empty<MenuMapping>());
        var service = CreateService(repoMock.Object);

        var result = await service.GetMenuAsync("01/01/25", 1, 2);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMenuAsync_maps_items_from_repository()
    {
        var repoMock = new Mock<IMenuRepository>();
        repoMock.Setup(r => r.GetMenuMappingsAsync("01/01/25", 1, 2, default)).ReturnsAsync(new List<MenuMapping>
        {
            new()
            {
                Menu = new Persistence.Models.Menu { Date = "01/01/25", CampusId = 1, MealId = 2 },
                MenuItem = new Persistence.Models.MenuItem
                {
                    Value = "Pasta",
                    Category = new Category { Value = "Entree" },
                    ItemRating = new ItemRating { TotalRating = 10, RatingCount = 3 },
                    Tags = "Vegan,Gluten Free"
                }
            }
        });

        var service = CreateService(repoMock.Object);

        var result = await service.GetMenuAsync("01/01/25", 1, 2);

        result.Should().NotBeNull();
        result!.Date.Should().Be("01/01/25");
        result.CampusId.Should().Be(1);
        result.Meal.Should().Be("2");
        result.Items.Should().ContainSingle();
        var item = result.Items.Single();
        item.Value.Should().Be("Pasta");
        item.Category.Should().Be("Entree");
        item.Tags.Should().BeEquivalentTo(new[] { "Vegan", "Gluten Free" });
        item.TotalRating.Should().Be(10);
        item.RatingCount.Should().Be(3);
    }
}
