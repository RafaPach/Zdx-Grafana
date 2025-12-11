using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NOCAPI.Modules.Zdx.DTOs;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.NewFiles
{
    public class GABackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly GAHelper _gaHelper;
        private readonly TokenService _tokenService;
        private readonly GATokenService _gaTokenService;

        public static string CachedMetrics = "# No GA data yet";

        public static string CachedGaJson = null; // ADD: raw JSON for debugging


        private static readonly object _cacheLock = new();
        private static GADto _cachedGaEmea;

        // ---- PROMETHEUS METRICS ----
        private static readonly Gauge GaInvestorCentreActiveUsers = Metrics.CreateGauge(
            "ga_investorcentre_active_users",
            "Google Analytics realtime active users",
            new GaugeConfiguration
            {
                LabelNames = new[] { "region", "screen" }
            });

        private static readonly Gauge GaInvestorCentrePageViews = Metrics.CreateGauge(
            "ga_investorcentre_pageviews",
            "Google Analytics realtime screen pageviews",
            new GaugeConfiguration
            {
                LabelNames = new[] { "region", "screen" }
            });


        private static readonly Gauge GaIssuerOnlineActiveUsers = Metrics.CreateGauge(
                    "ga_issueronline_active_users",
                    "Google Analytics realtime active users (IssuerOnline)",
                    new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaIssuerOnlinePageViews = Metrics.CreateGauge(
            "ga_issueronline_pageviews",
            "Google Analytics realtime screen pageviews (IssuerOnline)",
            new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });


        private static readonly Gauge GaSphereActiveUsers = Metrics.CreateGauge(
                    "ga_sphere_active_users",
                    "Google Analytics realtime active users (Sphere)",
                    new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaSpherePageViews = Metrics.CreateGauge(
            "ga_sphere_pageviews",
            "Google Analytics realtime screen pageviews (Sphere)",
            new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });



        public GABackgroundService(
            ILogger<GABackgroundService> logger,
            GAHelper gaHelper,
            TokenService tokenService,
            GATokenService gATokenService)
        {
            _logger = logger;
            _gaHelper = gaHelper;
            _tokenService = tokenService;
            _gaTokenService = gATokenService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GA Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshGaMetricsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing GA metrics.");
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        //private async Task RefreshGaMetricsAsync(CancellationToken token)
        //{
        //    _logger.LogInformation("Refreshing GA metrics...");

        //    //var accessToken = await _tokenService.GetAccessTokenAsync();

        //    var accessToken= await _gaTokenService.GetAccessTokenAsync();

        //    Console.WriteLine($"AccessToken{accessToken}");


        //    var regionCache = new Dictionary<GAHelper.Region, GADto>();

        //    GaInvestorCentreActiveUsers.Unpublish();
        //    GaInvestorCentrePageViews.Unpublish();
        //    GaIssuerOnlineActiveUsers.Unpublish();
        //    GaIssuerOnlinePageViews.Unpublish();

        //    foreach (GAHelper.Region region in Enum.GetValues(typeof(GAHelper.Region)))
        //    {
        //        var json = await _gaHelper.GetInvestorCentreMetricsAsync(
        //        accessToken,
        //        region,
        //        15);

        //        var model = JsonSerializer.Deserialize<GADto>(json);

        //        lock (_cacheLock)
        //        {
        //            regionCache[region] = model;
        //            CachedGaJson = json;
        //        }


        //        var rows = model.Rows;
        //        if (rows == null || rows.Count == 0)
        //        {

        //            _logger.LogInformation("GA realtime returned no rows for {Region}.", region);
        //            continue;

        //        }

        //        var regionLabel = region.ToString().ToLowerInvariant();


        //            foreach (var row in rows)
        //            {
        //                var screen = row.DimensionValues[0].Value;

        //                var activeUsers = int.Parse(row.MetricValues[0].Value);

        //                var pageViews = int.Parse(row.MetricValues[1].Value);

        //                GaInvestorCentreActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
        //                GaInvestorCentrePageViews.WithLabels(regionLabel, screen).Set(pageViews);

        //                _logger.LogInformation("GA row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
        //                    screen, activeUsers, pageViews);
        //            }
        //        }


        //    // Export metrics to controller
        //    using var stream = new MemoryStream();
        //    await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        //    stream.Position = 0;

        //    using var reader = new StreamReader(stream);
        //    CachedMetrics = reader.ReadToEnd();

        //    _logger.LogInformation("GA metrics updated.");
        //}


        private async Task RefreshGaMetricsAsync(CancellationToken token)
        {
            _logger.LogInformation("Refreshing GA metrics...");
            var accessToken = await _gaTokenService.GetAccessTokenAsync();
            Console.WriteLine($"AccessToken{accessToken}");

            var regionCache = new Dictionary<GAHelper.Region, GADto>();

            // Reset all gauges before repopulating
            GaInvestorCentreActiveUsers.Unpublish();
            GaInvestorCentrePageViews.Unpublish();
            GaIssuerOnlineActiveUsers.Unpublish();
            GaIssuerOnlinePageViews.Unpublish();

            foreach (GAHelper.Region region in Enum.GetValues(typeof(GAHelper.Region)))
            {
                // --- InvestorCentre ---
                try
                {
                    var jsonIc = await _gaHelper.GetInvestorCentreMetricsAsync(accessToken, region, 15);
                    var modelIc = JsonSerializer.Deserialize<GADto>(jsonIc);

                    lock (_cacheLock)
                    {
                        regionCache[region] = modelIc;
                        CachedGaJson = jsonIc;
                    }

                    var rowsIc = modelIc.Rows;
                    if (rowsIc == null || rowsIc.Count == 0)
                    {
                        _logger.LogInformation("GA realtime (InvestorCentre) returned no rows for {Region}.", region);
                    }
                    else
                    {
                        var regionLabel = region.ToString().ToUpperInvariant();
                        foreach (var row in rowsIc)
                        {
                            var screen = row.DimensionValues[0].Value;
                            var activeUsers = int.Parse(row.MetricValues[0].Value);
                            var pageViews = int.Parse(row.MetricValues[1].Value);

                            GaInvestorCentreActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
                            GaInvestorCentrePageViews.WithLabels(regionLabel, screen).Set(pageViews);

                            _logger.LogInformation(
                                "InvestorCentre row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
                                screen, activeUsers, pageViews);
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    // Unknown region for InvestorCentre mapping; skip
                    _logger.LogDebug(ex, "InvestorCentre not configured for region {Region}", region);
                }

                // --- IssuerOnline ---
                try
                {
                    // Helper throws if region isn't configured in PropertyIds_IssuerOnline; we catch & skip.
                    var jsonIo = await _gaHelper.GetIssuerOnlineMetricsAsync(accessToken, region, 15);
                    var modelIo = JsonSerializer.Deserialize<GADto>(jsonIo);

                    var rowsIo = modelIo.Rows;
                    if (rowsIo == null || rowsIo.Count == 0)
                    {
                        _logger.LogInformation("GA realtime (IssuerOnline) returned no rows for {Region}.", region);
                    }
                    else
                    {
                        var regionLabel = region.ToString().ToUpperInvariant();
                        foreach (var row in rowsIo)
                        {
                            var screen = row.DimensionValues[0].Value;
                            var activeUsers = int.Parse(row.MetricValues[0].Value);
                            var pageViews = int.Parse(row.MetricValues[1].Value);

                            GaIssuerOnlineActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
                            GaIssuerOnlinePageViews.WithLabels(regionLabel, screen).Set(pageViews);

                            _logger.LogInformation(
                                "IssuerOnline row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
                                screen, activeUsers, pageViews);
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogDebug(ex, "IssuerOnline not configured for region {Region}", region);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "IssuerOnline GA request failed for region {Region}", region);
                }
            }

            var sphereRegions = new[] { GAHelper.Region.Global };
            foreach (var region in sphereRegions)
            {
                try
                {
                    var jsonSphere = await _gaHelper.GetSphereMetricsAsync(accessToken, region, 15);
                    var modelSphere = JsonSerializer.Deserialize<GADto>(jsonSphere);

                    var rowsSp = modelSphere.Rows;
                    if (rowsSp == null || rowsSp.Count == 0)
                    {
                        _logger.LogInformation("GA realtime (Sphere) returned no rows for {Region}.", region);
                    }
                    else
                    {
                        var regionLabel = region.ToString().ToUpperInvariant();
                        foreach (var row in rowsSp)
                        {
                            var screen = row.DimensionValues[0].Value;
                            var activeUsers = int.Parse(row.MetricValues[0].Value);
                            var pageViews = int.Parse(row.MetricValues[1].Value);

                            GaSphereActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
                            GaSpherePageViews.WithLabels(regionLabel, screen).Set(pageViews);

                            _logger.LogInformation(
                                "Sphere row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
                                screen, activeUsers, pageViews);
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogDebug(ex, "Sphere not configured for region {Region}", region);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Sphere GA request failed for region {Region}", region);
                }
            }


            // Export metrics to controller
            using var stream = new MemoryStream();
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            CachedMetrics = reader.ReadToEnd();

            _logger.LogInformation("GA metrics updated.");
        }
    }
}
