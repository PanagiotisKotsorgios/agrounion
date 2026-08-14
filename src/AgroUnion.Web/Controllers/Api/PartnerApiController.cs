using System.Security.Claims;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroUnion.Web.Controllers.Api;

[ApiController, Route("api/portal"), IgnoreAntiforgeryToken, Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class PartnerApiController(IAgroUnionService service) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        if (User.IsInRole(RoleNames.Admin)) return Ok(await service.GetAdminDashboardAsync(ct: ct));
        if (User.IsInRole(RoleNames.Producer)) return Ok(await service.GetProducerDashboardAsync(UserId, ct));
        if (User.IsInRole(RoleNames.Trader)) return Ok(await service.GetBuyerDashboardAsync(UserId, false, ct));
        if (User.IsInRole(RoleNames.Company)) return Ok(await service.GetBuyerDashboardAsync(UserId, true, ct));
        return Forbid();
    }
}
