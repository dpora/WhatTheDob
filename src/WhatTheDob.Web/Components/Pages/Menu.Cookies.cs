using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace WhatTheDob.Web.Components.Pages
{
    public partial class Menu
    {
        private const string RatingCookiePrefix = "wtd-ur-"; // user rating cookie prefix

        private async Task LoadUserRatingsFromCookiesAsync()
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

        private async Task SaveUserRatingToCookiesAsync(string itemValue, int rating)
        {
            var context = HttpContextAccessor.HttpContext;
            if (string.IsNullOrEmpty(_sessionId)) return;

            var encodedItemValue = System.Net.WebUtility.UrlEncode(itemValue);
            var key = RatingCookiePrefix + encodedItemValue;
            var value = _sessionId + ":" + rating;

            var isHttps = context?.Request?.IsHttps == true;
            await JS.InvokeVoidAsync("CookieStorageAccessor.setCookie", key, value, (int)TimeSpan.FromDays(7).TotalDays, isHttps);
        }

        private async Task CleanupMismatchedRatingCookiesAsync()
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
