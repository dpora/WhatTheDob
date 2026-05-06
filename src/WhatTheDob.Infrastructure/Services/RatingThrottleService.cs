using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using WhatTheDob.Application.Interfaces.Services;

namespace WhatTheDob.Infrastructure.Services
{
    public class RatingThrottleService : IRatingThrottleService
    {
        private readonly IMemoryCache _cache;
        private readonly int _maxPerMinute;

        public RatingThrottleService(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;

            // Default to 10 if the config is missing or invalid.
            var configuredLimit = configuration.GetValue<int?>("RatingThrottle:MaxPerMinute") ?? 10;
            _maxPerMinute = Math.Max(1, configuredLimit);
        }

        public bool IsAllowedAndRecord(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return false;

            // Use a simple counter stored in memory with a 1 minute sliding expiration
            var cacheKey = $"rating_count:{sessionId}";
            var counter = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(1);
                return 0;
            });

            // increment and check
            var newCount = counter + 1;
            _cache.Set(cacheKey, newCount, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(1)
            });

            return newCount <= _maxPerMinute;
        }
    }
}
