using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NOCAPI.Modules.Zdx.NewFiles;
using Prometheus;
using System.Threading.RateLimiting;

namespace NOCAPI.Modules.Zdx
{


    [ApiController]
    [Route("api/zdx")]
    public class ZdxController : ControllerBase
    {
        private readonly PocHelper _pocMethods;
        private readonly ILogger<ZdxController> _logger;
        private readonly TokenService _tokenService;
        private readonly RateLimiter _rateLimiter;
        private readonly GAHelper _gaHelper;
        private readonly GATokenService _gaTokenService;

        private static readonly object _cacheLock = new();


        private static readonly Gauge ZdxAppScore = Metrics.CreateGauge(
          "zdx_app_score",
          "ZDX application score (0-100)",
          new GaugeConfiguration
          {
              LabelNames = new[] { "app_id", "app_name" }
          });

        private static readonly Gauge ZdxAppAvgPageFetchTimeSeconds = Metrics.CreateGauge(
          "zdx_app_avg_page_fetch_time_seconds",
          "Average page fetch time of the app in miliseconds",
          new GaugeConfiguration
          {
              LabelNames = new[] { "app_id", "app_name" }
          });

        private static readonly Gauge ZdxAppTotalUsers = Metrics.CreateGauge(
            "zdx_app_total_users",
            "Total number of users of the app",
            new GaugeConfiguration
            {
                LabelNames = new[] { "app_id", "app_name" }
            });

        private static readonly Gauge ZdxAppMostImpactedRegion = Metrics.CreateGauge(
            "zdx_app_most_impacted_region",
            "The most impacted region of the app",
        new GaugeConfiguration
        {
            LabelNames = new[] { "app_id", "app_name", "region" }
        });

        private static readonly Gauge ZdxAppTotalUsersByRegion = Metrics.CreateGauge(
        "zdx_app_total_users_by_region",
        "Total users of the app with region label for Grafana stacking",
        new GaugeConfiguration
        {
            LabelNames = new[] { "app_id", "app_name", "region" }
        });

        private static readonly Gauge ZdxIndividualAppStats = Metrics.CreateGauge(
        "zdx_individual_app_stats",
        "ZDX app stats including counts and percentages per app",
        new GaugeConfiguration
        {
            LabelNames = new[] { "app_id", "app_name", "metric_type" }
        });

        private static readonly Gauge ZdxActiveDevicesPerApp = Metrics.CreateGauge(
         "zdx_active_devices",
         "ZDX active devices per app",
         new GaugeConfiguration
         {
             LabelNames = new[] { "app_id", "app_name", "metric_type" }
         });


        public ZdxController(ILogger<ZdxController> logger)
        {
            _logger = logger;

            ZdxServiceInitializer.Initialize();

            _pocMethods = ZdxServiceInitializer.ServiceProvider.GetRequiredService<PocHelper>();
            _rateLimiter = ZdxServiceInitializer.ServiceProvider.GetRequiredService<RateLimiter>();
            _tokenService = ZdxServiceInitializer.ServiceProvider.GetRequiredService<TokenService>();
            _gaHelper = ZdxServiceInitializer.ServiceProvider.GetRequiredService<GAHelper>();
            _gaTokenService = ZdxServiceInitializer.ServiceProvider.GetRequiredService<GATokenService>();
        }

        private static readonly Dictionary<string, double> RegionMapping = new()
            {
                { "Kenya", 1 },
                { "Nigeria", 2 },
                { "South Africa", 3 },
                { "China", 4 },
                { "Hong Kong", 5 },
                { "India", 6 },
                { "Pakistan", 7 },
                { "Singapore", 8 },
                { "Austria", 9 },
                { "Belgium", 10 },
                { "Denmark", 11 },
                { "France", 12 },
                { "Germany", 13 },
                { "Ireland", 14 },
                { "Italy", 15 },
                { "Jersey", 16 },
                { "Netherlands", 17 },
                { "Norway", 18 },
                { "Poland", 19 },
                { "Slovakia", 20 },
                { "Spain", 21 },
                { "Sweden", 22 },
                { "Switzerland", 23 },
                { "United Kingdom", 24 },
                { "Vatican City", 25 },
                { "Canada", 26 },
                { "Dominican Republic", 27 },
                { "Mexico", 28 },
                { "United States", 29 },
                { "Australia", 30 },
                { "New Zealand", 31 },
                { "Brazil", 32 },
                { "Colombia", 33 },
                { "Unknown", 0 }  
            };

        private static readonly HashSet<string> UsStates = new(StringComparer.OrdinalIgnoreCase)
        {
            "Alabama","Arizona","California","Colorado","Connecticut","Delaware",
            "District of Columbia","Florida","Georgia","Illinois","Indiana","Iowa",
            "Kentucky","Maine","Maryland","Massachusetts","Michigan","Minnesota",
            "Mississippi","Missouri","Montana","Nebraska","New Hampshire","New Jersey",
            "New York","North Carolina","North Dakota","Ohio","Oklahoma","Oregon",
            "Pennsylvania","Rhode Island","South Carolina","South Dakota","Tennessee",
            "Texas","Utah","Virginia","Washington","West Virginia","Wisconsin","Wyoming"
        };

