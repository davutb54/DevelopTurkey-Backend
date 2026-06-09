using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

/// <summary>
/// Cross-tenant (kurum-bağımsız) veri erişim uç noktaları.
/// Global query filter'lar admin.cross_institution_read sahiplerinde otomatik bypass edilir
/// (WebTenantProvider IsGlobalAdmin = true atar).
/// Bu controller yalnızca intent'i belgelemek ve güvenlik sınırını vurgulamak amacıyla
/// ayrı tutulmuştur.
/// </summary>
[Route("api/admin/global")]
[ApiController]
public class GlobalAdminController : Controller
{
    private readonly IUserService     _userService;
    private readonly IProblemService  _problemService;
    private readonly ISolutionService _solutionService;

    public GlobalAdminController(
        IUserService     userService,
        IProblemService  problemService,
        ISolutionService solutionService)
    {
        _userService     = userService;
        _problemService  = problemService;
        _solutionService = solutionService;
    }

    /// <summary>GET /api/admin/global/users/{id}</summary>
    [HttpGet("users/{id:int}")]
    [RequireCapability("admin.cross_institution_read")]
    public IActionResult GetUser(int id)
    {
        var result = _userService.GetById(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>GET /api/admin/global/problems/{id}</summary>
    [HttpGet("problems/{id:int}")]
    [RequireCapability("admin.cross_institution_read")]
    public IActionResult GetProblem(int id)
    {
        var result = _problemService.GetById(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>GET /api/admin/global/solutions/{id}</summary>
    [HttpGet("solutions/{id:int}")]
    [RequireCapability("admin.cross_institution_read")]
    public IActionResult GetSolution(int id)
    {
        var result = _solutionService.GetById(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
