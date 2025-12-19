//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using NOCAPI.Modules.Zdx.DTOs;
//using Prometheus;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace NOCAPI.Modules.Zdx.NewFiles
//{
//    public class GABackgroundService : BackgroundService
//    {
//        private readonly ILogger _logger;
//        private readonly GAHelper _gaHelper;
//        private readonly TokenService _tokenService;
//        private readonly GATokenService _gaTokenService;
//        private readonly GASnapshots _gaSnapshots;

//        public static string CachedMetrics = "# No GA data yet";

//        public static string CachedGaJson = null; // ADD: raw JSON for debugging


//        private static readonly object _cacheLock = new();
//        private static GADto _cachedGaEmea;

//        // ---- PROMETHEUS METRICS ----
//        private static readonly Gauge GaInvestorCentreActiveUsers = Metrics.CreateGauge(
//            "ga_investorcentre_active_users",
//            "Google Analytics realtime active users",
//            new GaugeConfiguration
//            {
//                LabelNames = new[] { "region", "screen" }
//            });

//        private static readonly Gauge GaInvestorCentrePageViews = Metrics.CreateGauge(
//            "ga_investorcentre_pageviews",
//            "Google Analytics realtime screen pageviews",
//            new GaugeConfiguration
//            {
//                LabelNames = new[] { "region", "screen" }
//            });


//        private static readonly Gauge GaIssuerOnlineActiveUsers = Metrics.CreateGauge(
//                    "ga_issueronline_active_users",
//                    "Google Analytics realtime active users (IssuerOnline)",
//                    new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

//        private static readonly Gauge GaIssuerOnlinePageViews = Metrics.CreateGauge(
//            "ga_issueronline_pageviews",
//            "Google Analytics realtime screen pageviews (IssuerOnline)",
//            new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });


//        private static readonly Gauge GaSphereActiveUsers = Metrics.CreateGauge(
//                    "ga_sphere_active_users",
//                    "Google Analytics realtime active users (Sphere)",
//                    new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

//        private static readonly Gauge GaSpherePageViews = Metrics.CreateGauge(
//            "ga_sphere_pageviews",
//            "Google Analytics realtime screen pageviews (Sphere)",
//            new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });


//        public static readonly Gauge GaInvestorCentreDailyActiveUsers =
//    Metrics.CreateGauge("ga_investorcentre_daily_active_users", "Daily snapshot active users",
//    new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

//        public static readonly Gauge GaInvestorCentreDailyPageViews =
//            Metrics.CreateGauge("ga_investorcentre_daily_pageviews", "Daily snapshot pageviews",
//            new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

//        public static readonly Gauge GaIssuerOnlineDailyActiveUsers =
//            Metrics.CreateGauge("ga_issueronline_daily_active_users", "Daily snapshot active users",
//            new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

//        public static readonly Gauge GaIssuerOnlineDailyPageViews =
//            Metrics.CreateGauge("ga_issueronline_daily_pageviews", "Daily snapshot pageviews",
//            new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });



//        public GABackgroundService(
//            ILogger<GABackgroundService> logger,
//            GAHelper gaHelper,
//            TokenService tokenService,
//            GATokenService gATokenService,
//            GASnapshots gaSnapshots)
//        {
//            _logger = logger;
//            _gaHelper = gaHelper;
//            _tokenService = tokenService;
//            _gaTokenService = gATokenService;
//            _gaSnapshots = gaSnapshots;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("GA Background Service started.");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await RefreshGaMetricsAsync(stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error refreshing GA metrics.");
//                }

//                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
//            }
//        }

//        private async Task RefreshGaMetricsAsync(CancellationToken token)
//        {
//            _logger.LogInformation("Refreshing GA metrics...");
//            var accessToken = await _gaTokenService.GetAccessTokenAsync();
//            Console.WriteLine($"AccessToken{accessToken}");

//            var regionCache = new Dictionary<GAHelper.Region, GADto>();

//            // Reset all gauges before repopulating
//            GaInvestorCentreActiveUsers.Unpublish();
//            GaInvestorCentrePageViews.Unpublish();
//            GaIssuerOnlineActiveUsers.Unpublish();
//            GaIssuerOnlinePageViews.Unpublish();

