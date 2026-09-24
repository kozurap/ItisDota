namespace ItisDota.Business.Models;

public class GroupRoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteToken { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public IReadOnlyList<PlayerDto> Members { get; set; } = Array.Empty<PlayerDto>();
}
