namespace ItisDota.Business.Services;

public class PromptScope
{
    private readonly IServiceScopeFactory _scopes;

    public PromptScope(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task<TResult> UseAsync<TService, TResult>(Func<TService, Task<TResult>> action)
        where TService : notnull
    {
        using var scope = _scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    public async Task UseAsync<TService>(Func<TService, Task> action)
        where TService : notnull
    {
        using var scope = _scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await action(service);
    }
}