//            foreach (GAHelper.Region region in Enum.GetValues(typeof(GAHelper.Region)))
//            {
//                // --- InvestorCentre ---
//                try
//                {
//                    var jsonIc = await _gaHelper.GetInvestorCentreMetricsAsync(accessToken, region, 15);
//                    var modelIc = JsonSerializer.Deserialize<GADto>(jsonIc);

//                    lock (_cacheLock)
//                    {
//                        regionCache[region] = modelIc;
//                        CachedGaJson = jsonIc;
//                    }

//                    var rowsIc = modelIc.Rows;
//                    if (rowsIc == null || rowsIc.Count == 0)
//                    {
//                        _logger.LogInformation("GA realtime (InvestorCentre) returned no rows for {Region}.", region);
//                    }
//                    else
//                    {
//                        var regionLabel = region.ToString().ToUpperInvariant();
//                        foreach (var row in rowsIc)
//                        {
//                            var screen = row.DimensionValues[0].Value;
//                            var activeUsers = int.Parse(row.MetricValues[0].Value);
//                            var pageViews = int.Parse(row.MetricValues[1].Value);

//                            GaInvestorCentreActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
//                            GaInvestorCentrePageViews.WithLabels(regionLabel, screen).Set(pageViews);

//                            _logger.LogInformation(
//                                "InvestorCentre row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
//                                screen, activeUsers, pageViews);
//                        }
//                    }
//                }
//                catch (ArgumentException ex)
//                {
//                    // Unknown region for InvestorCentre mapping; skip
//                    _logger.LogDebug(ex, "InvestorCentre not configured for region {Region}", region);
//                }

//                // --- IssuerOnline ---
//                try
//                {
//                    // Helper throws if region isn't configured in PropertyIds_IssuerOnline; we catch & skip.
//                    var jsonIo = await _gaHelper.GetIssuerOnlineMetricsAsync(accessToken, region, 15);
//                    var modelIo = JsonSerializer.Deserialize<GADto>(jsonIo);

//                    var rowsIo = modelIo.Rows;
//                    if (rowsIo == null || rowsIo.Count == 0)
//                    {
//                        _logger.LogInformation("GA realtime (IssuerOnline) returned no rows for {Region}.", region);
//                    }
//                    else
//                    {
//                        var regionLabel = region.ToString().ToUpperInvariant();
//                        foreach (var row in rowsIo)
//                        {
//                            var screen = row.DimensionValues[0].Value;
//                            var activeUsers = int.Parse(row.MetricValues[0].Value);
//                            var pageViews = int.Parse(row.MetricValues[1].Value);

//                            GaIssuerOnlineActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
//                            GaIssuerOnlinePageViews.WithLabels(regionLabel, screen).Set(pageViews);

//                            _logger.LogInformation(
//                                "IssuerOnline row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
//                                screen, activeUsers, pageViews);
//                        }
//                    }
//                }
//                catch (ArgumentException ex)
//                {
//                    _logger.LogDebug(ex, "IssuerOnline not configured for region {Region}", region);
//                }

//                catch (HttpRequestException ex)
//                {
//                    _logger.LogWarning(ex, "IssuerOnline GA request failed for region {Region}", region);
//                }
//            }

//            var sphereRegions = new[] { GAHelper.Region.Global };
//            foreach (var region in sphereRegions)
//            {
//                try
//                {
//                    var jsonSphere = await _gaHelper.GetSphereMetricsAsync(accessToken, region, 15);
//                    var modelSphere = JsonSerializer.Deserialize<GADto>(jsonSphere);

//                    var rowsSp = modelSphere.Rows;
//                    if (rowsSp == null || rowsSp.Count == 0)
//                    {
//                        _logger.LogInformation("GA realtime (Sphere) returned no rows for {Region}.", region);
//                    }
//                    else
//                    {
//                        var regionLabel = region.ToString().ToUpperInvariant();
//                        foreach (var row in rowsSp)
//                        {
//                            var screen = row.DimensionValues[0].Value;
//                            var activeUsers = int.Parse(row.MetricValues[0].Value);
//                            var pageViews = int.Parse(row.MetricValues[1].Value);

//                            GaSphereActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
//                            GaSpherePageViews.WithLabels(regionLabel, screen).Set(pageViews);

