using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.Service
{
    public class MetricsBackgroundService : BackgroundService
    {

        private readonly ILogger<MetricsBackgroundService> _logger;
        private readonly TokenService _tokenService;
        private readonly PocHelper _pocMethods;
        private readonly RateLimiter _rateLimiter;
        private static readonly Random _random = new();


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

        private static readonly Gauge ZdxMockRegionalImpact = Metrics.CreateGauge(
        "zdx_mock_regional_impact",
        "Mock regional impact score for testing",
        new GaugeConfiguration
        {
            LabelNames = new[] { "region" }
        });

        public MetricsBackgroundService(ILogger<MetricsBackgroundService> logger,
                                           TokenService tokenService,
                                           PocHelper pocMethods, RateLimiter rateLimiter)
        {
            _logger = logger;
            _tokenService = tokenService;
            _pocMethods = pocMethods;
            _rateLimiter = rateLimiter;
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


        private async Task AddJitterAsync()
        {
            await Task.Delay(_random.Next(300, 600)); // 300–600 ms
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    _logger.LogInformation("Background service started");

                    await _rateLimiter.WaitTurnAsync();

                    var token = await _tokenService.GetAccessTokenAsync();

                    var appsOverview = await _pocMethods.GetAppPoc(token);

                    await AddJitterAsync();

                    //var stats = await _pocMethods.GetStatsPerApp(token);

                    // Build Prometheus metrics

                    ZdxAppScore.Unpublish();
                    ZdxAppAvgPageFetchTimeSeconds.Unpublish();
                    ZdxAppTotalUsers.Unpublish();
                    ZdxAppMostImpactedRegion.Unpublish();
                    ZdxAppTotalUsersByRegion.Unpublish();
                    ZdxIndividualAppStats.Unpublish();

                    foreach (var app in appsOverview)
                    {
                        var appIdStr = app.AppId.ToString();
                        var appName = app.AppName;
                        var region = app.MostImpactedRegion;
                        var numericValue = RegionToNumeric(region);

                        ZdxAppScore.WithLabels(appIdStr, appName).Set(app.Score);
                        ZdxAppAvgPageFetchTimeSeconds.WithLabels(appIdStr, appName).Set(app.AvgPageFetchTime);
                        ZdxAppTotalUsers.WithLabels(appIdStr, appName).Set(app.TotalUsers);
                        ZdxAppMostImpactedRegion.WithLabels(appIdStr, appName, region).Set(numericValue);
                        ZdxAppTotalUsersByRegion.WithLabels(appIdStr, appName, region).Set(app.TotalUsers);
                    }


                    await AddJitterAsync();

                    var stats = await _pocMethods.GetStatsPerApp(token);

                    foreach (var stat in stats)
                    {
                        var appIdStr = stat.AppId.ToString();
                        var appName = stat.AppName ?? "Unknown";

                        ZdxIndividualAppStats.WithLabels(appIdStr, appName, "num_poor").Set(stat.NumPoor);
                        ZdxIndividualAppStats.WithLabels(appIdStr, appName, "num_okay").Set(stat.NumOkay);
                        ZdxIndividualAppStats.WithLabels(appIdStr, appName, "num_good").Set(stat.NumGood);
                        ZdxActiveDevicesPerApp.WithLabels(appIdStr, appName, "active_devices").Set(stat.ActiveDevices);
                    }

                    var mockRegions = new Dictionary<string, double>
                    {
                        { "North America", 150 },
                        { "Europe", 100 },
                        { "Asia", 80 },
                        { "South America", 40 }
                    };

                    foreach (var region in mockRegions)
                    {
                        ZdxMockRegionalImpact.WithLabels(region.Key).Set(region.Value);
                    }

                    var stream = new MemoryStream();
                    await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
                    stream.Position = 0;
                    using var reader = new StreamReader(stream);
                    var metrics = await reader.ReadToEndAsync();

                    MetricsCache.Update(metrics);
                    _logger.LogInformation("ZDX metrics updated successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating ZDX metrics.");
                }

                await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken); 
            }

        }
    }
}
