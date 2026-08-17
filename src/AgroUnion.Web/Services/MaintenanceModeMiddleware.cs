using System.Security.Claims;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace AgroUnion.Web.Services;

// When the admin toggles PlatformConfiguration.MaintenanceMode = true, every incoming request
// EXCEPT static assets, health checks, and auth/admin routes gets a full-page "Under Development"
// response with status 503 Service Unavailable. Admins keep normal access so they can turn the
// switch back off from the admin dashboard.
public sealed class MaintenanceModeMiddleware(RequestDelegate next)
{
    private static readonly string[] AlwaysAllowedPrefixes =
    {
        "/css/", "/js/", "/lib/", "/images/", "/fonts/", "/favicon", "/.well-known/",
        "/health", "/api/",
        "/account/login", "/account/logout", "/account/access-denied",
        "/account/forgot-password", "/account/reset-password", "/account/change-password",
        "/portal", // admin dashboard, portal actions
        "/home/error", "/error"
    };

    public async Task InvokeAsync(HttpContext context, AgroUnionDbContext db, ICompositeViewEngine viewEngine, ITempDataProvider tempData)
    {
        var path = context.Request.Path.Value ?? "/";
        if (IsAlwaysAllowed(path)) { await next(context); return; }

        // Admins always pass through so they can turn maintenance off.
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole(RoleNames.Admin))
        {
            await next(context); return;
        }

        var config = await db.PlatformConfigurations.AsNoTracking()
            .Select(x => new { x.MaintenanceMode, x.MaintenanceTitle, x.MaintenanceMessage })
            .SingleOrDefaultAsync(context.RequestAborted);

        if (config is null || !config.MaintenanceMode) { await next(context); return; }

        // Non-admin, maintenance ON, not a whitelisted path → serve the maintenance page.
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.RetryAfter = "300";
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";

        var routeData = context.GetRouteData();
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(context, routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var viewResult = viewEngine.FindView(actionContext, "Maintenance", isMainPage: true);
        if (!viewResult.Success) viewResult = viewEngine.GetView(executingFilePath: null, "/Views/Shared/Maintenance.cshtml", isMainPage: true);
        if (!viewResult.Success)
        {
            await context.Response.WriteAsync($"<!doctype html><html><head><meta charset=\"utf-8\"><title>{config.MaintenanceTitle}</title></head><body><h1>{config.MaintenanceTitle}</h1><p>{config.MaintenanceMessage}</p></body></html>", context.RequestAborted);
            return;
        }

        var title = string.IsNullOrWhiteSpace(config.MaintenanceTitle) ? "Ο ιστότοπος βρίσκεται υπό αναβάθμιση" : config.MaintenanceTitle;
        var message = string.IsNullOrWhiteSpace(config.MaintenanceMessage)
            ? "Πραγματοποιούμε προγραμματισμένες εργασίες συντήρησης για να βελτιώσουμε την πλατφόρμα της AGRO UNION. Θα επιστρέψουμε πολύ σύντομα."
            : config.MaintenanceMessage;
        var viewData = new ViewDataDictionary<MaintenanceViewModel>(
            metadataProvider: new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
            modelState: new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
        {
            Model = new MaintenanceViewModel(title, message)
        };
        var tempDataDict = new TempDataDictionary(context, tempData);
        await using var writer = new StreamWriter(context.Response.Body, leaveOpen: true);
        var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(actionContext, viewResult.View!, viewData, tempDataDict, writer, new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());
        await viewResult.View!.RenderAsync(viewContext);
        await writer.FlushAsync();
    }

    private static bool IsAlwaysAllowed(string path)
    {
        foreach (var prefix in AlwaysAllowedPrefixes)
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}

public sealed record MaintenanceViewModel(string Title, string Message);

public static class MaintenanceModeExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app) => app.UseMiddleware<MaintenanceModeMiddleware>();
}
