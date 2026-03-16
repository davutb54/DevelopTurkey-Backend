using Core.Utilities.Context;
using System.Security.Claims;

namespace WebAPI.Context;

public class WebClientContext : IClientContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebClientContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetIpAddress() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Bilinmiyor";

    public string GetPort() => _httpContextAccessor.HttpContext?.Connection?.RemotePort.ToString() ?? "Bilinmiyor";

    public int? GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id)) return id;
        return null;
    }

    public string GetUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name
               ?? _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
               ?? "Anonim/Sistem";
    }

    public List<string> GetRoles()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims?
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }

    public int? GetInstitutionId()
    {
        var institutionClaim = _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "InstitutionId");
        if (institutionClaim != null && int.TryParse(institutionClaim.Value, out int id)) return id;
        return null;
    }
}