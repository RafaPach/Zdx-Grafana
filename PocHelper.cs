////using Microsoft.Extensions.Configuration;
//using Microsoft.AspNetCore.Http;
//using Newtonsoft.Json.Linq;
//using NOCAPI.Modules.Zdx.Dto;
//using NOCAPI.Modules.Zdx.DTOs;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using NOCAPI.Modules.Zdx;

//namespace NOCAPI.Modules.Zdx
//{
//    public class PocHelper
//    {
//        private readonly IHttpClientFactory _httpClientFactory;
//        //private readonly IConfiguration _configuration;
//        //private readonly HelperMethods _helperMethods;


//        public PocHelper(IHttpClientFactory httpClientFactory)
//        {
//            _httpClientFactory = httpClientFactory;
//            //_configuration = configuration;
//            //_helperMethods = helperMethods;
//        }

//        public HttpClient CreateAuthClient(string token)
//        {
//            var client = _httpClientFactory.CreateClient();
//            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token?.Trim());
//            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//            return client;
//        }

//        public Task<List<AppDto>> GetAppsCached(string token) =>
//          ApiCache.GetOrFetchAsync("apps", () => GetApps(token));

//        public async Task<List<AppDto>?> GetApps(string token)
//        {

//            await RateLimiter.WaitTurnAsync(); 

//            var client = CreateAuthClient(token);
//            var url = "https://api.zsapi.net/zdx/v1/apps";
//            var appsRes = await client.GetAsync(url);
//            if (!appsRes.IsSuccessStatusCode) return null;

//            var json = await appsRes.Content.ReadAsStringAsync();
//            return JsonSerializer.Deserialize<List<AppDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//        }

//        public Task<ZdxAppResponseDto> GetAppStatsCached(int appId, string token) =>
//           ApiCache.GetOrFetchAsync($"appstats-{appId}", () => GetStatsPerApp(appId, token));


//        public async Task<List<AppStatsDto>> GetStatsPerApp(string token)
//        {

//            await RateLimiter.WaitTurnAsync(); // ⏳ wait 3 sec if needed

//            var client = CreateAuthClient(token);

//            var apps = await GetApps(token);
//            if (apps == null) return new();

//            var interestedApps = new List<string>
//             {
//            "Microsoft Login",
//            "Outlook Online",
//            "SharePoint Online EMEA",
//            "OneDrive for Business EMEA",
//            "Microsoft Teams Web App EMEA"
//             };

//            var filteredApps = apps
//                .Where(a => interestedApps.Contains(a.Name))
//                .OrderBy(a => a.Score)
//                .ToList();

//            var allStats = new List<AppStatsDto>();

//            foreach (var app in filteredApps)
//            {
//                var url = $"https://api.zsapi.net/zdx/v1/apps/{app.Id}";

//                Console.WriteLine("making getcallnow");

//                await RateLimiter.WaitTurnAsync();

//                var response = await client.GetAsync(url);

//                if (!response.IsSuccessStatusCode) continue;

//                var json = await response.Content.ReadAsStringAsync();

//                Console.WriteLine(json);

//                var result = JsonSerializer.Deserialize<ZdxAppResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//                if (result == null) continue;

//                var statsDto = new AppStatsDto
//                {
//                    AppId = result.Id,
//                    AppName = result.Name,
//                    Score = result.Score,
//                    MostImpactedRegion = result.MostImpactedRegion?.Country ?? "Unknown",
//                    ActiveUsers = result.Stats.ActiveUsers,
//                    ActiveDevices = result.Stats.ActiveDevices,
//                    NumPoor = result.Stats.NumPoor,
//                    NumOkay = result.Stats.NumOkay,
//                    NumGood = result.Stats.NumGood,
//                    NumNA = result.Stats.NumNA,
//                    PercentPoor = result.Stats.ActiveUsers > 0 ? (double)result.Stats.NumPoor / result.Stats.ActiveUsers * 100 : 0,
//                    PercentOkay = result.Stats.ActiveUsers > 0 ? (double)result.Stats.NumOkay / result.Stats.ActiveUsers * 100 : 0,
//                    PercentGood = result.Stats.ActiveUsers > 0 ? (double)result.Stats.NumGood / result.Stats.ActiveUsers * 100 : 0,
//                };

//                allStats.Add(statsDto);
//            }

//            return allStats;

//        }
//        private async Task<List<UserScoreDto>> GetTopUsersAsync(int appId, int limit, string token)
//        {
//            var client = CreateAuthClient(token);
//            var url = $"https://api.zsapi.net/zdx/v1/apps/{appId}/users?limit={limit}";

