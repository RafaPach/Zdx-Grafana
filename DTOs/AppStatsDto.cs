using System.Text.Json.Serialization;

namespace NOCAPI.Modules.Zdx.DTOs
{
    public class AppStatsDto
    {
        public int AppId { get; set; }
        public string AppName { get; set; }
        public double Score { get; set; }
        public string MostImpactedRegion { get; set; }

        public int ActiveUsers { get; set; }
        public int ActiveDevices { get; set; }

        public int NumPoor { get; set; }
        public int NumOkay { get; set; }
        public int NumGood { get; set; }
        public int NumNA { get; set; }

        public double PercentPoor { get; set; }
        public double PercentOkay { get; set; }
        public double PercentGood { get; set; }
    }

    public class ZdxAppResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("most_impacted_region")]
        public MostImpactedRegion MostImpactedRegion { get; set; }

        [JsonPropertyName("stats")]
        public Stats Stats { get; set; }
    }

    public class MostImpactedRegion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("geo_type")]
        public string GeoType { get; set; }
    }

    public class Stats
    {
        [JsonPropertyName("active_users")]
        public int ActiveUsers { get; set; }

        [JsonPropertyName("active_devices")]
        public int ActiveDevices { get; set; }

        [JsonPropertyName("num_poor")]
        public int NumPoor { get; set; }

        [JsonPropertyName("num_okay")]
        public int NumOkay { get; set; }

        [JsonPropertyName("num_good")]
        public int NumGood { get; set; }

        [JsonPropertyName("num_na")]
        public int NumNA { get; set; }
    }
}
