using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WhatTheDob.Infrastructure.Services.External;
using Xunit;

namespace WhatTheDob.Infrastructure.Tests;

public class MenuApiClientTests
{
    [Fact]
    public async Task GetMenuDataAsync_uses_get_without_filters()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var api = new MenuApiClient(client, NullLogger.Instance);

        var content = await api.GetMenuDataAsync("http://example.com/menu");

        content.Should().Be("ok");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri.Should().Be("http://example.com/menu");
    }

    [Fact]
    public async Task GetMenuDataAsync_posts_when_filters_present()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var api = new MenuApiClient(client, NullLogger.Instance);

        var content = await api.GetMenuDataAsync("http://example.com/menu", "01/01/25", "Lunch", 46);

        content.Should().Be("ok");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        body.Should().Contain("selMenuDate=01%2F01%2F25");
        body.Should().Contain("selMeal=Lunch");
        body.Should().Contain("selCampus=46");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class NullLogger : ILogger<MenuApiClient>
    {
        public static readonly NullLogger Instance = new();
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, Func<TState, System.Exception?, string> formatter) { }
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}
