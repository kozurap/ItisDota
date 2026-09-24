namespace ItisDota.Data.Entities;

public class PlayerGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteToken { get; set; } = string.Empty;
    public int OwnerPlayerId { get; set; }
    public string? PromptText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<GroupMember> Members { get; set; } = new();
}