//                            _logger.LogInformation(
//                                "Sphere row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
//                                screen, activeUsers, pageViews);
//                        }
//                    }
//                }
//                catch (ArgumentException ex)
//                {
//                    _logger.LogDebug(ex, "Sphere not configured for region {Region}", region);
//                }

//                catch (HttpRequestException ex)
//                {
//                    _logger.LogWarning(ex, "Sphere GA request failed for region {Region}", region);
//                }

//                var snapshotRegions = new[] { GAHelper.Region.EMEA, GAHelper.Region.NA, GAHelper.Region.OCEANIA };

//                foreach(var regionsnap in snapshotRegions)
//                {
//                    try
//                    {
//                        var jsonSnapshotIc = await _gaSnapshots.GetInvestorCentreSnapshotMetricsAsync(accessToken, regionsnap, 15);
//                        var modelSnapshotIc = JsonSerializer.Deserialize<GADto>(jsonSnapshotIc);

//                        var rowsSnapIc = modelSnapshotIc?.Rows;
//                        if (rowsSnapIc == null || rowsSnapIc.Count == 0)
//                        {
//                            _logger.LogInformation("GA snapshot (InvestorCentre) returned no rows for {Region}.", region);
//                        }
//                        else
//                        {
//                            var regionLabel = region.ToString().ToUpperInvariant();

//                            foreach (var row in rowsSnapIc)
//                            {
//                                var screen = row.DimensionValues[0].Value;
//                                var activeUsers = int.Parse(row.MetricValues[0].Value);
//                                var pageViews = int.Parse(row.MetricValues[1].Value);

//                                // Reuse your existing gauges
//                                GaInvestorCentreDailyActiveUsers.WithLabels(regionLabel, screen).Set(activeUsers);
//                                GaInvestorCentreDailyPageViews.WithLabels(regionLabel, screen).Set(pageViews);

//                                _logger.LogInformation(
//                                    "IC Snapshot row: screen={Screen}, activeUsers={ActiveUsers}, pageViews={PageViews}",
//                                    screen, activeUsers, pageViews);
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogWarning(ex, "Snapshot IC GA request failed for region {Region}", region);
//                    }
//                }


//            }

//            // Export metrics to controller
//            using var stream = new MemoryStream();
//            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
//            stream.Position = 0;
//            using var reader = new StreamReader(stream);
//            CachedMetrics = reader.ReadToEnd();

//            _logger.LogInformation("GA metrics updated.");
//        }
//    }
//}


