using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.NewFiles
{
    public class GASnapshots
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public enum Region
        {
            NA,
            EMEA,
            OCEANIA,

        }

        //public enum Region2
        //{
        //    Global
        //}

        private static readonly IReadOnlyDictionary<GAHelper.Region2, int> PropertyIds_GEMS = new Dictionary<GAHelper.Region2, int>
            {
              { GAHelper.Region2.Global, 257449611 },
            };

        private static readonly IReadOnlyDictionary<GAHelper.Region, int> PropertyIds = new Dictionary<GAHelper.Region, int>
        {

        { GAHelper.Region.EMEA,    378497620 },
        { GAHelper.Region.OCEANIA, 384244692 },
        { GAHelper.Region.NA,      377402046 }

        };
        private static readonly IReadOnlyDictionary<GAHelper.Region, int> PropertyIds_IO = new Dictionary<GAHelper.Region, int>
        {

        { GAHelper.Region.NA,    384250257 },

        };


        public GASnapshots(IHttpClientFactory httpClientFactory)
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

        public async Task<string> GetInvestorCentreSnapshotMetricsAsync(string token, GAHelper.Region region, string date, int limit = 10)
        {
            var client = CreateAuthClient(token);

            if (!PropertyIds.TryGetValue(region, out var propertyId))
                throw new ArgumentException($"Unknown region: {region}", nameof(region));

            var url = $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runReport";

            var requestBody = new
            {
                //dateRanges = new[]
                //{
                //    new
                //    {
                //        startDate ="yesterday",
                //        endDate="yesterday"
                //    },
                //},

                dateRanges = new[]
                {
                    new
                    {
                        startDate = date,
                        endDate = date
                    }
                },
                dimensions = new[]
                    {
                        new { name ="unifiedScreenName" },
                        new {name = "region" }
                    },
                metrics = new[]
                        {
                new { name = "activeUsers" },
                new { name = "screenPageViews" } // keep this only if your property accepts it in Realtime
            },
                limit = 10000
            };

            using var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, jsonContent);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log or return the full GA error; it includes a helpful message and reason.
                throw new HttpRequestException(
                    $"GA Realtime API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {json}");
            }

            return json;
        }

        public async Task<string> GetIssuerOnlineSnapshotMetricsAsync(string token, GAHelper.Region region, string date, int limit = 10)
        {
            var client = CreateAuthClient(token);

            if (!PropertyIds_IO.TryGetValue(region, out var propertyId))
                throw new ArgumentException($"Unknown region: {region}", nameof(region));

            var url = $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runReport";

            var requestBody = new
            {
                dateRanges = new[]
                {
                    new
                    {
                        startDate = date,
                        endDate = date
                    }
                },
                dimensions = new[]
                    {
                        new { name ="unifiedScreenName" },
                        new {name = "region" }
                    },
                metrics = new[]
                        {
                new { name = "activeUsers" },
                new { name = "screenPageViews" } // keep this only if your property accepts it in Realtime
            },
                limit = 10000
            };

            using var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, jsonContent);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log or return the full GA error; it includes a helpful message and reason.
                throw new HttpRequestException(
                    $"GA Realtime API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {json}");
            }

            return json;
        }

        public async Task<string> GetGEMSnapshotMetricsAsync(string token, GAHelper.Region2 region, string date, int limit = 10)
        {
            var client = CreateAuthClient(token);

            if (!PropertyIds_GEMS.TryGetValue(region, out var propertyId))
                throw new ArgumentException($"Unknown region: {region}", nameof(region));

            var url = $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runReport";

            var requestBody = new
            {
                dateRanges = new[]
                {
                    new
                    {
                        startDate = date,
                        endDate = date
                    }
                },
                dimensions = new[]
                    {
                        new { name ="unifiedScreenName" },
                        new {name = "region" }
                    },
                metrics = new[]
                        {
                new { name = "activeUsers" },
                new { name = "screenPageViews" } // keep this only if your property accepts it in Realtime
            },
                limit = 10000
            };

            using var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, jsonContent);

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
