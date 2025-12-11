using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.NewFiles
{
    public class GAHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public enum Region
        {
            NA,
            EMEA,
            OCEANIA,
            Global
        }

        private static readonly IReadOnlyDictionary<Region, int> PropertyIds = new Dictionary<Region, int>
        {
            { Region.EMEA, 378497620 },
            { Region.OCEANIA,   384244692 },
            { Region.NA,   377402046 }
        };

        private static readonly IReadOnlyDictionary<Region, int> PropertyIds_IssuerOnline = new Dictionary<Region, int>
        {
            { Region.NA,   384250257 }
        };

        private static readonly IReadOnlyDictionary<Region, int> PropertyIds_SphereMobile = new Dictionary<Region, int>
            {
              { Region.Global, 487647347 },
            };


        public GAHelper(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateAuthClient(string token)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token?.Trim());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<string> GetInvestorCentreMetricsAsync(string token, Region region, int limit = 10)
        {
            var client = CreateAuthClient(token);

            if (!PropertyIds.TryGetValue(region, out var propertyId))
                throw new ArgumentException($"Unknown region: {region}", nameof(region));

            var url = $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runRealtimeReport";


            var requestBody = new
            {
                dimensions = new[] { new { name = "unifiedScreenName" } }, // page title & screen name (unified)
                metrics = new[]
                    {
            new { name = "activeUsers" },
            new { name = "screenPageViews" } // keep this only if your property accepts it in Realtime
        },
                orderBys = new[]
                    {
            new { metric = new { metricName = "activeUsers" }, desc = true }
        },
                limit
            };


            using var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, jsonContent);
            //response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                // Log or return the full GA error; it includes a helpful message and reason.
                throw new HttpRequestException(
                    $"GA Realtime API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {json}");
            }

            return json;
        }


        public async Task<string> GetIssuerOnlineMetricsAsync(string token, Region region, int limit = 10)
        {
            var client = CreateAuthClient(token);

            if (!PropertyIds_IssuerOnline.TryGetValue(region, out var propertyId))
                throw new ArgumentException($"Unknown region: {region}", nameof(region));

            var url = $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runRealtimeReport";


            var requestBody = new
            {
                dimensions = new[] { new { name = "unifiedScreenName" } }, // page title & screen name (unified)
                metrics = new[]
                    {
            new { name = "activeUsers" },
            new { name = "screenPageViews" } // keep this only if your property accepts it in Realtime
        },
                orderBys = new[]
                    {
            new { metric = new { metricName = "activeUsers" }, desc = true }
        },
                limit
            };


            using var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, jsonContent);
            //response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                // Log or return the full GA error; it includes a helpful message and reason.
                throw new HttpRequestException(
                    $"GA Realtime API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {json}");
            }

            return json;
        }

        public async Task<string> GetSphereMetricsAsync(string token, Region region, int limit = 10)
        {
            var client = CreateAuthClient(token);

            if (!PropertyIds_SphereMobile.TryGetValue(region, out var propertyId))
                throw new ArgumentException($"Unknown region: {region}", nameof(region));

            var url = $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runRealtimeReport";


            var requestBody = new
            {
                dimensions = new[] { new { name = "unifiedScreenName" } }, // page title & screen name (unified)
                metrics = new[]
                    {
            new { name = "activeUsers" },
            new { name = "screenPageViews" } // keep this only if your property accepts it in Realtime
        },
                orderBys = new[]
                    {
            new { metric = new { metricName = "activeUsers" }, desc = true }
        },
                limit
            };


            using var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, jsonContent);
            //response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                // Log or return the full GA error; it includes a helpful message and reason.
                throw new HttpRequestException(
                    $"GA Realtime API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {json}");
            }

            return json;
        }

    }
}

