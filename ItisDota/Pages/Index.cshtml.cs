using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Business.Models;
using WebApplication1.Business.Services;

namespace WebApplication1.Pages;

public class IndexModel : PageModel
{
    private readonly PromptService _promptService;

    public IndexModel(PromptService promptService)
    {
        _promptService = promptService;
    }

    public IReadOnlyList<PlayerDto> Players { get; private set; } = Array.Empty<PlayerDto>();

    [BindProperty]
    public string JsonInput { get; set; } = string.Empty;

    [BindProperty]
    public IFormFile? JsonFile { get; set; }

    [BindProperty]
    public string PromptText { get; set; } = string.Empty;

    [BindProperty]
    public PlayerEditDto EditPlayer { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? TgSearch { get; set; }

    public string? TgSearchError { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostImportAsync(CancellationToken cancellationToken)
    {
        try
        {
            var json = await ResolveImportJsonAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                ErrorMessage = "Вставьте JSON или выберите файл перед сохранением.";
            }
            else
            {
                await _promptService.ImportPlayersAsync(json, cancellationToken);
                StatusMessage = "Игроки успешно сохранены (новые добавлены, существующие по Telegram-тегу обновлены).";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSavePromptAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PromptText))
            {
                ErrorMessage = "Промпт не может быть пустым.";
            }
            else
            {
                await _promptService.SavePromptAsync(PromptText, cancellationToken);
                StatusMessage = "Промпт сохранён.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (EditPlayer.Id <= 0)
            {
                ErrorMessage = "Не удалось определить игрока для удаления.";
            }
            else
            {
                await _promptService.DeletePlayerAsync(EditPlayer.Id, cancellationToken);
                StatusMessage = "Игрок удалён.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { TgSearch });
    }

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _promptService.UpdatePlayerAsync(EditPlayer, cancellationToken);
            StatusMessage = "Игрок обновлён.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { TgSearch });
    }

    public async Task<IActionResult> OnPostRecordSelectionsAsync(
        [FromBody] RecordSelectionsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PlayerIds is null || request.PlayerIds.Count == 0 || request.PlayerIds.Count % 10 != 0)
        {
            return BadRequest(new { error = "Число игроков должно быть кратно 10." });
        }

        await _promptService.RecordSelectionsAsync(request.PlayerIds, cancellationToken);
        return new JsonResult(new { ok = true });
    }

    private async Task<string> ResolveImportJsonAsync(CancellationToken cancellationToken)
    {
        if (JsonFile is { Length: > 0 })
        {
            await using var stream = JsonFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        return JsonInput;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var players = await _promptService.GetPlayersAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(TgSearch))
        {
            var term = TgSearch.Trim();
            if (term.Contains('@', StringComparison.Ordinal))
            {
                TgSearchError = "Не указывайте символ @ — он уже стоит слева от поля. Введите только ник.";
            }
            else
            {
                var searchTerm = "@" + term;
                players = players
                    .Where(p => p.TgTag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        Players = players;
        PromptText = await _promptService.GetPromptAsync(cancellationToken);
    }

    public sealed class RecordSelectionsRequest
    {
        public List<int> PlayerIds { get; set; } = new();
    }
}
