namespace Entities.DTOs.Geo;

public class ReverseGeocodeCityDto
{
    public int CityCode { get; set; }
    public string CityName { get; set; }
    public string? ResolvedAddress { get; set; }
}
