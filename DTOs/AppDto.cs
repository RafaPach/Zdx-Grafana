namespace NOCAPI.Modules.Zdx.Dto
{
    public class AppDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Score { get; set; } 
        public int Total_Users { get; set; }
        public RegionDto Most_Impacted_Region { get; set; }
    }

    public class RegionDto
    {
        public string Id { get; set; }
        public string Country { get; set; }
    }


}

