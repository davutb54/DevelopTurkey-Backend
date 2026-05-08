using Entities.DTOs.Geo;

namespace WebAPI.Services;

public interface IGeoLocationService
{
    Task<ReverseGeocodeCityDto?> ReverseGeocodeCityAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
