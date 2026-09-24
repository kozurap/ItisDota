namespace ItisDota.Business.Services;

public class PromptScope
{
    private readonly IServiceScopeFactory _scopes;

    public PromptScope(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task<T> UseAsync<T>(Func<PromptService, Task<T>> action)
    {
        using var scope = _scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PromptService>();
        return await action(service);
    }

    public async Task UseAsync(Func<PromptService, Task> action)
    {
        using var scope = _scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PromptService>();
        await action(service);
    }
}
