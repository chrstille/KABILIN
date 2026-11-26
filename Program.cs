using KABILIN.Data;
using KABILIN.Filters;
using KABILIN.Models;
using KABILIN.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// =====================================
// MVC (anti-forgery validated on every unsafe request)
// =====================================
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<RequirePasswordChangeFilter>();
});

// =====================================
// POSTGRESQL DATABASE
// =====================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// =====================================
// APPLICATION SERVICES
// =====================================
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IFileStorage, FileStorage>();
builder.Services.AddScoped<RequestWorkflowService>();

// Allow up to eight 5 MB files in one submission.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 60 * 1024 * 1024;
});

// =====================================
// COOKIE AUTHENTICATION
// =====================================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "KABILIN.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Sign out accounts that were deactivated or removed while signed in.
        options.Events.OnValidatePrincipal = async context =>
        {
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib") || path.StartsWith("/images"))
                return;

            var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idText, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var active = await db.Users.AsNoTracking()
                .Where(u => u.UserID == userId)
                .Select(u => (bool?)u.IsActive)
                .FirstOrDefaultAsync();
            if (active != true)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// =====================================
// DATABASE: apply migrations + seed administrator
// =====================================
await DbSeeder.InitialiseAsync(app.Services);

// =====================================
// MIDDLEWARE
// =====================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "same-origin";
    await next();
});

app.UseHttpsRedirection();

// Fixed culture so decimal coordinates and dates parse the same on every machine.
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US")
    .AddSupportedUICultures("en-US"));

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

// "/" goes to the login page (which forwards signed-in users to their dashboard).
app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Account", action = "Login" }
).WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}"
).WithStaticAssets();

app.Run();
