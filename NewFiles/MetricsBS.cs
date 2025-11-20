using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NOCAPI.Modules.Zdx.Dto;
using NOCAPI.Modules.Zdx.DTOs;
using Prometheus;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.NewFiles
{
        public class MetricsBS : BackgroundService
        {
            private readonly ILogger<MetricsBS> _logger;
            private readonly TokenService _tokenService;
            private readonly PocHelper _pocMethods;

            private static readonly Gauge ZdxAppScore = Metrics.CreateGauge(
                "zdx_app_score", "ZDX application score (0-100)",
                new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name" } });

            private static readonly Gauge ZdxAppAvgPageFetchTimeSeconds = Metrics.CreateGauge(
                "zdx_app_avg_page_fetch_time_seconds", "Average page fetch time of the app in milliseconds",
                new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name" } });

            private static readonly Gauge ZdxAppTotalUsers = Metrics.CreateGauge(
                "zdx_app_total_users", "Total number of users of the app",
                new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name" } });

            private static readonly Gauge ZdxIndividualAppStats = Metrics.CreateGauge(
                "zdx_individual_app_stats", "ZDX app stats including counts and percentages per app",
                new GaugeConfiguration { LabelNames = new[] { "app_id", "app_name", "metric_type" } });

            private static readonly object _cacheLock = new();
            private static List<PocDto> _cachedApps = new();
            private static List<AppStatsDto> _cachedStats = new();

            public MetricsBS(ILogger<MetricsBS> logger,
                                            TokenService tokenService,
                                            PocHelper pocMethods)
            {
                _logger = logger;
                _tokenService = tokenService;
                _pocMethods = pocMethods;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                _logger.LogInformation("Metrics background service started.");

                // Start two tasks: fast refresh and slow refresh
                var fastTask = Task.Run(() => FastRefreshLoop(stoppingToken));
                var slowTask = Task.Run(() => SlowRefreshLoop(stoppingToken));

                await Task.WhenAll(fastTask, slowTask);
            }

            private async Task FastRefreshLoop(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        lock (_cacheLock)
                        {
                            // Update Prometheus metrics from cache
                            ZdxAppScore.Unpublish();
                            ZdxAppAvgPageFetchTimeSeconds.Unpublish();
                            ZdxAppTotalUsers.Unpublish();
                            ZdxIndividualAppStats.Unpublish();

                            foreach (var app in _cachedApps)
                            {
                                ZdxAppScore.WithLabels(app.AppId.ToString(), app.AppName).Set(app.Score);
                                ZdxAppAvgPageFetchTimeSeconds.WithLabels(app.AppId.ToString(), app.AppName).Set(app.AvgPageFetchTime);
                                ZdxAppTotalUsers.WithLabels(app.AppId.ToString(), app.AppName).Set(app.TotalUsers);
                            }

                            foreach (var stat in _cachedStats)
                            {
                                ZdxIndividualAppStats.WithLabels(stat.AppId.ToString(), stat.AppName, "num_poor").Set(stat.NumPoor);
                                ZdxIndividualAppStats.WithLabels(stat.AppId.ToString(), stat.AppName, "num_okay").Set(stat.NumOkay);
                                ZdxIndividualAppStats.WithLabels(stat.AppId.ToString(), stat.AppName, "num_good").Set(stat.NumGood);
                            }
                        }

                        _logger.LogInformation("Prometheus metrics updated from cache.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating Prometheus metrics from cache.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            private async Task SlowRefreshLoop(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Refreshing cache from ZDX API...");
                        var token = await _tokenService.GetAccessTokenAsync();

                        var apps = await _pocMethods.GetAppPoc(token);
                        var stats = await _pocMethods.GetStatsPerApp(token);

                        lock (_cacheLock)
                        {
                            _cachedApps = apps;
                            _cachedStats = stats;
                        }

                        _logger.LogInformation($"Cache refreshed: {apps.Count} apps, {stats.Count} stats.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing cache from ZDX API.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
