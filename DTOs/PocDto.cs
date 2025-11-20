namespace NOCAPI.Modules.Zdx.Dto
{
    public class PocDto
    {
        public int AppId { get; set; }
        public string AppName { get; set; }
        public double Score { get; set; }
        public double AvgPageFetchTime { get; set; }
        public string MostImpactedRegion { get; set; }
        public int TotalUsers { get; set; }
        public object TopUsers { get; set; } = new();
    }

    public class UserScoreDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public double Score { get; set; }
    }

    public class AppUsersResponseDto
    {
        public string Next_Offset { get; set; }
        public List<UserScoreDto> Users { get; set; }
    }

    public class AppMetricDto
    {
        public string Metric { get; set; }
        public string Unit { get; set; }
        public List<MetricDatapoint> Datapoints { get; set; }
    }

    public class MetricDatapoint
    {
        public long Timestamp { get; set; }
        public double Value { get; set; }
    }


}
