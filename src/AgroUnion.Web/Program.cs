using System.Text;
using System.Threading.RateLimiting;
using System.Globalization;
using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Web.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/agro-union-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IValidator<InterestApplicationRequest>, InterestApplicationValidator>();
builder.Services.AddScoped<IValidator<ContactRequest>, ContactRequestValidator>();
builder.Services.AddScoped<IValidator<ProductionRequest>, ProductionRequestValidator>();
builder.Services.AddScoped<IValidator<CounterOfferRequest>, CounterOfferValidator>();
builder.Services.AddSingleton<PartnerFileStore>();

builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 262_144_000);
builder.Services.AddAuthentication().AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Δεν έχει οριστεί Jwt:Key.");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";
    options.Cookie.Name = "AgroUnion.Portal";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        var manager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var user = await manager.GetUserAsync(context.Principal!);
        if (user is null || !user.IsActive) context.RejectPrincipal();
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(RoleNames.Admin));
    options.AddPolicy("FarmerOnly", p => p.RequireRole(RoleNames.Producer));
    options.AddPolicy("MerchantOnly", p => p.RequireRole(RoleNames.Trader));
    options.AddPolicy("PartnerCompanyOnly", p => p.RequireRole(RoleNames.Company));
    options.AddPolicy("BuyerOnly", p => p.RequireRole(RoleNames.Trader, RoleNames.Company));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-forms", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks().AddDbContextCheck<AgroUnionDbContext>("mysql");
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AGRO UNION API", Version = "v1", Description = "Ασφαλές API συναλλαγών και portal συνεργατών." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var greek = new CultureInfo("el-GR");
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(greek);
    options.SupportedCultures = [greek];
    options.SupportedUICultures = [greek];
});

var app = builder.Build();

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/home/error"); app.UseHsts(); }
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
var publicWebsitePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "/", "/about", "/history", "/team", "/vision", "/network", "/products", "/sustainability",
    "/faq", "/services", "/how-it-works", "/partners", "/contracts", "/apply", "/account/register",
    "/contact", "/privacy", "/legal", "/terms", "/cookies", "/partner-terms", "/accessibility", "/payments", "/account/login", "/account/forgot-password", "/account/reset-password",
    "/account/access-denied"
};
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.TrimEnd('/') ?? "";
    if (string.IsNullOrEmpty(path)) path = "/";
    var isPublicWebsitePage = publicWebsitePaths.Contains(path);
    var isPageRequest = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);

    if (isPublicWebsitePage && isPageRequest)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";

        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.Redirect("/portal");
            return;
        }
    }

    await next();
});
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgroUnionDbContext>();
    if (db.Database.IsRelational()) await db.Database.MigrateAsync(); else await db.Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.Run();

public partial class Program;
