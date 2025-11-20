namespace NOCAPI.Modules.Zdx.Dto
{

    public class UsersResponseDto
    {
        public string Next_Offset { get; set; }
        public List<UserDto> Users { get; set; }
    }
    public class UserDto
    {
        public int Id { get; set; } 
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class IndivUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<DeviceDto> Devices { get; set; }
    }

    public class DeviceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<GeoLocationDto> Geo_Loc { get; set; }
    }

    public class GeoLocationDto
    {
        public string Country { get; set; }
    }
}