//            await RateLimiter.WaitTurnAsync();

//            var response = await client.GetAsync(url);
//            if (!response.IsSuccessStatusCode) return new();

//            var json = await response.Content.ReadAsStringAsync();
//            var result = JsonSerializer.Deserialize<AppUsersResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//            return result?.Users?.Select(u => new UserScoreDto
//            {
//                Name = u.Name,
//                Email = u.Email,
//                Score = u.Score
//            }).ToList() ?? new();
//        }



//        private async Task<double> GetAvgPageFetchTimeAsync(int appId, string token)
//        {
//            var client = CreateAuthClient(token);
//            var url = $"https://api.zsapi.net/zdx/v1/apps/{appId}/metrics";

//            await RateLimiter.WaitTurnAsync();

//            var response = await client.GetAsync(url);
//            if (!response.IsSuccessStatusCode) return 0;

//            var json = await response.Content.ReadAsStringAsync();
//            var metrics = JsonSerializer.Deserialize<List<AppMetricDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//            var pft = metrics?.FirstOrDefault(m => m.Metric == "pft");
//            if (pft == null || pft.Datapoints.Count == 0) return 0;

//            return Math.Round(pft.Datapoints.Average(d => d.Value), 2);
//        }




//        public async Task<List<PocDto>> GetAppPoc(string token)
//        {
//            await RateLimiter.WaitTurnAsync();


//            var apps = await GetApps(token);
//            if (apps == null) return new();

//            var interestedApps = new List<string>
//             {
//            "Microsoft Login",
//            "Outlook Online",
//            "SharePoint Online EMEA",
//            "OneDrive for Business EMEA",
//            "Microsoft Teams Web App EMEA"
//             };

//            var filteredApps = apps
//                .Where(a => interestedApps.Contains(a.Name))
//                .OrderBy(a => a.Score)
//                //.Take(5)
//                .ToList();

//            var appOverviewList = new List<PocDto>();

//            foreach (var app in filteredApps)
//            {
//                var overview = new PocDto
//                {
//                    AppId = app.Id,
//                    AppName = app.Name,
//                    Score = app.Score,
//                    MostImpactedRegion = app.Most_Impacted_Region.Country,
//                    TotalUsers = app.Total_Users
//                };

//                overview.AvgPageFetchTime = await GetAvgPageFetchTimeAsync(app.Id, token);

//                if (app.Score < 69)
//                {
//                    var users = await GetTopUsersAsync(app.Id, 10, token);
//                    overview.TopUsers = users
//                        .Where(u => u.Score < 70)
//                        .OrderBy(u => u.Score)
//                        .Take(5)
//                        .ToList();
//                }
//                else
//                {
//                    overview.TopUsers = "No most impacted users - App healthy";

//                }

//                appOverviewList.Add(overview);
//            }

//            return appOverviewList;
//        }

//    }
//}




