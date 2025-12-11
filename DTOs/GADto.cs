using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.DTOs
{
    public class GADto
    {
        [JsonPropertyName("dimensionHeaders")]
        public List<GaDimensionHeader> DimensionHeaders { get; set; }

        [JsonPropertyName("metricHeaders")]
        public List<GaMetricHeader> MetricHeaders { get; set; }

        [JsonPropertyName("rows")]
        public List<GaRow> Rows { get; set; }

        [JsonPropertyName("rowCount")]
        public int RowCount { get; set; }

        [JsonPropertyName("kind")]
        public string Kind { get; set; }
    }

    public class GaDimensionHeader
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class GaMetricHeader
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class GaRow
    {
        [JsonPropertyName("dimensionValues")]
        public List<GaDimensionValue> DimensionValues { get; set; }

        [JsonPropertyName("metricValues")]
        public List<GaMetricValue> MetricValues { get; set; }
    }

    public class GaDimensionValue
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class GaMetricValue
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

}

