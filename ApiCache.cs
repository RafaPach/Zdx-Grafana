using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx
{
    public  class ApiCache
    {
        private static readonly ConcurrentDictionary<string, (object Value, DateTime Timestamp)> _cache = new();
        private static readonly TimeSpan _defaultCacheDuration = TimeSpan.FromSeconds(15);
        private readonly RateLimiter _rateLimiter;
        
        public  ApiCache(RateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        public async Task<T> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan? duration = null)
        {
            var cacheDuration = duration ?? _defaultCacheDuration;

            if (_cache.TryGetValue(key, out var cached))
            {
                if (DateTime.UtcNow - cached.Timestamp < cacheDuration)
                    return (T)cached.Value;
            }

            await _rateLimiter.WaitTurnAsync();

            var value = await fetchFunc();
            _cache[key] = (value!, DateTime.UtcNow);
            return value;
        }
    }
    }
