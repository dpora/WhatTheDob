using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WhatTheDob.Web.Middleware;
using Xunit;

namespace WhatTheDob.Web.IntegrationTests;

public class SessionCookieMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_creates_cookie_when_missing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SessionCookie:CookieKey"] = "UserSessionId",
            ["SessionCookie:DaysToExpire"] = "1"
        }).Build();

        var context = new DefaultHttpContext();
        var middleware = new SessionCookieMiddleware(_ => Task.CompletedTask, config, NullLogger<SessionCookieMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContainsKey("Set-Cookie").Should().BeTrue();
        context.Items.TryGetValue("UserSessionId", out var session).Should().BeTrue();
        session.Should().NotBeNull();
    }
}
