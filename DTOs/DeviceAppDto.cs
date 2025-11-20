using System.Text.Json.Serialization;

namespace NOCAPI.Modules.Zdx.Dto
{
    public class DeviceAppDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class WebProbeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonPropertyName("avg_pft")]
        public double AvgPft { get; set; }

        [JsonPropertyName("num_probes")]
        public int NumProbes { get; set; }

        [JsonPropertyName("avg_score")]
        public double AvgScore { get; set; }
    }
}
