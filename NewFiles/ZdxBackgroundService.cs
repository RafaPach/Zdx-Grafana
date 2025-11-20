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

        // ---- PROMETHEUS GAUGES ----

        private static readonly Gauge ZdxAppScore = Metrics.CreateGauge(
            "zdx_app_score",
            "ZDX application score (0-100)",
            new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name" } });

        private static readonly Gauge ZdxAppAvgPageFetchTime = Metrics.CreateGauge(
            "zdx_app_avg_page_fetch_time_seconds",
            "Average page fetch time of the app in milliseconds",
            new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name" } });

        private static readonly Gauge ZdxAppTotalUsers = Metrics.CreateGauge(
            "zdx_app_total_users",
            "Total number of users of the app",
            new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name" } });

        private static readonly Gauge ZdxIndividualAppStats = Metrics.CreateGauge(
            "zdx_individual_app_stats",
            "ZDX app stats including counts and percentages",
            new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name", "metric_type" } });

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
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
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
            ZdxAppAvgPageFetchTime.Unpublish();
            ZdxAppTotalUsers.Unpublish();
            ZdxIndividualAppStats.Unpublish();

            foreach (var app in apps)
            {
                ZdxAppScore.WithLabels(app.AppId.ToString(), app.AppName).Set(app.Score);
                ZdxAppAvgPageFetchTime.WithLabels(app.AppId.ToString(), app.AppName).Set(app.AvgPageFetchTime);
                ZdxAppTotalUsers.WithLabels(app.AppId.ToString(), app.AppName).Set(app.TotalUsers);
            }

            foreach (var s in stats)
            {
                ZdxIndividualAppStats.WithLabels(s.AppId.ToString(), s.AppName, "num_poor").Set(s.NumPoor);
                ZdxIndividualAppStats.WithLabels(s.AppId.ToString(), s.AppName, "num_okay").Set(s.NumOkay);
                ZdxIndividualAppStats.WithLabels(s.AppId.ToString(), s.AppName, "num_good").Set(s.NumGood);
            }

            _logger.LogInformation(
                $"ZDX metrics updated: {apps.Count} apps, {stats.Count} stats.");
        }
    }
}
