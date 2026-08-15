using System.Security.Claims;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgroUnion.Web.Services;

public sealed class PlatformAuditActionFilter(AgroUnionDbContext db, ILogger<PlatformAuditActionFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
        {
            await next();
            return;
        }

        ActionExecutedContext? executed = null;
        Exception? failure = null;
        try
        {
            executed = await next();
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            var principal = context.HttpContext.User;
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                try
                {
                    var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
                    var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
                    var routeTargets = context.RouteData.Values
                        .Where(x => x.Key is not ("controller" or "action"))
                        .Select(x => $"{x.Key}={Limit(x.Value?.ToString(), 120)}")
                        .ToArray();
                    var controllerInstance = context.Controller as Controller;
                    var hasHandledError = controllerInstance?.TempData.ContainsKey("Error") == true;
                    var succeeded = failure is null && executed?.Exception is null && context.HttpContext.Response.StatusCode < 400 && !hasHandledError;
                    var agent = request.Headers.UserAgent.ToString();

                    db.AuditLogs.Add(new AuditLog
                    {
                        UserId = userId,
                        Action = $"{request.Method}_{action}",
                        Category = principal.IsInRole(RoleNames.Admin) ? "Administration" : "UserAction",
                        Severity = succeeded ? (principal.IsInRole(RoleNames.Admin) ? "Warning" : "Info") : "Critical",
                        EntityName = controller,
                        EntityId = Limit(action, 80) ?? "Unknown",
                        Details = $"{request.Method} {request.Path}. {(routeTargets.Length == 0 ? "Χωρίς route target." : string.Join(", ", routeTargets))}",
                        IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Limit(agent, 500),
                        CorrelationId = Limit(context.HttpContext.TraceIdentifier, 100),
                        Succeeded = succeeded
                    });
                    await db.SaveChangesAsync(context.HttpContext.RequestAborted);
                }
                catch (Exception auditException)
                {
                    logger.LogError(auditException, "Could not persist platform audit event for {Method} {Path}", request.Method, request.Path);
                }
            }
        }
    }

    private static string? Limit(string? value, int length) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= length ? value : value[..length];
}