using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NOCAPI.Modules.Zdx.DTOs;
using Prometheus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NOCAPI.Modules.Zdx.NewFiles
{
    public class GABackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly GAHelper _gaHelper;
        //private readonly TokenService _tokenService;
        private readonly GATokenService _gaTokenService;
        private readonly GASnapshots _gaSnapshots;

        public static string CachedMetrics = "# No GA data yet";
        public static string CachedGaJson = null;

        private static readonly object _cacheLock = new();

        private static readonly Dictionary<string, int> _previousTotals = new();


        // -------------------- PROMETHEUS METRICS --------------------

        private static readonly Gauge GaInvestorCentreActiveUsers =
            Metrics.CreateGauge("ga_investorcentre_active_users",
                "Google Analytics realtime active users",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaInvestorCentrePageViews =
            Metrics.CreateGauge("ga_investorcentre_pageviews",
                "Google Analytics realtime screen pageviews",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaIssuerOnlineActiveUsers =
            Metrics.CreateGauge("ga_issueronline_active_users",
                "Google Analytics realtime active users (IssuerOnline)",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaIssuerOnlinePageViews =
            Metrics.CreateGauge("ga_issueronline_pageviews",
                "Google Analytics realtime screen pageviews (IssuerOnline)",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaSphereActiveUsers =
            Metrics.CreateGauge("ga_sphere_active_users",
                "Google Analytics realtime active users (Sphere)",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaSpherePageViews =
            Metrics.CreateGauge("ga_sphere_pageviews",
                "Google Analytics realtime screen pageviews (Sphere)",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        // -------- SNAPSHOTS (INVESTORCENTRE ONLY) --------

        private static readonly Gauge GaInvestorCentreDailyActiveUsers =
            Metrics.CreateGauge("ga_investorcentre_daily_active_users",
                "Daily snapshot active users",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen", "date" } });

        private static readonly Gauge GaInvestorCentreDailyPageViews =
            Metrics.CreateGauge("ga_investorcentre_daily_pageviews",
                "Daily snapshot pageviews",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen", "date" } });

        private static readonly Gauge GaIODailyActiveUsers =
           Metrics.CreateGauge("ga_issueronline_daily_active_users",
               "Daily snapshot active users",
               new GaugeConfiguration { LabelNames = new[] { "region", "screen", "date" } });

        private static readonly Gauge GaIODailyPageViews =
            Metrics.CreateGauge("ga_issueronline_daily_pageviews",
                "Daily snapshot pageviews",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen", "date" } });


        // -------------GEMS-------------------

        private static readonly Gauge GaGEMnlineActiveUsers =
    Metrics.CreateGauge("ga_gem_active_users",
        "Google Analytics realtime active users (GEM)",
        new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaGEMOnlinePageViews =
            Metrics.CreateGauge("ga_gem_pageviews",
                "Google Analytics realtime screen pageviews (GEM)",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen" } });

        private static readonly Gauge GaGEMDailyActiveUsers =
          Metrics.CreateGauge("ga_gem_daily_active_users",
              "Daily snapshot active users",
              new GaugeConfiguration { LabelNames = new[] { "region", "screen", "date" } });

        private static readonly Gauge GaGEMDailyPageViews =
            Metrics.CreateGauge("ga_gem_daily_pageviews",
                "Daily snapshot pageviews",
                new GaugeConfiguration { LabelNames = new[] { "region", "screen", "date" } });

        private static readonly Gauge GaActiveUsersPercentageChange =
    Metrics.CreateGauge(
        "ga_active_users_percentage_change",
        "Percentage change of active users by region and source",
        new GaugeConfiguration
        {
            LabelNames = new[] { "region", "source" } // source = IC / IO / GEMS
        });

        // --------------------------------------------------

        public GABackgroundService(
            ILogger<GABackgroundService> logger,
            GAHelper gaHelper,
            GATokenService gaTokenService,
            GASnapshots gaSnapshots)
        {
            _logger = logger;
            _gaHelper = gaHelper;
            //_tokenService = tokenService;
            _gaTokenService = gaTokenService;
            _gaSnapshots = gaSnapshots;
        }

        private int GetPreviousTotal(string region, string source)
        {
            var key = $"{region}|{source}";
            return _previousTotals.TryGetValue(key, out var value) ? value : 0;
        }

        private void SetPreviousTotal(string region, string source, int total)
        {
            var key = $"{region}|{source}";
            _previousTotals[key] = total;
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

        private async Task RefreshGaMetricsAsync(CancellationToken token)
        {
            _logger.LogInformation("Refreshing GA metrics...");
            var accessToken = await _gaTokenService.GetAccessTokenAsync();

            // ---------------- RESET PROMETHEUS GAUGES ----------------

            GaInvestorCentreActiveUsers.Unpublish();
            GaInvestorCentrePageViews.Unpublish();

            GaIssuerOnlineActiveUsers.Unpublish();
            GaIssuerOnlinePageViews.Unpublish();

            GaSphereActiveUsers.Unpublish();
            GaSpherePageViews.Unpublish();

            GaInvestorCentreDailyActiveUsers.Unpublish();
            GaInvestorCentreDailyPageViews.Unpublish();

            foreach (GAHelper.Region region in Enum.GetValues(typeof(GAHelper.Region)))
            {
                var regionLabel = region.ToString().ToUpperInvariant();

                // ----- InvestorCentre -----
                try
                {
                    var jsonIc = await _gaHelper.GetInvestorCentreMetricsAsync(accessToken, region, 15);
                    var modelIc = JsonSerializer.Deserialize<GADto>(jsonIc);

                    CachedGaJson = jsonIc;

                    var rows = modelIc?.Rows;
                    if (rows != null)
                    {

                        // Compute totals once per region
                        var totalActiveIC = rows.Sum(r => int.Parse(r.MetricValues[0].Value));
                        var previousTotalIC = GetPreviousTotal(regionLabel, "IC");

                        double percentageChangeIC = previousTotalIC == 0
                            ? 0
                            : Math.Abs((totalActiveIC - previousTotalIC) / (double)previousTotalIC) * 100;

                        // Set percentage metric once per region
                        GaActiveUsersPercentageChange.WithLabels(regionLabel, "IC").Set(percentageChangeIC);
                        SetPreviousTotal(regionLabel, "IC", totalActiveIC);


                        foreach (var row in rows)
                        {
                            var screen = row.DimensionValues[0].Value;
                            var active = int.Parse(row.MetricValues[0].Value);
                            var views = int.Parse(row.MetricValues[1].Value);

                            GaInvestorCentreActiveUsers.WithLabels(regionLabel, screen).Set(active);
                            GaInvestorCentrePageViews.WithLabels(regionLabel, screen).Set(views);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    _logger.LogDebug("InvestorCentre not configured for region {Region}", region);
                }

                // ----- IssuerOnline -----
                try
                {
                    var jsonIo = await _gaHelper.GetIssuerOnlineMetricsAsync(accessToken, region, 15);
                    var modelIo = JsonSerializer.Deserialize<GADto>(jsonIo);

                    var rows = modelIo?.Rows;
                    if (rows != null)
                    {

                        var totalActiveIO = rows.Sum(r => int.Parse(r.MetricValues[0].Value));

                        Console.Write($"Total ACtive IO SUM IS {totalActiveIO}");

                        var previousTotalIO = GetPreviousTotal(regionLabel, "IO");

                        Console.Write($"PREVIOUS TTOALS FOR IO SUM IS {previousTotalIO}");


                        double percentageChangeIO = previousTotalIO == 0
                            ? 0
                            : Math.Abs((totalActiveIO - previousTotalIO) / (double)previousTotalIO) * 100;

                        GaActiveUsersPercentageChange.WithLabels(regionLabel, "IO").Set(percentageChangeIO);
                        SetPreviousTotal(regionLabel, "IO", totalActiveIO);


                        foreach (var row in rows)
                        {
                            var screen = row.DimensionValues[0].Value;
                            var active = int.Parse(row.MetricValues[0].Value);
                            var views = int.Parse(row.MetricValues[1].Value);

                            GaIssuerOnlineActiveUsers.WithLabels(regionLabel, screen).Set(active);
                            GaIssuerOnlinePageViews.WithLabels(regionLabel, screen).Set(views);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "IssuerOnline request failed for region {Region}", region);
                }
            }


            try
            {
                const GAHelper.Region sphereRegion = GAHelper.Region.NA;
                var regionLabel = "GLOBAL";

                var jsonSphere = await _gaHelper.GetSphereMetricsAsync(accessToken, sphereRegion, 15);
                var modelSphere = JsonSerializer.Deserialize<GADto>(jsonSphere);

                var rows = modelSphere?.Rows;
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var screen = row.DimensionValues[0].Value;
                        var active = int.Parse(row.MetricValues[0].Value);
                        var views = int.Parse(row.MetricValues[1].Value);

                        GaSphereActiveUsers.WithLabels(regionLabel, screen).Set(active);
                        GaSpherePageViews.WithLabels(regionLabel, screen).Set(views);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sphere GA request failed");
            }

            // ---------------- SNAPSHOTS ----------------

            var snapshotRegions = new[] {
                GAHelper.Region.NA,
                GAHelper.Region.EMEA,
                GAHelper.Region.OCEANIA
            };

            var dates = new[] { "yesterday", "today" };

            var csRegex = new Regex(@"^Computershare Investor (?:Center - United States|Centre - Australia|Centre - Canada|Centre - UK)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);


            foreach (var region in snapshotRegions)
            {
                var regionLabel = region.ToString().ToUpperInvariant();

                foreach (var date in dates)
                {
                    try
                    {

                        var jsonSnapshot = await _gaSnapshots.GetInvestorCentreSnapshotMetricsAsync(accessToken, region, date, 10);
                        var modelSnapshot = JsonSerializer.Deserialize<GADto>(jsonSnapshot);

                        var rows = modelSnapshot?.Rows;
                        if (rows != null)
                        {

                            var filteredRows = rows.Where(x => csRegex.IsMatch(x.DimensionValues[0].Value)).ToList();

                            foreach (var row in filteredRows)
                            {
                                var screen = row.DimensionValues[0].Value;
                                var active = int.Parse(row.MetricValues[0].Value);
                                var views = int.Parse(row.MetricValues[1].Value);

                                //GaInvestorCentreDailyActiveUsers.WithLabels(regionLabel, screen).Set(active);
                                //GaInvestorCentreDailyPageViews.WithLabels(regionLabel, screen).Set(views);

                                var totalActive = filteredRows.Sum(r => int.Parse(r.MetricValues[0].Value));
                                var totalPageView = filteredRows.Sum(r => int.Parse(r.MetricValues[1].Value));


                                GaInvestorCentreDailyActiveUsers.WithLabels(regionLabel, screen, date).Set(totalActive);
                                GaInvestorCentreDailyPageViews.WithLabels(regionLabel, screen, date).Set(totalPageView);

                                _logger.LogInformation(
                                    "Snapshot IC row: region={Region}, screen={Screen}, active={Active}, views={Views}",
                                    regionLabel, screen, active, views);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Snapshot IC failed for region {Region}", region);
                    }
                }

                // -----------------------ISSUER ONLINE SNAPSHOTS -------------
            }

            var snapshotRegionsIO = new[] {
                GAHelper.Region.NA,
            };

            foreach (var region in snapshotRegionsIO)
            {
                var regionLabel = region.ToString().ToUpperInvariant();

                foreach (var date in dates)
                {
                    try
                    {

                        var jsonSnapshot = await _gaSnapshots.GetIssuerOnlineSnapshotMetricsAsync(accessToken, region, date, 10);
                        var modelSnapshot = JsonSerializer.Deserialize<GADto>(jsonSnapshot);

                        var rows = modelSnapshot?.Rows;
                        if (rows != null)
                        {
                            foreach (var row in rows)
                            {
                                var screen = row.DimensionValues[0].Value;
                                var active = int.Parse(row.MetricValues[0].Value);
                                var views = int.Parse(row.MetricValues[1].Value);

                                //GaInvestorCentreDailyActiveUsers.WithLabels(regionLabel, screen).Set(active);
                                //GaInvestorCentreDailyPageViews.WithLabels(regionLabel, screen).Set(views);

                                var totalActive = rows.Sum(r => int.Parse(r.MetricValues[0].Value));
                                var totalPageView = rows.Sum(r => int.Parse(r.MetricValues[1].Value));


                                GaIODailyActiveUsers.WithLabels(regionLabel, screen, date).Set(totalActive);
                                GaIODailyPageViews.WithLabels(regionLabel, screen, date).Set(totalPageView);

                                _logger.LogInformation(
                                    "Snapshot IC row: region={Region}, screen={Screen}, active={Active}, views={Views}",
                                    regionLabel, screen, active, views);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Snapshot IC failed for region {Region}", region);
                    }
                }
            }

            var GEMSRegions = new[] {
                GAHelper.Region2.Global,
            };
 

            //var dates = new[] { "yesterday", "today" };

            foreach (var region in GEMSRegions)
            {
                var regionLabel = region.ToString().ToUpperInvariant();

                foreach (var date in dates)
                {

                    try
                    {

                        var json = await _gaHelper.GetGEMMetricsAsync(accessToken, region, 10);
                        var jsonSnapShot = await _gaSnapshots.GetGEMSnapshotMetricsAsync(accessToken, region, date, 10);
                        var modelSnapshot = JsonSerializer.Deserialize<GADto>(json);

                        var rows = modelSnapshot?.Rows;
                        if (rows != null)
                        {
                            foreach (var row in rows)
                            {
                                var screen = row.DimensionValues[0].Value;
                                var active = int.Parse(row.MetricValues[0].Value);
                                var views = int.Parse(row.MetricValues[1].Value);

                                var totalActive = rows.Sum(r => int.Parse(r.MetricValues[0].Value));
                                var totalPageView = rows.Sum(r => int.Parse(r.MetricValues[1].Value));

                                GaGEMnlineActiveUsers.WithLabels(regionLabel, screen).Set(active);
                                GaGEMOnlinePageViews.WithLabels(regionLabel, screen).Set(views);

                                GaGEMDailyActiveUsers.WithLabels(regionLabel, screen, date).Set(totalActive);
                                GaGEMDailyPageViews.WithLabels(regionLabel, screen, date).Set(totalPageView);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Snapshot IC failed for region {Region}", region);
                    }
                }
            }

            // ---------------- EXPORT METRICS ----------------

            using var stream = new MemoryStream();
                await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                CachedMetrics = reader.ReadToEnd();

                _logger.LogInformation("GA metrics updated.");
        }
     }
}