//using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using NOCAPI.Modules.Zdx;
using NOCAPI.Modules.Zdx.Dto;
using NOCAPI.Modules.Zdx.DTOs;
using System;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NOCAPI.Modules.Zdx
{
    public class PocHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RateLimiter _rateLimiter;
        private readonly ApiCache _apiCache;
        private static readonly Random _random = new();



        public PocHelper(IHttpClientFactory httpClientFactory, RateLimiter rateLimiter )
        {
            _httpClientFactory = httpClientFactory;
            _rateLimiter = rateLimiter;
            _apiCache = new ApiCache(rateLimiter);

        }

        public HttpClient CreateAuthClient(string token)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token?.Trim());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        //public Task<List<AppDto>> GetAppsCached(string token) =>
        //    _apiCache.GetOrFetchAsync("apps", () => GetApps(token));

        public async Task<List<AppDto>?> GetApps(string token)
        {
            await _rateLimiter.WaitTurnAsync();

            var client = CreateAuthClient(token);
            var url = "https://api.zsapi.net/zdx/v1/apps";
            var appsRes = await client.GetAsync(url);
            if (!appsRes.IsSuccessStatusCode) return null;

            var json = await appsRes.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AppDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<AppStatsDto?> GetStatsForAppAsync(int appId, string token)
        {

            var client = CreateAuthClient(token);
            var url = $"https://api.zsapi.net/zdx/v1/apps/{appId}";

            await _rateLimiter.WaitTurnAsync();
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ZdxAppResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result == null) return null;

            return new AppStatsDto
            {
                AppId = result.Id,
                AppName = result.Name,
                Score = result.Score,
                MostImpactedRegion = result.MostImpactedRegion?.Country ?? "Unknown",
                ActiveUsers = result.Stats.ActiveUsers,
                ActiveDevices = result.Stats.ActiveDevices,
                NumPoor = result.Stats.NumPoor,
                NumOkay = result.Stats.NumOkay,
                NumGood = result.Stats.NumGood,
                NumNA = result.Stats.NumNA,
                PercentPoor = result.Stats.ActiveUsers > 0 ? (double)result.Stats.NumPoor / result.Stats.ActiveUsers * 100 : 0,
                PercentOkay = result.Stats.ActiveUsers > 0 ? (double)result.Stats.NumOkay / result.Stats.ActiveUsers * 100 : 0,
                PercentGood = result.Stats.ActiveUsers > 0 ? (double)result.Stats.NumGood / result.Stats.ActiveUsers * 100 : 0
            };
        }

        //public Task<AppStatsDto?> GetStatsForAppCached(int appId, string token) =>
        //   _apiCache.GetOrFetchAsync($"appstats-{appId}", () => GetStatsForAppAsync(appId, token));

        public async Task<List<AppStatsDto>> GetStatsPerApp(string token)
        {
            await _rateLimiter.WaitTurnAsync();

            var apps = await GetApps(token);
            if (apps == null) return new();

            var interestedApps = new List<string>
            {
                "Microsoft Login",
                "Outlook Online",
                "SharePoint Online EMEA",
                "OneDrive for Business EMEA",
                "Microsoft Teams Web App EMEA"
            };

            var filteredApps = apps
                .Where(a => interestedApps.Contains(a.Name))
                .OrderBy(a => a.Score)
                .ToList();

            var allStats = new List<AppStatsDto>();
            foreach (var app in filteredApps)
            {
                var statsDto = await GetStatsForAppAsync(app.Id, token);
                if (statsDto != null) allStats.Add(statsDto);
            }

            return allStats;
        }

        private async Task<List<UserScoreDto>> GetTopUsersAsync(int appId, int limit, string token)
        {
            var client = CreateAuthClient(token);
            var url = $"https://api.zsapi.net/zdx/v1/apps/{appId}/users?limit={limit}";

            await _rateLimiter.WaitTurnAsync();

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppUsersResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Users?.Select(u => new UserScoreDto
            {
                Name = u.Name,
                Email = u.Email,
                Score = u.Score
            }).ToList() ?? new();
        }

        private async Task<double> GetAvgPageFetchTimeAsync(int appId, string token)
        {
            var client = CreateAuthClient(token);
            var url = $"https://api.zsapi.net/zdx/v1/apps/{appId}/metrics";

            await _rateLimiter.WaitTurnAsync();

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return 0;

            var json = await response.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<List<AppMetricDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var pft = metrics?.FirstOrDefault(m => m.Metric == "pft");
            if (pft == null || pft.Datapoints.Count == 0) return 0;

            return Math.Round(pft.Datapoints.Average(d => d.Value), 2);
        }

        public async Task<List<PocDto>> GetAppPoc(string token)
        {
            await _rateLimiter.WaitTurnAsync();

            var apps = await GetApps(token);
            if (apps == null) return new();

            var interestedApps = new List<string>
            {
                "Microsoft Login",
                "Outlook Online",
                "SharePoint Online EMEA",
                "OneDrive for Business EMEA",
                "Microsoft Teams Web App EMEA"
            };

            var filteredApps = apps
                .Where(a => interestedApps.Contains(a.Name))
                .OrderBy(a => a.Score)
                .ToList();

            var appOverviewList = new List<PocDto>();

            foreach (var app in filteredApps)
            {
                var overview = new PocDto
                {
                    AppId = app.Id,
                    AppName = app.Name,
                    Score = app.Score,
                    MostImpactedRegion = app.Most_Impacted_Region.Country,
                    TotalUsers = app.Total_Users
                };

                overview.AvgPageFetchTime = await GetAvgPageFetchTimeAsync(app.Id, token);

                if (app.Score < 60)
                {
                    var users = await GetTopUsersAsync(app.Id, 10, token);
                    overview.TopUsers = users
                        .Where(u => u.Score < 70)
                        .OrderBy(u => u.Score)
                        .Take(5)
                        .ToList();
                }
                else
                {
                    overview.TopUsers = "No most impacted users - App healthy";
                }

                appOverviewList.Add(overview);
            }

            return appOverviewList;
        }
    }
}
