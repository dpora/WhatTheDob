
using Microsoft.AspNetCore.Http;

namespace WhatTheDob.Application.Interfaces.Middleware
{
    /// <summary>
    /// Middleware interface for managing session cookies. 
    /// </summary>
    public interface ISessionCookieMiddleware
    {
        Task InvokeAsync(HttpContext context);
    }
}