        private static readonly Dictionary<string, string> LastRegions = new();


        // Convert region string to numeric
        private static double RegionToNumeric(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return 0;

            if (UsStates.Contains(region))
                region = "United States";

            if (RegionMapping.TryGetValue(region, out var value))
                return value;

            return 0;
        }


        //private async Task RefreshMetricsAsync()
        //{
        //    try
        //    {
        //        var token = await _tokenService.GetAccessTokenAsync();
        //        await _rateLimiter.WaitTurnAsync();
        //        var appsOverview = await _pocMethods.GetAppPoc(token);
        //        var stats = await _pocMethods.GetStatsPerApp(token);

        //        // Update Prometheus gauges
        //        ZdxAppScore.Unpublish();
        //        ZdxAppAvgPageFetchTimeSeconds.Unpublish();
        //        ZdxAppTotalUsers.Unpublish();
        //        ZdxIndividualAppStats.Unpublish();

        //        foreach (var app in appsOverview)
        //        {
        //            ZdxAppScore.WithLabels(app.AppId.ToString(), app.AppName).Set(app.Score);
        //            ZdxAppAvgPageFetchTimeSeconds.WithLabels(app.AppId.ToString(), app.AppName).Set(app.AvgPageFetchTime);
        //            ZdxAppTotalUsers.WithLabels(app.AppId.ToString(), app.AppName).Set(app.TotalUsers);
        //        }

        //        foreach (var stat in stats)
        //        {
        //            ZdxIndividualAppStats.WithLabels(stat.AppId.ToString(), stat.AppName, "num_poor").Set(stat.NumPoor);
        //            ZdxIndividualAppStats.WithLabels(stat.AppId.ToString(), stat.AppName, "num_okay").Set(stat.NumOkay);
        //            ZdxIndividualAppStats.WithLabels(stat.AppId.ToString(), stat.AppName, "num_good").Set(stat.NumGood);
        //        }

        //        // Export metrics to cache
        //        var stream = new MemoryStream();
        //        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        //        stream.Position = 0;
        //        using var reader = new StreamReader(stream);
        //        var metrics = await reader.ReadToEndAsync();

        //        lock (_cacheLock)
        //        {
        //            _cachedMetrics = metrics;
        //            _lastRefresh = DateTime.UtcNow;
        //        }

        //        _logger.LogInformation("Metrics refreshed successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error refreshing metrics.");
        //    }
        //}

        [HttpGet("centralisedData")]

        public async Task<IActionResult> GetAllZdxMetrics()
        {

            try
            {
                _logger.LogInformation("Endpoint is being hit");

                var stream = new MemoryStream();
                await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
                stream.Position = 0;

                using var reader = new StreamReader(stream);
                var metrics = await reader.ReadToEndAsync();

                return Content(metrics, "text/plain; version=0.0.4");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ZDX metrics.");
                return StatusCode(500, "Failed to fetch metrics.");
            }

        }
 
        [HttpGet("testData")]
        public async Task<IActionResult> GetTest()
        {
            //try
            //{
            //    await _rateLimiter.WaitTurnAsync();
            //    _logger.LogInformation("Prometheus scrape hit.");

            //    var metrics = ZdxBackgroundService.CachedMetrics;

            //    if (metrics == "# No data yet")
            //    {
            //        return Content("# No ZDX metrics yet, waiting for background refresh.", "text/plain");
            //    }

            //    return Content(metrics, "text/plain; version=0.0.4");
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error fetching ZDX metrics.");
            //    return StatusCode(500, "Failed to fetch metrics.");
            //}

            return StatusCode(200, "Ok");
        }

        [HttpGet("gatest")]
        public async Task<IActionResult> GATest()
        {
            try
            {
                await _rateLimiter.WaitTurnAsync();
                _logger.LogInformation("Prometheus scrape hit.");

                var metrics = GABackgroundService.CachedMetrics;

                if (metrics == "# No data yet")
                {
                    return Content("# No ZDX metrics yet, waiting for background refresh.", "text/plain");
                }

                return Content(metrics, "text/plain; version=0.0.4");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ZDX metrics.");
                return StatusCode(500, "Failed to fetch metrics.");
            }
        }



        [HttpGet("ga/test-once")]
        public async Task<IActionResult> GaTestOnce([FromServices] GAHelper gaHelper)
        {
            try
            {
                var token = await _gaTokenService.GetAccessTokenAsync();
                _logger.LogInformation("GA token acquired. Len={Len}, Prefix={Pref}",
                    token?.Length ?? 0,
                    token is { Length: > 10 } ? token[..10] : token ?? "<null>");

                var json = await gaHelper.GetIssuerOnlineMetricsAsync(token, GAHelper.Region.NA, 10);
                _logger.LogInformation("GA test-once JSON (first 500): {Json}",
                    json is null ? "<null>" : json.Substring(0, Math.Min(500, json.Length)));

                return Content(json ?? "null", "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GA test-once failed.");
                // TEMP: bubble error text to the client so curl shows the reason
                return StatusCode(500, $"GA test-once failed: {ex.GetType().Name}: {ex.Message}");
            }
        }


    }
}