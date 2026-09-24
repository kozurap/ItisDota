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
builder.Services.AddScoped<IAppSettingRepository, AppSettingRepository>();
builder.Services.AddScoped<PlayerImportService>();
builder.Services.AddScoped<PromptService>();
builder.Services.AddSingleton<PromptScope>();

var app = builder.Build();

await InitializeDatabaseAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
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

            var settings = scope.ServiceProvider.GetRequiredService<IAppSettingRepository>();
            var existing = await settings.GetValueAsync(PromptService.TeamPromptKey);
            if (existing is null)
            {
                await settings.SetValueAsync(PromptService.TeamPromptKey, PromptService.DefaultPrompt);
            }

            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"Waiting for PostgreSQL (attempt {attempt}/{maxAttempts}): {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
