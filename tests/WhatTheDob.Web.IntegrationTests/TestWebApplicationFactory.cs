using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using WhatTheDob.Application.Interfaces.Services.BackgroundTasks;
using WhatTheDob.Application.Interfaces.Services.External;
using WhatTheDob.Infrastructure.Persistence;
using WhatTheDob.Infrastructure.Services.External;
using WhatTheDob.Web;

namespace WhatTheDob.Web.IntegrationTests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WhatTheDobDbContext>));
            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<WhatTheDobDbContext>(options => options.UseSqlite(_connection));

            ReplaceService<IDailyMenuJob>(services, ServiceLifetime.Singleton, _ => new NoOpDailyMenuJob());
            ReplaceService<IMenuApiClient>(services, ServiceLifetime.Singleton, _ => new FakeMenuApiClient());

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WhatTheDobDbContext>();
            db.Database.EnsureCreated();
        });
    }

    private static void ReplaceService<T>(IServiceCollection services, ServiceLifetime lifetime, Func<IServiceProvider, T> factory)
        where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.Add(new ServiceDescriptor(typeof(T), factory, lifetime));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }

    private sealed class FakeMenuApiClient : IMenuApiClient
    {
        public Task<string> GetMenuDataAsync(string url, string? menuDate = null, string? meal = null, int? campusId = null)
            => Task.FromResult("<html></html>");
    }

    private sealed class NoOpDailyMenuJob : IDailyMenuJob
    {
        public Task RunTaskAsync(int daysOffsetValue) => Task.CompletedTask;
        public void ScheduleDailyTask(int daysOffsetValue) { }
    }
}
