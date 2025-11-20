using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx
{
    //public class RateLimiter
    //{
    //    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    //    private static DateTime _lastCallTime = DateTime.MinValue;
    //    private static readonly TimeSpan _delayBetweenCalls = TimeSpan.FromSeconds(6);

    //    public static async Task WaitTurnAsync()
    //    {
    //        await _semaphore.WaitAsync();
    //        try
    //        {
    //            var elapsed = DateTime.UtcNow - _lastCallTime;
    //            if (elapsed < _delayBetweenCalls)
    //            {
    //                var wait = _delayBetweenCalls - elapsed;
    //                await Task.Delay(wait);
    //            }

    //            _lastCallTime = DateTime.UtcNow;
    //        }
    //        finally
    //        {
    //            _semaphore.Release();
    //        }
    //    }

    //public class RateLimiter
    //{
    //    private static readonly object _lock = new();
    //    private static Queue<DateTime> _timestamps = new();
    //    private const int MaxPerSecond = 5;
    //    private const int MaxPerMinute = 30;

    //    public static async Task WaitTurnAsync()
    //    {
    //        while (true)
    //        {
    //            lock (_lock)
    //            {
    //                var now = DateTime.UtcNow;
    //                _timestamps = new Queue<DateTime>(_timestamps.Where(t => (now - t).TotalMinutes < 1));

    //                var callsLastSecond = _timestamps.Count(t => (now - t).TotalSeconds < 1);
    //                var callsLastMinute = _timestamps.Count;

    //                if (callsLastSecond < MaxPerSecond && callsLastMinute < MaxPerMinute)
    //                {
    //                    _timestamps.Enqueue(now);
    //                    return;
    //                }
    //            }
    //            await Task.Delay(200); // Wait and retry
    //        }
    //    }
    //}

    //public class RateLimiter
    //{

    //    private readonly SemaphoreSlim _secondLimiter = new SemaphoreSlim(5, 5);
    //    private readonly SemaphoreSlim _minuteLimiter = new SemaphoreSlim(30, 30);

    //    public RateLimiter()
    //    {
    //        // Refill every second
    //        var secondTimer = new Timer(_ =>
    //        {
    //            var toRelease = 5 - _secondLimiter.CurrentCount;
    //            if (toRelease > 0) _secondLimiter.Release(toRelease);
    //        }, null, 1000, 1000);

    //        // Refill every minute
    //        var minuteTimer = new Timer(_ =>
    //        {
    //            var toRelease = 30 - _minuteLimiter.CurrentCount;
    //            if (toRelease > 0) _minuteLimiter.Release(toRelease);
    //        }, null, 60000, 60000);
    //    }

    //    public async Task WaitTurnAsync()
    //    {
    //        await _secondLimiter.WaitAsync();
    //        await _minuteLimiter.WaitAsync();
    //    }

    //public class RateLimiter
    //{
    //    private readonly SemaphoreSlim _secondLimiter = new SemaphoreSlim(5, 5);
    //    private readonly SemaphoreSlim _minuteLimiter = new SemaphoreSlim(30, 30);

    //    public RateLimiter()
    //    {
    //        // Refill every second
    //        var secondTimer = new Timer(_ =>
    //        {
    //            var toRelease = 5 - _secondLimiter.CurrentCount;
    //            if (toRelease > 0)
    //            {
    //                _secondLimiter.Release(toRelease);
    //                Console.WriteLine($"[RateLimiter] Refilled {toRelease} tokens for second bucket. Current: {_secondLimiter.CurrentCount}");
    //            }
    //        }, null, 1000, 1000);

    //        // Refill every minute
    //        var minuteTimer = new Timer(_ =>
    //        {
    //            var toRelease = 30 - _minuteLimiter.CurrentCount;
    //            if (toRelease > 0)
    //            {
    //                _minuteLimiter.Release(toRelease);
    //                Console.WriteLine($"[RateLimiter] Refilled {toRelease} tokens for minute bucket. Current: {_minuteLimiter.CurrentCount}");
    //            }
    //        }, null, 60000, 60000);
    //    }

    //    public async Task WaitTurnAsync()
    //    {
    //        Console.WriteLine($"[RateLimiter] Waiting for token... Second: {_secondLimiter.CurrentCount}, Minute: {_minuteLimiter.CurrentCount}");
    //        await _secondLimiter.WaitAsync();
    //        await _minuteLimiter.WaitAsync();
    //        Console.WriteLine($"[RateLimiter] Token acquired. Remaining -> Second: {_secondLimiter.CurrentCount}, Minute: {_minuteLimiter.CurrentCount}");
    //    }
    //}

    public class RateLimiter : IDisposable
    {
        private readonly SemaphoreSlim _secondLimiter = new SemaphoreSlim(5, 5);
        private readonly SemaphoreSlim _minuteLimiter = new SemaphoreSlim(30, 30);

        private readonly Timer _secondTimer;
        private readonly Timer _minuteTimer;
        private readonly ILogger<RateLimiter> _logger;

        public RateLimiter(ILogger<RateLimiter> logger)
        {
            _logger = logger;

            // Refill every second
            _secondTimer = new Timer(_ =>
            {
                var toRelease = 5 - _secondLimiter.CurrentCount;
                if (toRelease > 0)
                {
                    _secondLimiter.Release(toRelease);
                }
            }, null, 1000, 1000);

            // Refill every minute
            _minuteTimer = new Timer(_ =>
            {
                var toRelease = 30 - _minuteLimiter.CurrentCount;
                if (toRelease > 0)
                {
                    _minuteLimiter.Release(toRelease);
                }
            }, null, 60000, 60000);
        }

        public async Task WaitTurnAsync()
        {
            await _secondLimiter.WaitAsync();
            await _minuteLimiter.WaitAsync();
        }

        public void Dispose()
        {
            _secondTimer?.Dispose();
            _minuteTimer?.Dispose();
        }
    }

}

