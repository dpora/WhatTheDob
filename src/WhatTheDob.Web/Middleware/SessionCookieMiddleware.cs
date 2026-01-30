using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace WhatTheDob.Web.Middleware
{
    public class SessionCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _cookieKey;
        private readonly int _daysToExpire;

        public SessionCookieMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            var cookieSettings = configuration.GetSection("SessionCookie");

            _next = next;
            _cookieKey = cookieSettings.GetValue<string>("CookieKey", "UserSessionId");
            _daysToExpire = cookieSettings.GetValue<int>("DaysToExpire", 7);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the cookie already exists in the Request
            if (!context.Request.Cookies.ContainsKey(_cookieKey))
            {
                // Generate a new unique ID
                string sessionId = Guid.NewGuid().ToString();

                // Configure cookie options
                var options = new CookieOptions
                {
                    HttpOnly = true,    // Prevents JavaScript access (good for XSS protection)
                    Secure = true,      // Ensures cookie is only sent over HTTPS
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
                context.Items[_cookieKey] = context.Request.Cookies[_cookieKey];
            }

            // Move to the next piece of middleware in the pipeline
            await _next(context);
        }
    }
}
