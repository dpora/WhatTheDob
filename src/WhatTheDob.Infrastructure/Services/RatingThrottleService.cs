using System;
using Microsoft.Extensions.Caching.Memory;
using WhatTheDob.Application.Interfaces.Services;

namespace WhatTheDob.Infrastructure.Services
{
    public class RatingThrottleService : IRatingThrottleService
    {
        private readonly IMemoryCache _cache;
        private readonly int _maxPerMinute = 5;

        public RatingThrottleService(IMemoryCache cache)
        {
            _cache = cache;
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
