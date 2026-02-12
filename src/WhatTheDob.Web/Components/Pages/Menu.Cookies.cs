using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace WhatTheDob.Web.Components.Pages
{
    public partial class Menu
    {
        private const string RatingCookiePrefix = "wtd-ur-"; // user rating cookie prefix

        private void LoadUserRatingsFromCookies()
        {
            var context = HttpContextAccessor.HttpContext;
            if (context == null) return;

            var reqCookies = context.Request.Cookies;
            foreach (var kvp in reqCookies)
            {
                var key = kvp.Key;
                if (!key.StartsWith(RatingCookiePrefix, StringComparison.Ordinal)) continue;

                var raw = kvp.Value;
                if (string.IsNullOrEmpty(raw)) continue;

                var parts = raw.Split(':', 2);
                if (parts.Length != 2)
                {
                    DeleteCookie(key);
                    continue;
                }

                var cookieSessionId = parts[0];
                if (string.IsNullOrEmpty(cookieSessionId) || cookieSessionId != _sessionId)
                {
                    DeleteCookie(key);
                    continue;
                }

                if (!int.TryParse(parts[1], out var rating) || rating < 1 || rating > 5)
                {
                    DeleteCookie(key);
                    continue;
                }

                var encodedItemValue = key.Substring(RatingCookiePrefix.Length);
                var itemValue = System.Net.WebUtility.UrlDecode(encodedItemValue);
                if (string.IsNullOrWhiteSpace(itemValue))
                {
                    DeleteCookie(key);
                    continue;
                }

                _userRatings[itemValue] = rating;
            }
        }

        private void SaveUserRatingToCookies(string itemValue, int rating)
        {
            var context = HttpContextAccessor.HttpContext;
            if (string.IsNullOrEmpty(_sessionId)) return;

            var encodedItemValue = System.Net.WebUtility.UrlEncode(itemValue);
            var key = RatingCookiePrefix + encodedItemValue;
            var value = _sessionId + ":" + rating;

            var isHttps = context?.Request?.IsHttps == true;
            var secureFlag = isHttps ? "Secure; " : string.Empty;
            var cookieString = $"{key}={value}; {secureFlag}SameSite=Lax; Path=/; Max-Age={(int)TimeSpan.FromDays(7).TotalSeconds}";
            JS.InvokeVoidAsync("eval", $"document.cookie = '{cookieString.Replace("'", "\\'")}'");
        }

        private void CleanupMismatchedRatingCookies()
        {
            var context = HttpContextAccessor.HttpContext;
            if (context == null) return;

            foreach (var kvp in context.Request.Cookies)
            {
                var key = kvp.Key;
                if (!key.StartsWith(RatingCookiePrefix, StringComparison.Ordinal)) continue;

                var raw = kvp.Value;

                if (string.IsNullOrEmpty(raw))
                {
                    DeleteCookie(key);
                    continue;
                }

                var parts = raw.Split(':', 2);
                if (parts.Length != 2 || parts[0] != _sessionId)
                {
                    DeleteCookie(key);
                }
            }
        }

        // May not run post server-side render, maybe run through js interop if it becomes a problem
        private void DeleteCookie(string key)
        {
            var context = HttpContextAccessor.HttpContext;
            if (context == null) return;
            context.Response.Cookies.Delete(key);
        }
    }
}
