using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NOCAPI.Modules.Zdx.Dto;
using NOCAPI.Modules.Zdx.DTOs;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.NewFiles
{
    /// <summary>
    /// This service runs in the background and periodically fetches ZDX metrics.
    /// Prometheus scrapes ONLY cached metrics, never calling ZDX API directly.
    /// </summary>
    public class ZdxBackgroundService : BackgroundService
    {
        private readonly ILogger<ZdxBackgroundService> _logger;
        private readonly TokenService _tokenService;
        private readonly PocHelper _pocMethods;
        public static string CachedMetrics = "# No data yet";


        // ---- PROMETHEUS GAUGES ----


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

        // ---- CACHED RESULTS ----

        private static readonly object _cacheLock = new();
        private static List<PocDto> _cachedApps = new();
        private static List<AppStatsDto> _cachedStats = new();

        public ZdxBackgroundService(
            ILogger<ZdxBackgroundService> logger,
            TokenService tokenService,
            PocHelper pocMethods)
        {
            _logger = logger;
            _tokenService = tokenService;
            _pocMethods = pocMethods;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ZDX Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshZdxMetricsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running ZDX background refresh loop.");
                }

                // Refresh ZDX every 5 minutes (within ZDX API rate limits)
                await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
            }
        }

        private async Task RefreshZdxMetricsAsync(CancellationToken token)
        {
            _logger.LogInformation("Refreshing ZDX metrics...");

            var accessToken = await _tokenService.GetAccessTokenAsync();

            var apps = await _pocMethods.GetAppPoc(accessToken);
            var stats = await _pocMethods.GetStatsPerApp(accessToken);

            lock (_cacheLock)
            {
                _cachedApps = apps;
                _cachedStats = stats;
            }

            // ---- UPDATE PROMETHEUS METRICS ----

            ZdxAppScore.Unpublish();
            ZdxAppAvgPageFetchTimeSeconds.Unpublish();
            ZdxAppTotalUsers.Unpublish();
            ZdxAppMostImpactedRegion.Unpublish();
            ZdxAppTotalUsersByRegion.Unpublish();
            ZdxIndividualAppStats.Unpublish();


            foreach (var app in apps)
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

            foreach (var s in stats)
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

            // Produce text output for controller
            using var stream = new MemoryStream();
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, CancellationToken.None);
            stream.Position = 0;

            using var reader = new StreamReader(stream);
            CachedMetrics = reader.ReadToEnd();

            _logger.LogInformation("ZDX metrics updated successfully.");


        }
    }
}
