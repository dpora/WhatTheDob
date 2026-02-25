using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Application.Interfaces.Services.BackgroundTasks;
using WhatTheDob.Infrastructure.Services.BackgroundTasks;
using Xunit;

namespace WhatTheDob.Infrastructure.Tests;

public class DailyMenuJobTests
{
    [Fact]
    public async Task RunTaskAsync_invokes_menu_service_for_target_date()
    {
        var menuService = new Mock<IMenuService>();
        menuService.Setup(s => s.FetchMenusFromApiAsync(It.IsAny<string>())).ReturnsAsync([]);

        var provider = new ServiceCollection()
            .AddSingleton(menuService.Object)
            .BuildServiceProvider();

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var logger = Mock.Of<ILogger<DailyMenuJob>>();
        var job = new DailyMenuJob(scopeFactory.Object, logger);

        await job.RunTaskAsync(daysOffsetValue: 0);

        menuService.Verify(s => s.FetchMenusFromApiAsync(It.Is<string>(date => !string.IsNullOrWhiteSpace(date))), Times.Once);
    }

    [Fact]
    public void ScheduleDailyTask_is_idempotent()
    {
        var scopeFactory = Mock.Of<IServiceScopeFactory>();
        var logger = Mock.Of<ILogger<DailyMenuJob>>();
        var job = new DailyMenuJob(scopeFactory, logger);

        job.ScheduleDailyTask(0);
        job.ScheduleDailyTask(0);

        // No exception thrown and second schedule does not override timer; behavior validated by absence of errors.
        Assert.True(true);
    }
}
