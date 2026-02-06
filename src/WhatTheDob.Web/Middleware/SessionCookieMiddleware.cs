using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WhatTheDob.Web.Middleware
{
    public class SessionCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _cookieKey;
        private readonly int _daysToExpire;
        private readonly ILogger<SessionCookieMiddleware> _logger;

        public SessionCookieMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<SessionCookieMiddleware> logger)
        {
            var cookieSettings = configuration.GetSection("SessionCookie");

            _next = next;
            _cookieKey = cookieSettings.GetValue<string>("CookieKey", "UserSessionId");
            _daysToExpire = cookieSettings.GetValue<int>("DaysToExpire", 7);
            _logger = logger;
            
            _logger.LogDebug("SessionCookieMiddleware initialized with CookieKey={CookieKey}, DaysToExpire={DaysToExpire}", _cookieKey, _daysToExpire);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Check if the cookie already exists in the Request
                if (!context.Request.Cookies.ContainsKey(_cookieKey))
                {
                    // Generate a new unique ID
                    string sessionId = Guid.NewGuid().ToString();
                    _logger.LogInformation("Creating new session cookie with SessionId={SessionId}", sessionId);

                    // Configure cookie options
                    var options = new CookieOptions
                    {
                        HttpOnly = true,    // Prevents JavaScript access (good for XSS protection)
                        Secure = context.Request.IsHttps,      // Ensures cookie is only sent over HTTPS
                        SameSite = SameSiteMode.Strict, // Mitigates CSRF attacks
                        Expires = DateTimeOffset.UtcNow.AddDays(_daysToExpire) // From app settings
                    };

                    // Append to the Response so the browser saves it
                    context.Response.Cookies.Append(_cookieKey, sessionId, options);

                    // Add to Items so subsequent middleware can use it immediately
                    context.Items[_cookieKey] = sessionId;
                }
                else
                {
                    // Already exists, pull it into Items for easy access in Controllers
                    var existingSessionId = context.Request.Cookies[_cookieKey];
                    context.Items[_cookieKey] = existingSessionId;
                    _logger.LogDebug("Using existing session cookie with SessionId={SessionId}", existingSessionId);
                }

                // Move to the next piece of middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SessionCookieMiddleware while processing session cookie");
                throw;
            }
        }
    }
}
