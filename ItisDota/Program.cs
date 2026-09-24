using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ItisDota.Business.Services;
using ItisDota.Data;
using ItisDota.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddSingleton<PromptScope>();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.AddTransient<KeycloakBackchannelHandler>();
builder.Services.AddHttpClient(KeycloakTokenValidator.HttpClientName)
    .AddHttpMessageHandler<KeycloakBackchannelHandler>();
builder.Services.AddSingleton<KeycloakTokenValidator>();
builder.Services.AddScoped<KeycloakAuthService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "itisdota.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = context =>
        {
            var auth = context.HttpContext.RequestServices.GetRequiredService<KeycloakAuthService>();
            return auth.RefreshIfNeededAsync(context);
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

await InitializeDatabaseAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapPost("/logout", async (HttpContext httpContext, IAntiforgery antiforgery, KeycloakAuthService auth) =>
{
    await antiforgery.ValidateRequestAsync(httpContext);
    var refreshToken = await httpContext.GetTokenAsync(KeycloakAuthService.RefreshTokenName);
    await auth.LogoutAsync(refreshToken, httpContext.RequestAborted);
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();
app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<ItisDota.Components.App>()
    .AddInteractiveServerRenderMode()
    .WithStaticAssets();

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    const int maxAttempts = 30;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"Waiting for PostgreSQL (attempt {attempt}/{maxAttempts}): {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
